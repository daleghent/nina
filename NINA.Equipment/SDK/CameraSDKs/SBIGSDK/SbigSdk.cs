using NINA.Core.Utility;
using NINA.Equipment.SDK.CameraSDKs.SBIGSDK.SbigSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Threading;
using NINA.Core.Interfaces.Utility;

namespace NINA.Equipment.SDK.CameraSDKs.SBIGSDK {

    /// <summary>
    /// Encapsulates SBIG driver access primitives
    /// </summary>
    public class SbigSdk : ISbigSdk {

        private object driverLock = new object();
        private object exposureLock = new object();
        private IMicroCache<SBIG.QueryTemperatureStatusResults2> queryTemperatureStatusCache;
        private CcdCameraInfo openedCameraInfo;

        private static bool sdkIsInitialized = false;
        private static byte refCount = 0;
        private static SBIG.DeviceType? openedDevice;
        private static SBIG.CameraType openedDeviceType = SBIG.CameraType.NoCamera;

        public SbigSdk(IMicroCacheFactory microCacheFactory) {
            this.queryTemperatureStatusCache = microCacheFactory.Create<SBIG.QueryTemperatureStatusResults2>();
        }

        /// <summary>
        /// Gets a string representing the sdk file version
        /// </summary>
        /// <returns></returns>
        public string GetSdkVersion() {
            return SBIG.GetVersion().FileVersion;
        }

        /// <summary>
        /// Ensures the SDK is initialized, and increments a ref counter if it already is
        /// </summary>
        public void InitSdk() {
            lock (driverLock) {
                Logger.Trace($"SBIGSDK: Initializing SBIG SDK, refCount={refCount}, sdkIsInitialized={sdkIsInitialized}");

                if (!sdkIsInitialized && refCount == 0) {
                    UnivDrvCommand<object>(SBIG.Cmd.CC_OPEN_DRIVER, null);
                    sdkIsInitialized = true;
                }
            }
        }

        /// <summary>
        /// Releases the SDK, freeing it if the ref count drops to 0. Every SDK client should call InitSdk on initialization, and ReleaseSdk on teardown
        /// </summary>
        public void ReleaseSdk() {
            lock (driverLock) {
                Logger.Trace($"SBIGSDK: Releasing SBIG SDK, refCount={refCount}");

                if (sdkIsInitialized && refCount == 0) {
                    if (openedDevice.HasValue) {
                        CloseDevice();
                    }

                    SBIG.UnivDrvCommand(SBIG.Cmd.CC_CLOSE_DRIVER, null);
                    sdkIsInitialized = false;
                }
            }
        }

        /// <summary>
        /// Opens the device connected on the interface defined by deviceId
        /// </summary>
        /// <param name="deviceId">An enumeration representing the port and connection type for the device (ie, USB1, USB2, etc). The deviceId from QueryUsbDevices should be provided</param>
        /// <returns>The type of device opened up. Raises an exception if the device couldn't be opened</returns>
        public SBIG.CameraType OpenDevice(SBIG.DeviceType deviceId) {
            lock (driverLock) {
                Logger.Trace($"SBIGSDK: Opening device {deviceId}, refCount={refCount}");

                if (openedDevice.HasValue) {
                    if (openedDevice.Value == deviceId) {
                        Logger.Trace($"SBIGSDK: Device {deviceId} already opened. Only increasing ref count");
                    } else {
                        throw new Exception($"Cannot open {deviceId} when {openedDevice.Value} is already open");
                    }
                } else {
                    UnivDrvCommand(SBIG.Cmd.CC_OPEN_DEVICE, new SBIG.OpenDeviceParams { deviceType = deviceId });
                    var result = UnivDrvCommand<SBIG.EstablishLinkParams, SBIG.EstablishLinkResults>(SBIG.Cmd.CC_ESTABLISH_LINK, new SBIG.EstablishLinkParams());
                    openedDeviceType = result.cameraType;
                    if (openedDeviceType != SBIG.CameraType.NoCamera) {
                        openedCameraInfo = GetCameraInfo();
                    }
                    openedDevice = deviceId;
                }

                ++refCount;
                Logger.Trace($"SBIGSDK: Opened device {deviceId}, cameraType={openedDeviceType}, refCount={refCount}");
                return openedDeviceType;
            }
        }

        /// <summary>
        /// Closes the open device
        /// NOTE: For now this supports just one open device. When Filter Wheel support is added, this will likely take a parameter to know which device to close
        /// </summary>
        public void CloseDevice() {
            lock (driverLock) {
                Logger.Trace($"SBIGSDK: Closing device {openedDevice}");

                if (openedDevice.HasValue) {
                    SBIG.UnivDrvCommand(SBIG.Cmd.CC_CLOSE_DEVICE, null);
                    openedDevice = null;
                }
            }
        }

        /// <summary>
        /// Converts a binary coded decimal to a native format. Every base-10 digit is encoded in 4 bits
        /// </summary>
        /// <param name="bcd">The binary coded decimal</param>
        /// <returns>The native uint converted</returns>
        public static uint BCDToUInt(uint bcd) {
            uint result = 0;
            uint multiplier = 1;
            for (int i = 0; i < 8; ++i) {
                var nextDigit = (bcd >> (4 * i)) & 0xF;
                result += nextDigit * multiplier;
                multiplier *= 10;
            }
            return result;
        }

        /// <summary>
        /// Gets a unified set of camera settings and information for the connected imaging device
        /// </summary>
        /// <returns>A struct containing a union of all relevant camera information</returns>
        public CcdCameraInfo GetCameraInfo() {
            var cameraInfo = new CcdCameraInfo();
            var ccdInfo01 = UnivDrvCommand<SBIG.GetCcdInfoParams, SBIG.GetCcdInfoResults01>(
                SBIG.Cmd.CC_GET_CCD_INFO, new SBIG.GetCcdInfoParams(SBIG.CcdInfoRequest.ImagingCcdStandard));
            var ccdInfo2 = UnivDrvCommand<SBIG.GetCcdInfoParams, SBIG.GetCcdInfoResults2>(
                SBIG.Cmd.CC_GET_CCD_INFO, new SBIG.GetCcdInfoParams(SBIG.CcdInfoRequest.CameraInfoExtended));
            var ccdInfo45 = UnivDrvCommand<SBIG.GetCcdInfoParams, SBIG.GetCcdInfoResults45>(
                SBIG.Cmd.CC_GET_CCD_INFO, new SBIG.GetCcdInfoParams(SBIG.CcdInfoRequest.ImagingCcdSecondaryExtended));
            var ccdInfo6 = UnivDrvCommand<SBIG.GetCcdInfoParams, SBIG.GetCcdInfoResults6>(
                SBIG.Cmd.CC_GET_CCD_INFO, new SBIG.GetCcdInfoParams(SBIG.CcdInfoRequest.CcdCameraExtended));

            // Info from GetCcdInfoResults01
            cameraInfo.FirmwareVersion = $"{ccdInfo01.firmwareVersion >> 8}.{ccdInfo01.firmwareVersion & 0xFF}";
            cameraInfo.CameraType = ccdInfo01.cameraType;
            cameraInfo.Name = ccdInfo01.name;
            cameraInfo.ReadoutModeConfigs = new ReadoutModeConfig[ccdInfo01.readoutModeCount];
            for (int i = 0; i < ccdInfo01.readoutModeCount; ++i) {
                var rawMode = ccdInfo01.readoutInfo[i].mode;
                var readoutMode = new ReadoutMode() {
                    RawMode = rawMode,
                    Mode = (SBIG.ReadoutMode)(rawMode & 0xFF)
                };

                int binX = 0, binY = 0;
                bool binOffChip = false;
                // The raw binning mode is encoded in 2-bytes. The least significant corresponds to the SBIG.ReadoutMode enum, and the most significant is an optional
                // one containing a parameter, if relevant. For example "BinNx1" requires the higher byte set for N, as does "BinNxN"
                switch (readoutMode.Mode) {
                    case SBIG.ReadoutMode.NoBinning:
                        binX = binY = 1;
                        break;
                    case SBIG.ReadoutMode.Bin2x2:
                        binX = binY = 2;
                        break;
                    case SBIG.ReadoutMode.Bin3x3:
                        binX = binY = 3;
                        break;
                    case SBIG.ReadoutMode.BinNx1:
                        binX = 1;
                        break;
                    case SBIG.ReadoutMode.BinNx2:
                        binX = 2;
                        break;
                    case SBIG.ReadoutMode.BinNx3:
                        binX = 3;
                        break;
                    case SBIG.ReadoutMode.NoBinning2:
                        binX = binY = 1;
                        binOffChip = true;
                        break;
                    case SBIG.ReadoutMode.Bin2x2VertOffChip:
                        binX = binY = 2;
                        binOffChip = true;
                        break;
                    case SBIG.ReadoutMode.Bin3x3VertOffChip:
                        binX = binY = 3;
                        binOffChip = true;
                        break;
                    case SBIG.ReadoutMode.Bin9x9:
                        binX = binY = 9;
                        break;
                    case SBIG.ReadoutMode.BinNxN:
                        break;
                }
                cameraInfo.ReadoutModeConfigs[i].ReadoutMode = readoutMode;
                cameraInfo.ReadoutModeConfigs[i].BinX = binX;
                cameraInfo.ReadoutModeConfigs[i].BinY = binY;
                cameraInfo.ReadoutModeConfigs[i].BinningOffChip = binOffChip;
                cameraInfo.ReadoutModeConfigs[i].ElectronsPerAdu = BCDToUInt(ccdInfo01.readoutInfo[i].gain) / 100.0d;
                cameraInfo.ReadoutModeConfigs[i].Width = ccdInfo01.readoutInfo[i].width;
                cameraInfo.ReadoutModeConfigs[i].Height = ccdInfo01.readoutInfo[i].height;
                cameraInfo.ReadoutModeConfigs[i].PixelWidthMicrons = BCDToUInt(ccdInfo01.readoutInfo[i].pixelWidth) / 100.0d;
                cameraInfo.ReadoutModeConfigs[i].PixelHeightMicrons = BCDToUInt(ccdInfo01.readoutInfo[i].pixelHeight) / 100.0d;
            }

            // Info from GetCcdInfoResults2
            var numBadColumns = Math.Min((ushort)4, ccdInfo2.badColumns);
            cameraInfo.BadColumns = new ushort[numBadColumns];
            for (int i = 0; i < numBadColumns; ++i) {
                cameraInfo.BadColumns[i] = ccdInfo2.columns[i];
            }
            cameraInfo.HasAntiBloomingGateProtection = ccdInfo2.imagingABG == SBIG.AntiBloomingGatePresence.HasABG;
            cameraInfo.SerialNumber = ccdInfo2.serialNumber;

            // Info from GetCcdInfoResults45
            cameraInfo.CcdFrameType = (ccdInfo45.capabilitiesBits & 0x1) == 0x1 ? CcdFrameType.FRAME_TRANSFER_CCD : CcdFrameType.FULL_FRAME_CCD;
            cameraInfo.HasElectronicShutter = (ccdInfo45.capabilitiesBits & 0x2) == 0x2;
            cameraInfo.SupportsExternalTracker = (ccdInfo45.capabilitiesBits & 0x4) == 0x4;
            cameraInfo.SupportsBTDI = (ccdInfo45.capabilitiesBits & 0x8) == 0x8;
            cameraInfo.HasAO8 = (ccdInfo45.capabilitiesBits & 0x10) == 0x10;
            cameraInfo.HasFrameBuffer = (ccdInfo45.capabilitiesBits & 0x20) == 0x20;
            cameraInfo.RequiresStartExposure2 = (ccdInfo45.capabilitiesBits & 0x40) == 0x40;
            cameraInfo.NumberExtraUnbinnedRows = ccdInfo45.dumpExtra;

            // Info from GetCcdInfoResults6
            cameraInfo.IsSTXL = (ccdInfo6.cameraBits & 0x1) == 0x1;
            cameraInfo.HasMechanicalShutter = (ccdInfo6.cameraBits & 0x2) == 0x2;
            if ((ccdInfo6.ccdBits & 0x1) == 0) {
                cameraInfo.CcdType = CcdType.MONO;
            } else if ((ccdInfo6.ccdBits & 0x2) == 0) {
                cameraInfo.CcdType = CcdType.BAYER_MATRIX;
            } else {
                cameraInfo.CcdType = CcdType.TRUSENSE_COLOR_MATRIX;
            }

            try {
                // This will only work for PixCel255/237 devices. Otherwise it is always 16-bit ADC
                var ccdInfo3 = UnivDrvCommand<SBIG.GetCcdInfoParams, SBIG.GetCcdInfoResults3>(
                    SBIG.Cmd.CC_GET_CCD_INFO, new SBIG.GetCcdInfoParams(SBIG.CcdInfoRequest.PixCel255_237Extended));
                if (ccdInfo3.a2dSize == SBIG.A2dSize.SixteenBits) {
                    cameraInfo.AdcBits = 16;
                } else if (ccdInfo3.a2dSize == SBIG.A2dSize.TwelveBits) {
                    cameraInfo.AdcBits = 12;
                } else {
                    Logger.Warning($"SBIGSDK: Unknown a2dSize reported by GetCcdInfoResults3. Assuming 16 bit ADC");
                    cameraInfo.AdcBits = 16;
                }
                cameraInfo.FilterType = ccdInfo3.filterType;
            } catch (SBIG.FailedOperation) {
                Logger.Info($"SBIGSDK: Failed to GetCcdInfoResults3. Device likely isn't Pixcel255/237. Assuming 16-bit ADC");
                // All other cameras are 16 bits
                cameraInfo.AdcBits = 16;
            }
            return cameraInfo;
        }

        /// <summary>
        /// Convenience method to extract a given ReadoutModeConfig from an array given the desired SBIG.ReadoutMode
        /// </summary>
        /// <param name="modeConfigs">The readout mode configurations to search</param>
        /// <param name="readoutMode">The SBIG readout mode to match</param>
        /// <returns>Null if the config isn't found, the first match otherwise</returns>
        public static ReadoutModeConfig? GetReadoutModeConfig(ReadoutModeConfig[] modeConfigs, SBIG.ReadoutMode readoutMode) {
            for (int i = 0; i < modeConfigs.Length; ++i) {
                if (modeConfigs[i].ReadoutMode.Mode == readoutMode) {
                    return modeConfigs[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Queries temperature status from the connected CCD camera. This uses a thread-safe cache with a 1-second TTL to simplify accessing the temperature information
        /// without overwhelming the device with one request per property per device poll
        /// </summary>
        /// <returns>The queried temperature status</returns>
        public SBIG.QueryTemperatureStatusResults2 QueryTemperatureStatus() {
            var serialNumber = openedCameraInfo.SerialNumber;
            return this.queryTemperatureStatusCache.GetOrAdd(
                serialNumber,
                () => UnivDrvCommand<SBIG.QueryTemperatureStatusParams, SBIG.QueryTemperatureStatusResults2>(
                SBIG.Cmd.CC_QUERY_TEMPERATURE_STATUS, new SBIG.QueryTemperatureStatusParams(SBIG.TempStatusRequest.TEMP_STATUS_ADVANCED2)),
                TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// Wraps the uber-method that issues a command to the universal SBIG driver. It uses a lock to ensure only one call is made at a time, which is required per the SDK documentation
        /// </summary>
        /// <param name="command">The command</param>
        /// <param name="Params">The input parameters. This should be IntPtr.Zero if the command doesn't take input, or a pinned GC handle to a memory location otherwise</param>
        /// <param name="results">Where to directly write the command output. This should be IntPtr.Zero if the command doesn't write output, or a pinned GC handle to a memory location otherwise</param>
        public void UnivDrvCommandDirect(SBIG.Cmd command, IntPtr Params, IntPtr results) {
            lock (driverLock) {
                SBIG.UnivDrvCommandDirect(command, Params, results);
            }
        }

        /// <summary>
        /// A convenience wrapper for UnivDrvCommand that passes input parameters directly if they are blittable (or an array of blittables), or marshals to a pinned GC handle otherwise.
        /// No return value is provided, so use this only for commands that don't return a result structure
        /// </summary>
        /// <typeparam name="P">The input parameter type</typeparam>
        /// <param name="command">The command</param>
        /// <param name="Params">The input parameters as a native CLR type</param>
        public void UnivDrvCommand<P>(SBIG.Cmd command, P Params) {
            lock (driverLock) {
                var paramsBlittable = (Params == null) || Blittable<P>.IsBlittable;
                if (paramsBlittable) {
                    SBIG.UnivDrvCommand(command, Params);
                } else {
                    SBIG.UnivDrvCommandMarshal(command, Params);
                }
            }
        }

        /// <summary>
        /// Another convenience wrapper for UnivDryCommand that returns data. If both input and output are blittable, then a more efficient version of UnivDrvCommand is used which doesn't
        /// marshal the data
        /// </summary>
        /// <typeparam name="P">The input parameter type</typeparam>
        /// <typeparam name="R">The output parameter type</typeparam>
        /// <param name="command">The command</param>
        /// <param name="Params">The input parameter as a native CLR type</param>
        /// <param name="pResults">The output parameter will be written directly to the given instance</param>
        public void UnivDrvCommand<P, R>(SBIG.Cmd command, P Params, R pResults) where R : new() {
            lock (driverLock) {
                if (Blittable<P>.IsBlittable && Blittable<R>.IsBlittable) {
                    SBIG.UnivDrvCommand(command, Params, pResults);
                } else if (Blittable<P>.IsBlittable) {
                    SBIG.UnivDrvCommand_OutComplex(command, Params, pResults);
                } else {
                    throw new NotSupportedException("Cannot use this version of UnivDrvCommand when both Params nor pResults aren't blittable. Use the other signature");
                }
            }
        }

        /// <summary>
        /// Yet another convenience wrapper similar, except a new instance of the return data are provided instead of writing inline to an existing one
        /// </summary>
        /// <typeparam name="P">The input parameter type</typeparam>
        /// <typeparam name="R">The output parameter type</typeparam>
        /// <param name="command">The command</param>
        /// <param name="Params">The input parameter as a native CLR type</param>
        /// <returns>The command output</returns>
        public R UnivDrvCommand<P, R>(SBIG.Cmd command, P Params) where R : new() {
            lock (driverLock) {
                var paramsBlittable = (Params == null) || Blittable<P>.IsBlittable;
                if (paramsBlittable && Blittable<R>.IsBlittable) {
                    return SBIG.UnivDrvCommand<R>(command, Params);
                } else if (paramsBlittable) {
                    var result = new R();
                    SBIG.UnivDrvCommand_OutComplex(command, Params, result);
                    return result;
                } else {
                    return SBIG.UnivDrvCommandMarshal<R>(command, Params);
                }
            }
        }

        /// <summary>
        /// Queries all cameras connected on the USB bus
        /// </summary>
        /// <returns>An array containing all connected cameras. This will be empty if there are no cameras.</returns>
        public CameraQueryInfo[] QueryUsbCameras() {
            var queryResults = UnivDrvCommand<object, SBIG.QueryUsbResults>(SBIG.Cmd.CC_QUERY_USB, (object)null);
            var queryInfos = new CameraQueryInfo[queryResults.camerasFound];
            for (int i = 0; i < queryResults.camerasFound; i++) {
                queryInfos[i].CameraType = queryResults.dev[i].cameraType;
                queryInfos[i].Name = queryResults.dev[i].name;
                queryInfos[i].SerialNumber = queryResults.dev[i].serialNumber;
                queryInfos[i].DeviceId = (SBIG.DeviceType)((ushort)SBIG.DeviceType.USB1 + i);
            }
            return queryInfos;
        }

        /// <summary>
        /// Enables temperature regulation at the given set point. If temperature regulation is already enabled, it updates the set point
        /// </summary>
        /// <param name="celcius">The set point</param>
        public void RegulateTemperature(double celcius) {
            // CONSIDER: AutoFreeze is an option for guiding cameras to reduce readout noise. It would be easy to support as an equipment option, but probably
            // won't be needed for regular imaging
            var tempRegulationParams = new SBIG.SetTemperatureRegulationParams2 {
                state = SBIG.TemperatureRegulation.On,
                ccdSetpointCelcius = celcius
            };
            UnivDrvCommand(SBIG.Cmd.CC_SET_TEMPERATURE_REGULATION2, tempRegulationParams);
            this.queryTemperatureStatusCache.Remove(openedCameraInfo.SerialNumber);
        }

        /// <summary>
        /// Disables temperature regulation if it is enabled, or does nothing otherwise
        /// </summary>
        public void DisableTemperatureRegulation() {
            var tempRegulationParams = new SBIG.SetTemperatureRegulationParams2 {
                state = SBIG.TemperatureRegulation.Off
            };
            UnivDrvCommand(SBIG.Cmd.CC_SET_TEMPERATURE_REGULATION2, tempRegulationParams);
            this.queryTemperatureStatusCache.Remove(openedCameraInfo.SerialNumber);
        }

        /// <summary>
        /// Gets the exposure state (Idle/In Progress/Complete), which is useful for polling to determine when to download the exposure
        /// </summary>
        /// <returns>A CommandState representing the exposure state</returns>
        public CommandState GetExposureState() {
            var status = UnivDrvCommand<SBIG.QueryCommandStatusParams, SBIG.QueryCommandStatusResults>(
                SBIG.Cmd.CC_QUERY_COMMAND_STATUS, new SBIG.QueryCommandStatusParams(SBIG.Cmd.CC_START_EXPOSURE));
            var rawState = ((ushort)status.status & 0x3);
            // The 2 least significant bits represent Ccd camera status. Bit 0 is "Complete" if set, and "In Progress" otherwise. Bit 1 is "Command Active" if set, and "Idle" otherwise.
            if ((rawState & 0x2) == 0) {
                return CommandState.IDLE;
            } else if ((rawState & 0x1) == 0) {
                return CommandState.IN_PROGRESS;
            } else {
                return CommandState.COMPLETE;
            }
        }

        /// <summary>
        /// Starts an exposure, and returns the SBIG parameters later needed to download the exposure
        /// </summary>
        /// <param name="readoutMode">The readout mode</param>
        /// <param name="darkFrame">Whether this is a dark frame. This will be used to close/open the internal shutter if one is available</param>
        /// <param name="exposureTimeSecs">Exposure time in seconds. The SDK supports hundredths of a second, so any additional precision is ignored</param>
        /// <param name="exposureStart">The X,Y coordinates representing a bounding box for the exposure. Unless ROI is used, this should be 0, 0</param>
        /// <param name="exposureSize">The width, height of a bounding box for the exposure. This should account for the amount of binning represented in the readout mode</param>
        /// <returns></returns>
        public SBIG.StartExposureParams2 StartExposure(ReadoutMode readoutMode, bool darkFrame, double exposureTimeSecs, Point exposureStart, Size exposureSize) {
            var targetShutterState = darkFrame ? SBIG.ShutterState.Close : SBIG.ShutterState.Open;
            var exposureTimeHundredths = (uint)Math.Round(exposureTimeSecs * 100.0f);
            var exposureParams = new SBIG.StartExposureParams2() {
                ccd = SBIG.CCD.Imaging,
                readoutMode = readoutMode.RawMode,
                abgState = SBIG.AbgState.Off,
                openShutter = targetShutterState,
                exposureTime = exposureTimeHundredths,
                top = (ushort)exposureStart.Y,
                left = (ushort)exposureStart.X,
                height = (ushort)exposureSize.Height,
                width = (ushort)exposureSize.Width
            };

            UnivDrvCommand<SBIG.StartExposureParams2>(SBIG.Cmd.CC_START_EXPOSURE2, exposureParams);
            return exposureParams;
        }

        /// <summary>
        /// Ends an exposure. This must be called before downloading an exposure (readout). This method is also used for aborting an exposure
        /// </summary>
        public void EndExposure() {
            UnivDrvCommand<SBIG.EndExposureParams>(SBIG.Cmd.CC_END_EXPOSURE, new SBIG.EndExposureParams(SBIG.CCD.Imaging));
        }

        /// <summary>
        /// Downloads an exposure
        /// </summary>
        /// <param name="exposureParams">Exposure parameters returned by StartExposure</param>
        /// <param name="ct">The cancellation token</param>
        /// <returns>Flat array of 16-bit pixel data</returns>
        public ushort[] DownloadExposure(SBIG.StartExposureParams2 exposureParams, CancellationToken ct) {
            // Only one exposure can be readout at a time, so the exposureLock ensures no interlacing occurs
            lock (exposureLock) {
                var readoutParams = new SBIG.StartReadoutParams() {
                    ccd = SBIG.CCD.Imaging,
                    readoutMode = exposureParams.readoutMode,
                    left = exposureParams.left,
                    top = exposureParams.top,
                    width = exposureParams.width,
                    height = exposureParams.height,
                };
                UnivDrvCommand(SBIG.Cmd.CC_START_READOUT, readoutParams);

                var data = new ushort[exposureParams.width * exposureParams.height];
                var dataGcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
                var pinnedPtr = dataGcHandle.AddrOfPinnedObject();
                var readoutLineParams = new SBIG.ReadoutLineParams() {
                    ccd = SBIG.CCD.Imaging,
                    pixelStart = exposureParams.left,
                    pixelLength = exposureParams.width,
                    readoutMode = exposureParams.readoutMode
                };

                var pinnedParams = GCHandle.Alloc(readoutLineParams, GCHandleType.Pinned);
                try {
                    for (int i = 0; i < exposureParams.height; i++) {
                        if (ct.IsCancellationRequested) {
                            throw new OperationCanceledException();
                        }
                        UnivDrvCommandDirect(SBIG.Cmd.CC_READOUT_LINE, pinnedParams.AddrOfPinnedObject(), pinnedPtr + (i * exposureParams.width * sizeof(ushort)));
                    }                    
                } finally {
                    pinnedParams.Free();
                    dataGcHandle.Free();
                }

                return data;
            }
        }
    }
}
