#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Profile.Interfaces;
using QHYCCD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Image.ImageData;
using Nito.AsyncEx;
using NINA.Core.Enum;
using NINA.Core.Model.Equipment;
using NINA.Core.Locale;
using NINA.Image.Interfaces;
using NINA.Equipment.Model;
using NINA.Equipment.Interfaces;

namespace NINA.Equipment.Equipment.MyCamera {

    public class QHYCamera : BaseINPC, ICamera {
        private static readonly TimeSpan COOLING_TIMEOUT = TimeSpan.FromSeconds(2);
        private AsyncObservableCollection<BinningMode> _binningModes;
        private bool _connected = false;
        private short _readoutModeForNormalImages = 0;
        private short _readoutModeForSnapImages = 0;
        private Task coolerTask;
        private CancellationTokenSource coolerWorkerCts;
        private Task sensorStatsTask;
        private CancellationTokenSource sensorStatsCts;
        private QhySdk.QHYCCD_CAMERA_INFO Info;
        private readonly IProfileService profileService;
        private CancellationTokenSource downloadExposureTaskCTS;
        private Task<IExposureData> downloadExposureTask;
        public IQhySdk Sdk { get; set; } = QhySdk.Instance;

        public QHYCamera(uint cameraIdx, IProfileService profileService) {
            this.profileService = profileService;

            StringBuilder cameraId = new StringBuilder(QhySdk.QHYCCD_ID_LEN);
            StringBuilder cameraModel = new StringBuilder(0);

            /*
             * Camera model long form, eg: "QHY183C-c915484fa76ea7552"
             * The QHY SDK uses this to internally identify connected cameras
             * and this we need this to create a handle for our camera.
             */
            Sdk.GetId(cameraIdx, cameraId);

            /*
             * Camera model short form, eg: "QHY183C"
             * We use this to put in the camera equipment selection menu
             * rather than the long form above.
             */
            Sdk.GetModel(cameraId, cameraModel);

            Name = cameraModel.ToString();
            Info.Index = cameraIdx;
            Info.Id = cameraId;

            Logger.Debug($"QHYCCD: Found camera {Info.Id}");
        }

        public string Category { get; } = "QHYCCD";

        private List<int> SupportedBinFactors {
            get {
                if (Info.SupportedBins == null) {
                    var supportedBins = new List<int>();
                    if (Sdk.IsControl(QhySdk.CONTROL_ID.CAM_BIN1X1MODE)) {
                        supportedBins.Add(1);
                    }

                    if (Sdk.IsControl(QhySdk.CONTROL_ID.CAM_BIN2X2MODE)) {
                        supportedBins.Add(2);
                    }

                    if (Sdk.IsControl(QhySdk.CONTROL_ID.CAM_BIN3X3MODE)) {
                        supportedBins.Add(3);
                    }

                    if (Sdk.IsControl(QhySdk.CONTROL_ID.CAM_BIN4X4MODE)) {
                        supportedBins.Add(4);
                    }
                    Info.SupportedBins = supportedBins;
                }
                return Info.SupportedBins;
            }
        }

        public int BatteryLevel => -1;

        public AsyncObservableCollection<BinningMode> BinningModes {
            get {
                if (_binningModes == null) {
                    _binningModes = new AsyncObservableCollection<BinningMode>();
                    foreach (int f in SupportedBinFactors) {
                        /*
                         * QHY cameras are known to support only symmetrical bin modes
                         */
                        _binningModes.Add(new BinningMode((short)f, (short)f));
                    }
                }
                return _binningModes;
            }
        }

        public short BinX {
            get => Info.CurBin;
            set => Info.CurBin = value;
        }

        /// <summary>
        // Setting the Y pixel of a bin mode is redundant as QHY cameras
        // (currently) offer symmetrical bin modes only. Thus operations for
        // setting the Y pixes are no-ops, and getting Y pixel bin modes defer
        // to the getters for X pixels.
        /// </summary>
        public short BinY {
            get => BinX;
            set {
            }
        }

        public int BitDepth {
            get => (int)Info.Bpp;
        }

        public string CameraState {
            get => Info.CamState;
            set => Info.CamState = value;
        }

        public int CameraXSize {
            get => unchecked((int)Info.CurImage.SizeX);
            private set {
                Logger.Debug($"QHYCCD: Setting CameraXSize to {value}");
                Info.CurImage.SizeX = unchecked((uint)value);
                RaisePropertyChanged();
            }
        }

        public int CameraYSize {
            get => unchecked((int)Info.CurImage.SizeY);
            private set {
                Logger.Debug($"QHYCCD: Setting CameraYSize to {value}");
                Info.CurImage.SizeY = unchecked((uint)value);
                RaisePropertyChanged();
            }
        }

        public bool CanGetGain {
            get {
                if (Connected) {
                    return Info.HasGain;
                }

                return false;
            }
        }

        public bool CanSetGain {
            get {
                if (Connected) {
                    return Info.HasGain;
                }

                return false;
            }
        }

        public bool CanSetOffset {
            get {
                if (Connected) {
                    return Info.HasOffset;
                }

                return false;
            }
        }

        public bool CanSetTemperature {
            get {
                if (Connected) {
                    return Info.HasCooler;
                }

                return false;
            }
        }

        public bool CanSetUSBLimit {
            get {
                if (Connected) {
                    return Info.HasUSBTraffic;
                }

                return false;
            }
        }

        public bool CanFastReadout {
            get {
                if (Connected) {
                    return Info.HasReadoutSpeed;
                }

                return false;
            }
            private set => Info.HasReadoutSpeed = value;
        }

        public bool CanShowLiveView => false;
        public bool CanSubSample => true;

        public bool Connected {
            get => _connected;
            set {
                _connected = value;
                RaisePropertyChanged();
            }
        }

        public bool CoolerOn {
            get => Info.CoolerOn;
            set {
                if (Connected && CanSetTemperature) {
                    Logger.Debug(string.Format("QHYCCD: Cooler turned {0}", value ? "ON" : "OFF"));

                    Info.CoolerOn = value;
                    RaisePropertyChanged();
                }
            }
        }

        public double CoolerPower {
            get {
                double rv = double.NaN;

                if (Connected && CanSetTemperature) {
                    if ((rv = Sdk.GetControlValue(QhySdk.CONTROL_ID.CONTROL_CURPWM)) != QhySdk.QHYCCD_ERROR) {
                        /*
                         * This needs to be returned as a percentage of Info.CoolerPwmMax.
                         */
                        return (rv / Info.CoolerPwmMax) * 100;
                    }
                }

                return rv;
            }
        }

        public string Description => Info.Id.ToString();

        public bool DewHeaterOn {
            get => false;
            set {
            }
        }

        public string DriverInfo => "Native driver for QHYCCD cameras";
        public string DriverVersion => string.Empty;
        public bool EnableSubSample { get; set; }
        public double ExposureMax => Info.ExpMax / 1e6;
        public double ElectronsPerADU => double.NaN;

        /// <summary>
        // We store the camera's exposure times in microseconds
        // but we present this number as seconds to those who ask
        /// </summary>
        public double ExposureMin => Info.ExpMin / 1e6;

        public int Gain {
            get {
                if (Connected && CanGetGain) {
                    double rv;

                    if (Info.HasUnreliableGain) {
                        rv = Info.CurGain;
                    } else {
                        rv = Sdk.GetControlValue(QhySdk.CONTROL_ID.CONTROL_GAIN);
                    }

                    return unchecked((int)rv);
                }

                return 1;
            }
            set {
                if (Connected && CanSetGain) {
                    if (Sdk.SetControlValue(QhySdk.CONTROL_ID.CONTROL_GAIN, value)) {
                        Info.CurGain = value;
                        RaisePropertyChanged();
                    }
                }
            }
        }

        public int GainMax => Info.GainMax;
        public int GainMin => Info.GainMin;
        public IList<int> Gains => new List<int>();
        public bool HasBattery => false;
        public bool HasDewHeater => false;
        public bool HasSetupDialog => false;

        public bool HasShutter {
            get {
                if (Connected) {
                    return Info.HasShutter;
                }

                return false;
            }

            private set => Info.HasShutter = value;
        }

        public string Id => $"{Description}";
        public bool LiveViewEnabled { get => false; set => throw new NotImplementedException(); }

        public short MaxBinX => (short)SupportedBinFactors.DefaultIfEmpty(1).Max();
        public short MaxBinY => MaxBinX;

        public string Name {
            get => Info.Model.ToString();
            set {
                Info.Model = new StringBuilder(value);
                RaiseAllPropertiesChanged();
            }
        }

        public int Offset {
            get {
                if (Connected) {
                    double rv;

                    if ((rv = Sdk.GetControlValue(QhySdk.CONTROL_ID.CONTROL_OFFSET)) != QhySdk.QHYCCD_ERROR) {
                        if (Info.InflatedOff != 0) {
                            return unchecked((int)(rv - Info.InflatedOff));
                        } else {
                            return unchecked((int)rv);
                        }
                    }
                }

                return 0;
            }
            set {
                if (Connected && CanSetOffset) {
                    if (Sdk.SetControlValue(QhySdk.CONTROL_ID.CONTROL_OFFSET, value)) {
                        RaisePropertyChanged();
                    }
                }
            }
        }

        public int OffsetMax => Info.OffMax;
        public int OffsetMin => Info.OffMin;

        public double PixelSizeX {
            get => Info.PixelX;
            private set {
                Logger.Debug($"QHYCCD: Setting PixelSizeX to {value}");
                Info.PixelX = value;
                RaisePropertyChanged();
            }
        }

        public double PixelSizeY {
            get => Info.PixelY;
            private set {
                Logger.Debug($"QHYCCD: Setting PixelSizeY to {value}");
                Info.PixelY = value;
                RaisePropertyChanged();
            }
        }

        public short ReadoutModeForNormalImages {
            get => _readoutModeForNormalImages;
            set {
                if (value >= 0 && value < ReadoutModes.Count) {
                    _readoutModeForNormalImages = value;
                } else {
                    _readoutModeForNormalImages = 0;
                }

                RaisePropertyChanged();
            }
        }

        public short ReadoutModeForSnapImages {
            get => _readoutModeForSnapImages;
            set {
                if (value >= 0 && value < ReadoutModes.Count) {
                    _readoutModeForSnapImages = value;
                } else {
                    _readoutModeForSnapImages = 0;
                }

                RaisePropertyChanged();
            }
        }

        public short ReadoutMode {
            get {
                uint mode = 0;
                uint rv;

                if (Connected) {
                    if (CanFastReadout) {
                        mode = (uint)Sdk.GetControlValue(QhySdk.CONTROL_ID.CONTROL_SPEED);
                    } else {
                        if ((rv = Sdk.GetReadMode(ref mode)) != QhySdk.QHYCCD_SUCCESS) {
                            Logger.Error($"QHYCCD: GetQHYCCDReadMode() failed. Returned {rv}");
                            return 0;
                        }
                    }

                    return (short)mode;
                } else {
                    return 0;
                }
            }
            set {
                if (value >= 0 && value < ReadoutModes.Count) {
                    uint rv;
                    string modeName = ReadoutModes[value];
                    uint mode = (uint)value;

                    if (Connected) {
                        if (CanFastReadout) {
                            if (mode >= Info.ReadoutSpeedMin && value <= Info.ReadoutSpeedMax) {
                                Logger.Debug($"QHYCCD: ReadoutMode: Setting readout speed to {mode} ({modeName})");
                                Sdk.SetControlValue(QhySdk.CONTROL_ID.CONTROL_SPEED, mode);
                            } else {
                                Logger.Error($"QHYCCD: Invalid readout speed: {mode}");
                                return;
                            }
                        } else {
                            if (mode != ReadoutMode) {
                                if (mode < 0 && mode > ReadoutModes.Count - 1) {
                                    Logger.Error($"QHYCCD: Invalid readout mode {mode}");
                                    return;
                                }

                                Logger.Debug($"QHYCCD: ReadoutMode: Setting readout mode to {mode} ({modeName})");

                                if ((rv = Sdk.SetReadMode(mode)) != QhySdk.QHYCCD_SUCCESS) {
                                    Logger.Error($"QHYCCD: SetQHYCCDReadMode() failed. Returned {rv}");
                                    return;
                                }

                                Sdk.InitCamera();
                                SetImageResolution();
                            }
                        }
                    }
                } else {
                    Logger.Warning($"QHYCCD: ReadoutMode: Index for readoutmode does not exist {value}");
                }
            }
        }

        public IList<string> ReadoutModes {
            get => Info.ReadoutModes;
            set => Info.ReadoutModes = value;
        }

        public string SensorName => string.Empty;

        public SensorType SensorType { get; private set; } = SensorType.Monochrome;

        public int SubSampleHeight { get; set; }
        public int SubSampleWidth { get; set; }
        public int SubSampleX { get; set; }
        public int SubSampleY { get; set; }

        public double Temperature {
            get {
                double rv = double.NaN;

                if (Connected && Info.HasChipTemp) {
                    if ((rv = Sdk.GetControlValue(QhySdk.CONTROL_ID.CONTROL_CURTEMP)) != QhySdk.QHYCCD_ERROR)
                        return rv;
                }

                return rv;
            }
        }

        public double TemperatureSetPoint {
            get {
                if (Connected && CanSetTemperature) {
                    return Info.CoolerTargetTemp;
                } else {
                    return double.NaN;
                }
            }
            set {
                if (Connected && CanSetTemperature) {
                    Logger.Debug($"QHYCCD: Cooler target temperature set to {value}");
                    Info.CoolerTargetTemp = value;
                    RaisePropertyChanged();
                }
            }
        }

        public int USBLimit {
            get {
                double rv;

                if (Connected && ((rv = Sdk.GetControlValue(QhySdk.CONTROL_ID.CONTROL_USBTRAFFIC)) != QhySdk.QHYCCD_ERROR))
                    return unchecked((int)rv);

                return -1;
            }
            set {
                if (Connected && CanSetUSBLimit) {
                    if (Sdk.SetControlValue(QhySdk.CONTROL_ID.CONTROL_USBTRAFFIC, value))
                        RaisePropertyChanged();
                }
            }
        }

        public int USBLimitMax => unchecked((int)Info.USBMax);

        public int USBLimitMin => unchecked((int)Info.USBMin);

        public int USBLimitStep => unchecked((int)Info.USBStep);

        /// <summary>
        // QHY cameras have two eras of firmware when it comes to managing cooling.
        // "Old" camera firmware requires that the target temperature or PWM be set at regular, constant
        // intervals, usually every 2 seconds. "New" camera firmware does not have this requirement, but
        // there is no programmatic way to determine which firmware is old and which firmware is new. As
        // a result, QHY's own suggestion is to just treat every camera as if it is running old firmware
        // and set the target temperature every 2 seconds.
        //
        // To manage this, we will create a task to run constantly in the background while CoolerOn = true.
        /// </summary>
        private async Task CoolerWorker(CancellationToken ct) {
            try {
                bool previous = Info.CoolerOn;

                Logger.Debug("QHYCCD: CoolerWorker task started");
                while (!ct.IsCancellationRequested) {
                    if (Info.CoolerOn) {
                        Logger.Trace($"QHYCCD: CoolerWorker setting camera target temp to {Info.CoolerTargetTemp}");
                        Sdk.ControlTemp(Info.CoolerTargetTemp);
                    } else if (previous == true) {
                        Logger.Debug("QHYCCD: CoolerWorker turning off TEC due user request");
                        _ = Sdk.SetControlValue(QhySdk.CONTROL_ID.CONTROL_MANULPWM, 0);
                    }

                    previous = Info.CoolerOn;

                    /* sleep (cancelable) */
                    await Task.Delay(QhySdk.QHYCCD_COOLER_DELAY, ct);
                }
            } catch (OperationCanceledException) {
                Logger.Debug("QHYCCD: CoolerWorker task cancelled");
            }
        }

        private async Task SensorStatsWorker(CancellationToken ct) {
            try {
                Logger.Debug("QHYCCD: SensorStatsWorker task started");

                while (!ct.IsCancellationRequested) {
                    if (QhyHasSensorAirPressure) {
                        QhySensorAirPressure = GetQhySensorAirPressure();
                    }

                    if (QhyHasSensorHumidity) {
                        QhySensorHumidity = GetQhySensorHumidity();
                    }

                    /* sleep (cancelable) */
                    await Task.Delay(TimeSpan.FromSeconds(10), ct);
                }
            } catch (OperationCanceledException) {
                Logger.Debug("QHYCCD: SensorStatsWorker task cancelled");
            }
        }

        private bool GetSensorType() {
            Info.BayerPattern = Sdk.GetBayerType();

            switch (Info.BayerPattern) {
                case QhySdk.BAYER_ID.BAYER_GB:
                    SensorType = SensorType.GBRG;
                    break;

                case QhySdk.BAYER_ID.BAYER_GR:
                    SensorType = SensorType.GRBG;
                    break;

                case QhySdk.BAYER_ID.BAYER_BG:
                    SensorType = SensorType.BGGR;
                    break;

                case QhySdk.BAYER_ID.BAYER_RG:
                    SensorType = SensorType.RGGB;
                    break;

                default:
                    return false;
            }
            return true;
        }

        public short BayerOffsetX => 0;
        public short BayerOffsetY => 0;

        public void AbortExposure() {
            StopExposure();
        }

        public Task<bool> Connect(CancellationToken ct) {
            return Task.Run(() => ConnectSync(), ct);
        }

        private void CancelCoolingSync() {
            if (!Connected || !Info.HasCooler) {
                return;
            }

            Logger.Debug("QHYCCD: Terminating CoolerWorker task");

            CoolerOn = false;
            coolerWorkerCts.Cancel();
            coolerWorkerCts.Dispose();
            try {
                using (var timeoutSource = new CancellationTokenSource(COOLING_TIMEOUT)) {
                    coolerTask?.Wait(timeoutSource.Token);
                }
            } catch (Exception ex) {
                Logger.Error($"QHYCCD: Cooling thread failed to terminate within {COOLING_TIMEOUT}", ex);
            } finally {
                coolerWorkerCts = null;
                coolerTask = null;
            }

            /* CoolerWorker task was killed. Make sure the TEC is turned off before closing the camera. */
            Logger.Debug("QHYCCD: CoolerWorker task cancelled, turning off TEC");
            _ = Sdk.SetControlValue(QhySdk.CONTROL_ID.CONTROL_MANULPWM, 0);
        }

        private void CancelSensorStatsSync() {
            if (!Connected && !QhyHasSensorHumidity && !QhyHasSensorAirPressure) {
                return;
            }

            Logger.Debug("QHYCCD: Terminating SensorStatsWorker task");

            sensorStatsCts.Cancel();
            sensorStatsCts.Dispose();
            try {
                using (var timeoutSource = new CancellationTokenSource(COOLING_TIMEOUT)) {
                    sensorStatsTask?.Wait(timeoutSource.Token);
                }
            } catch (Exception ex) {
                Logger.Error($"QHYCCD: SensorStats thread failed to terminate within {COOLING_TIMEOUT}", ex);
            } finally {
                sensorStatsCts = null;
                sensorStatsTask = null;
            }
        }

        private void ThrowOnFailure(string op, uint result) {
            if (result != QhySdk.QHYCCD_SUCCESS) {
                throw new Exception($"QHYCCD: {op} failed");
            }
        }

        public bool ConnectSync() {
            if (Connected) {
                return true;
            }

            var success = false;
            double min = 0, max = 0, step = 0;
            List<string> modeList = new List<string>();
            StringBuilder cameraID = new StringBuilder(QhySdk.QHYCCD_ID_LEN);
            StringBuilder modeName = new StringBuilder(0);
            uint num_modes = 0;

            try {
                Sdk.InitSdk();

                Logger.Info($"QHYCCD: Connecting to {Info.Id}");

                /*
                 * Get our selected camera's ID from the SDK
                 */
                Sdk.GetId(Info.Index, cameraID);

                /*
                 * CameraP is the handle we use to reference this camera
                 * from now on.
                 */
                Sdk.Open(cameraID);
                ThrowOnFailure("SetQHYCCDStreamMode", Sdk.SetStreamMode((byte)QhySdk.QHYCCD_CAMERA_MODE.SINGLE_EXPOSURE));

                /*
                 * Initialize the camera and make it available for use
                 */
                Sdk.InitCamera();

                SetImageResolution();

                /*
                 * Is this a color sensor or not?
                 * If so, do not debayer the image data
                 */
                if (GetSensorType() == true) {
                    Logger.Info($"QHYCCD: Color camera detected (pattern = {Info.BayerPattern}). Setting debayering to off");
                    ThrowOnFailure("SetQHYCCDDebayerOnOff", Sdk.SetDebayerOnOff(false));
                    Info.IsColorCam = true;
                } else {
                    Info.IsColorCam = false;
                }

                /*
                 * See if the camera offers a fast readout speed
                 * Readout speeds and readout modes are mutually exclusive and no camera should have both.
                 */
                if ((CanFastReadout = Sdk.IsControl(QhySdk.CONTROL_ID.CONTROL_SPEED)) == true) {
                    ThrowOnFailure("GetQHYCCDParamMinMaxStep", Sdk.GetParamMinMaxStep(QhySdk.CONTROL_ID.CONTROL_SPEED, ref min, ref max, ref step));

                    Info.ReadoutSpeedMin = (uint)min;
                    Info.ReadoutSpeedMax = (uint)max;
                    Info.ReadoutSpeedStep = (uint)step;
                    Logger.Debug($"QHYCCD: ReadoutSpeedMin={Info.ReadoutSpeedMin}, ReadoutSpeedMax={Info.ReadoutSpeedMax}, ReadoutSpeedStep={Info.ReadoutSpeedStep}");

                    modeList.Add(Loc.Instance["LblNormal"]);
                    modeList.Add(Loc.Instance["LblFast"]);
                } else {
                    /*
                     * See if this camera has any readout modes and build a list of their names if so
                     */
                    ThrowOnFailure("GetQHYCCDNumberOfReadModes", Sdk.GetNumberOfReadModes(ref num_modes));
                    Logger.Debug($"QHYCCD: Camera has {num_modes} readout mode(s)");

                    /*
                     * Every camera always has 1 readout mode. We are only interested in getting names for ones that have more than 1
                     */
                    if (num_modes > 1) {
                        for (uint i = 0; i < num_modes; i++) {
                            ThrowOnFailure("GetQHYCCDReadModeName", Sdk.GetReadModeName(i, modeName));
                            Logger.Debug($"QHYCCD: Found readout mode \"{modeName}\"");
                            modeList.Add(modeName.ToString());
                        }
                    }
                }

                if (modeList.Count == 0) {
                    modeList.Add("Default");
                }

                Logger.Debug($"QHYCCD: Readout mode names: {string.Join(", ", modeList)}");
                ReadoutModes = modeList;

                /*
                 * Get our min and max shutter speed (exposure times)
                 * The QHY SDK reports this value in microseconds (us)
                 */
                ThrowOnFailure("GetQHYCCDParamMinMaxStep(Exposure)", Sdk.GetParamMinMaxStep(QhySdk.CONTROL_ID.CONTROL_EXPOSURE, ref min, ref max, ref step));

                Info.ExpMin = min;
                Info.ExpMax = max;
                Info.ExpStep = step;
                Logger.Debug($"QHYCCD: ExpMin={Info.ExpMin}, ExpMax={Info.ExpMax}, ExpStep={Info.ExpStep}");

                /*
                 * Get our min and max gain
                 */
                ThrowOnFailure("GetQHYCCDParamMinMaxStep(Gain)", Sdk.GetParamMinMaxStep(QhySdk.CONTROL_ID.CONTROL_GAIN, ref min, ref max, ref step));

                Info.GainMin = (short)min;
                Info.GainMax = (short)max;
                Info.GainStep = step;
                Logger.Debug($"QHYCCD: GainMin={Info.GainMin}, GainMax={Info.GainMax}, GainStep={Info.GainStep}");

                /*
                 * Check for gain setting bugs
                 */
                Info.HasUnreliableGain = QuirkUnreliableGain();

                /*
                 * If we have to keep track of gain ourselves, initialize the gain to Info.GainMin
                 */
                if (Info.HasUnreliableGain == true) {
                    Gain = Info.GainMin;
                }

                /*
                 * Get our min and max offset
                 */
                ThrowOnFailure("GetQHYCCDParamMinMaxStep(Offset)", Sdk.GetParamMinMaxStep(QhySdk.CONTROL_ID.CONTROL_OFFSET, ref min, ref max, ref step));

                Info.OffMin = (int)min;
                Info.OffMax = (int)max;
                Info.OffStep = step;
                Logger.Debug($"QHYCCD: OffMin={Info.OffMin}, OffMax={Info.OffMax}, OffStep={Info.OffStep}");

                QuirkInflatedOffset();

                /*
                 * Fetch our min and max PWM settings for
                 * the cooler.
                 */
                Info.HasCooler = Sdk.IsControl(QhySdk.CONTROL_ID.CONTROL_COOLER);
                Info.HasChipTemp = Sdk.IsControl(QhySdk.CONTROL_ID.CONTROL_CURTEMP);

                if (Info.HasCooler) {
                    // Ignore the return code due to a bug in the SDK causes this to return a failure even if it succeeds
                    Sdk.GetParamMinMaxStep(QhySdk.CONTROL_ID.CONTROL_MANULPWM, ref min, ref max, ref step);

                    Info.CoolerPwmMin = min;
                    Info.CoolerPwmMax = max;
                    Info.CoolerPwmStep = step;
                    Logger.Debug($"QHYCCD: CoolerPwmMin={Info.CoolerPwmMin}, CoolerPwmMax={Info.CoolerPwmMax}, CoolerPwmStep={Info.CoolerPwmStep}");

                    /*
                     * Initialize cooler's target temperature to 0C
                     */
                    Info.CoolerTargetTemp = 0;

                    /*
                     * Force any TEC cooler to off upon startup
                     */
                    CoolerOn = false;

                    /*
                     * Start the thread that operates the TEC
                     * This thread will operate the TEC in accordance with the user turning the cooler on or off
                     * and program the camera's TEC to cool to the desired temperature. This thread will operate
                     * for as long as the camera is connected.
                     */
                    Logger.Debug("QHYCCD: Starting CoolerWorker task");
                    CancelCoolingSync();
                    coolerWorkerCts = new CancellationTokenSource();
                    coolerTask = CoolerWorker(coolerWorkerCts.Token);
                }

                /*
                 * QHY SDK offers no way to get the current bin mode! So we track
                 * it manually using QHYCCD_CAMERA_INFO.CurBin. We initialize the
                 * camera with 1x1 binning.
                 */
                Info.CurBin = 1;
                SetBinning(Info.CurBin, Info.CurBin);

                HasShutter = Sdk.IsControl(QhySdk.CONTROL_ID.CAM_MECHANICALSHUTTER);
                Info.HasGain = Sdk.IsControl(QhySdk.CONTROL_ID.CONTROL_GAIN);
                Info.HasOffset = Sdk.IsControl(QhySdk.CONTROL_ID.CONTROL_OFFSET);

                /*
                 * Fetch our min and max USB bandwidth settings. The QHY163M
                 * changes whether CONTROL_USBTRAFFIC based on the StreamMode,
                 * so recompute this when reconnecting.
                 */
                Info.HasUSBTraffic = Sdk.IsControl(QhySdk.CONTROL_ID.CONTROL_USBTRAFFIC);

                if (Info.HasUSBTraffic) {
                    ThrowOnFailure("GetQHYCCDParamMinMaxStep(USBTraffic)", Sdk.GetParamMinMaxStep(QhySdk.CONTROL_ID.CONTROL_USBTRAFFIC, ref min, ref max, ref step));

                    Info.USBMin = min;
                    Info.USBMax = max;
                    Info.USBStep = step;
                    Logger.Debug($"QHYCCD: USBMin={Info.USBMin}, USBMax={Info.USBMax}, USBStep={Info.USBStep}");

                    if (QuirkNoUSBTraffic()) {
                        Info.HasUSBTraffic = false;
                    }
                }

                /*
                 * Detect if this camera has sensor chamber air pressure and humidity sensors
                 */
                QhyHasSensorAirPressure = Sdk.IsControl(QhySdk.CONTROL_ID.CAM_PRESSURE);
                QhyHasSensorHumidity = Sdk.IsControl(QhySdk.CONTROL_ID.CAM_HUMIDITY);

                if (QhyHasSensorAirPressure || QhyHasSensorHumidity) {
                    Logger.Debug("QHYCCD: Starting SensorStatsWorker task");

                    sensorStatsCts = new CancellationTokenSource();
                    sensorStatsTask = SensorStatsWorker(coolerWorkerCts.Token);
                }

                QhyFirmwareVersion = GetFirmwareVersion();
                QhyFPGAVersion = GetFPGAVersion();
                QhySdkVersion = GetSdkVersion();

                /*
                 * Check the USB driver version and emit a warning if it's below the recommended minimum version
                 */
                DriverVersionCheck();

                /*
                 * Announce that this camera is now initialized and ready
                 */
                CameraState = QhySdk.QHYCCD_CAMERA_STATE.IDLE.ToString();
                Connected = true;
                success = true;

                RaisePropertyChanged(nameof(Connected));
                RaiseAllPropertiesChanged();
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(ex.Message);
                Disconnect();
            }
            return success;
        }

        public void Disconnect() {
            if (!Connected) {
                return;
            }

            try {
                Connected = false;

                CancelCoolingSync();
                CancelSensorStatsSync();

                Logger.Info($"QHYCCD: Closing camera {Info.Id}");
                Sdk.Close();
                Sdk.ReleaseSdk();
            } catch (Exception ex) {
                Logger.Error("QHYCCD: Failed to disconnect", ex);
            }
        }

        public async Task WaitUntilExposureIsReady(CancellationToken token) {
            RaiseIfNotConnected();
            using (token.Register(() => { AbortExposure(); downloadExposureTaskCTS.Cancel(); })) {
                if (downloadExposureTask != null) {
                    await downloadExposureTask;
                }
            }
        }

        private Task<IExposureData> StartDownloadExposure(CancellationToken ct) {
            return Task.Run<IExposureData>(async () => {
                uint width = 0;
                uint height = 0;
                uint bpp = 0;
                uint channels = 0;
                uint rv;

                Logger.Debug("QHYCCD: Downloading exposure...");

                while (Sdk.GetExposureRemaining() > 0) {
                    await Task.Delay(100, ct);
                }

                bool is16bit = Info.Bpp > 8;

                /*
                 * Size the image data byte array for the image
                 */
                uint numPixels = is16bit ? ImageSize / 2U : ImageSize;
                ushort[] ImgData = new ushort[numPixels];

                /*
                 * Download the image from the camera
                 */
                CameraState = QhySdk.QHYCCD_CAMERA_STATE.DOWNLOADING.ToString();
                if (is16bit) {
                    rv = Sdk.GetSingleFrame(ref width, ref height, ref bpp, ref channels, ImgData);
                } else {
                    byte[] ImgDataBytes = new byte[numPixels];
                    rv = Sdk.GetSingleFrame(ref width, ref height, ref bpp, ref channels, ImgDataBytes);
                    for (int i = 0; i < ImgDataBytes.Length; i++) {
                        ImgData[i] = ImgDataBytes[i];
                    }
                }
                if (rv != QhySdk.QHYCCD_SUCCESS) {
                    Logger.Warning($"QHYCCD: Failed to download image from camera! rv = {rv }");
                    throw new Exception(Loc.Instance["LblASIImageDownloadError"]);
                }

                Logger.Debug($"QHYCCD: Downloaded image: {width}x{height}, {bpp} bpp, {channels} channels");

                CameraState = QhySdk.QHYCCD_CAMERA_STATE.IDLE.ToString();

                return new ImageArrayExposureData(
                    input: ImgData,
                    width: (int)width,
                    height: (int)height,
                    bitDepth: BitDepth,
                    isBayered: SensorType != SensorType.Monochrome && (BinX == 1 && BinY == 1),
                    metaData: new ImageMetaData());
            }, ct);
        }

        public async Task<IExposureData> DownloadExposure(CancellationToken ct) {
            using (ct.Register(() => downloadExposureTaskCTS.Cancel())) {
                return await downloadExposureTask.WaitAsync(ct);
            }
        }

        private void RaiseIfNotConnected() {
            // Check for connection status and raise exceptions explicitly since using invalid CameraP handles can cause hard application crashes
            if (!Connected) {
                throw new Exception("QHYCCD is not connected");
            }
        }

        public Task<IExposureData> DownloadLiveView(CancellationToken ct) {
            throw new NotImplementedException();
        }

        public void SetBinning(short x, short y) {
            Logger.Debug($"QHYCCD: Setting bin mode to {x}x{y}");
            BinX = x;
        }

        public void SetupDialog() {
        }

        private bool SetResolution(out uint startx, out uint starty, out uint sizex, out uint sizey) {
            /* ROI coordinates and resolution */
            if (EnableSubSample == true) {
                startx = (uint)SubSampleX / (uint)BinX;
                starty = (uint)SubSampleY / (uint)BinY;
                sizex = (uint)SubSampleWidth / (uint)BinX;
                sizey = (uint)SubSampleHeight / (uint)BinY;
            } else {
                startx = StartPixelX / (uint)BinX;
                starty = StartPixelY / (uint)BinY;
                sizex = (uint)CameraXSize / (uint)BinX;
                sizey = (uint)CameraYSize / (uint)BinY;
            }

            uint rv;
            Logger.Debug($"QHYCCD: Setting image resolution: startx={startx}, starty={starty}, sizex={sizex}, sizey={sizey}");
            if ((rv = Sdk.SetResolution(startx, starty, sizex, sizey)) != QhySdk.QHYCCD_SUCCESS) {
                Logger.Warning($"QHYCCD: Failed to set exposure resolution: rv = {rv}");
                CameraState = QhySdk.QHYCCD_CAMERA_STATE.ERROR.ToString();
                return false;
            }
            return true;
        }

        public void StartExposure(CaptureSequence sequence) {
            RaiseIfNotConnected();
            uint rv;
            uint startx, starty, sizex, sizey;
            bool isSnap;
            short readoutMode;

            /*
             * Setup camera with the desired exposure setttings
             */

            isSnap = sequence.ImageType == CaptureSequence.ImageTypes.SNAPSHOT;

            /* Open or close the shutter if camera is equipped with one */
            if (HasShutter == true) {
                if (sequence.ImageType == CaptureSequence.ImageTypes.DARK ||
                        sequence.ImageType == CaptureSequence.ImageTypes.DARKFLAT ||
                        sequence.ImageType == CaptureSequence.ImageTypes.BIAS) {
                    Logger.Debug($"QHYCCD: Closing shutter for {sequence.ImageType} frame");
                    _ = Sdk.ControlShutter(1);
                } else {
                    Logger.Debug($"QHYCCD: Opening shutter for {sequence.ImageType} frame");
                    _ = Sdk.ControlShutter(0);
                }
            }

            /* Exposure readout mode */
            readoutMode = isSnap ? ReadoutModeForSnapImages : ReadoutModeForNormalImages;

            if (ReadoutMode != readoutMode) {
                ReadoutMode = readoutMode;
            }

            /* Exposure bit depth */
            if (Sdk.SetBitsMode((uint)BitDepth) != QhySdk.QHYCCD_SUCCESS) {
                Logger.Warning("QHYCCD: Failed to set exposure bit depth. This may not be a fatal error.");
                CameraState = QhySdk.QHYCCD_CAMERA_STATE.ERROR.ToString();
            }

            /* Exposure length (in microseconds) */
            if (!Sdk.SetControlValue(QhySdk.CONTROL_ID.CONTROL_EXPOSURE, sequence.ExposureTime * 1e6)) {
                Logger.Warning("QHYCCD: Failed to set exposure time");
                CameraState = QhySdk.QHYCCD_CAMERA_STATE.ERROR.ToString();
                return;
            }

            /*
             * Set binning
             * Certain models of camera require a 200ms pause after setting the bin mode.
             * SetQHYCCDBinMode() will return QHYCCD_DELAY_200MS if that is required.
             */
            if ((rv = Sdk.SetBinMode((uint)BinX, (uint)BinY)) != QhySdk.QHYCCD_SUCCESS) {
                if (rv == QhySdk.QHYCCD_DELAY_200MS) {
                    Thread.Sleep(200);
                } else {
                    Logger.Warning($"QHYCCD: Failed to set BIN mode {BinX}x{BinY}");
                }
            }

            if (!SetResolution(out startx, out starty, out sizex, out sizey)) {
                CameraState = QhySdk.QHYCCD_CAMERA_STATE.ERROR.ToString();
                return;
            }

            /*
             * Calculate exposure array size, with overflow protection.
             * Strictly speaking, we should also multiply by the number of image channels (aka planes)
             * but since we do no debayer anything here in the driver, that number will always be 1 (monochrome).
             */
            ImageSize = (uint)((sizex * sizey * BitDepth) + (8 - 1)) / 8;

            /*
             * Initiate the exposure
             */
            Logger.Debug("QHYCCD: Starting exposure...");
            CameraState = QhySdk.QHYCCD_CAMERA_STATE.EXPOSING.ToString();
            if (Sdk.ExpSingleFrame() == QhySdk.QHYCCD_ERROR) {
                Logger.Warning("QHYCCD: Failed to initiate the exposure!");
                CameraState = QhySdk.QHYCCD_CAMERA_STATE.ERROR.ToString();
                return;
            }

            downloadExposureTaskCTS?.Dispose();
            downloadExposureTaskCTS = new CancellationTokenSource();
            downloadExposureTask = StartDownloadExposure(downloadExposureTaskCTS.Token);
        }

        public void StartLiveView() {
            throw new System.NotImplementedException();
        }

        public void StopExposure() {
            RaiseIfNotConnected();
            if (Sdk.CancelExposingAndReadout() != QhySdk.QHYCCD_ERROR) {
                CameraState = QhySdk.QHYCCD_CAMERA_STATE.IDLE.ToString();
            }
        }

        public void StopLiveView() {
            throw new NotImplementedException();
        }

        public bool QhyIncludeOverscan {
            get => profileService.ActiveProfile.CameraSettings.QhyIncludeOverscan;
            set {
                profileService.ActiveProfile.CameraSettings.QhyIncludeOverscan = value;
                SetImageResolution();
            }
        }

        public bool QhyHasSensorAirPressure {
            get => Info.HasSensorAirPressure;
            private set => Info.HasSensorAirPressure = value;
        }

        public bool QhyHasSensorHumidity {
            get => Info.HasSensorHumidity;
            private set => Info.HasSensorHumidity = value;
        }

        public double QhySensorAirPressure {
            get => Info.SensorAirPressure;
            private set {
                if (Info.SensorAirPressure != value) {
                    Info.SensorAirPressure = value;
                    RaisePropertyChanged();
                }
            }
        }

        public double QhySensorHumidity {
            get => Info.SensorHumidity;
            private set {
                if (Info.SensorHumidity != value) {
                    Info.SensorHumidity = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double GetQhySensorAirPressure() {
            double pressure = double.NaN;

            if (Connected && QhyHasSensorHumidity) {
                Sdk.GetPressure(ref pressure);
            }

            Logger.Trace($"QHYCCD: Sensor air pressure: {pressure} hPa");
            return pressure;
        }

        private double GetQhySensorHumidity() {
            double rh = double.NaN;

            if (Connected && QhyHasSensorHumidity) {
                Sdk.GetHumidity(ref rh);
            }

            Logger.Trace($"QHYCCD: Sensor humidity: {rh}%");
            return rh;
        }

        public string QhyFirmwareVersion {
            get => Info.FirmwareVersion;
            private set => Info.FirmwareVersion = value;
        }

        public string QhyFPGAVersion {
            get => Info.FPGAVersion;
            private set => Info.FPGAVersion = value;
        }

        private string GetFirmwareVersion() {
            return Sdk.GetFwVersion();
        }

        private string GetFPGAVersion() {
            return Sdk.GetFpgaVersion();
        }

        private string GetSdkVersion() {
            return Sdk.GetSdkVersion();
        }

        public string QhySdkVersion {
            get => Info.SdkVersion;
            private set => Info.SdkVersion = value;
        }

        public string QhyUsbDriverVersion {
            get => Info.UsbDriverVersion;
            private set => Info.UsbDriverVersion = value;
        }

        private void SetImageResolution() {
            double pixelx = 0, pixely = 0;

            /*
             * Get full sensor size and pixel dimensions
             */
            ThrowOnFailure("GetQHYCCDChipInfo", Sdk.GetChipInfo(
                ref Info.ChipX, ref Info.ChipY,
                ref Info.FullArea.SizeX, ref Info.FullArea.SizeY,
                ref pixelx, ref pixely,
                ref Info.Bpp));

            Info.FullArea.StartX = Info.FullArea.StartY = 0;
            Logger.Debug($"QHYCCD: Chip Info: ChipX={Info.ChipX}mm, ChipY={Info.ChipY}mm, SizeX={Info.FullArea.SizeX}, SizeY={Info.FullArea.SizeY}, PixelX={pixelx}um, PixelY={pixely}um, bpp={Info.Bpp}");

            /*
             * Update the pixel size if it has changed (eg; QHY294M/C Pro)
             * Other NINA processes depend on pixel size being accurate and source this info from the profile, so we must update that as well.
             */
            if (PixelSizeX != pixelx) {
                profileService.ActiveProfile.CameraSettings.PixelSize = PixelSizeX = pixelx;
                PixelSizeY = pixely;
            }

            /*
             * The Effective Area is a sensor's real imaging area. On sensors that have an overscan area, the effective area will be smaller than
             * the sensor's dimensions that were reported by GetQHYCCDChipInfo(). If the sensor does not have an overscan area, the values should be equal.
             */
            ThrowOnFailure("GetQHYCCDEffectiveArea", Sdk.GetEffectiveArea(ref Info.EffectiveArea.StartX, ref Info.EffectiveArea.StartY, ref Info.EffectiveArea.SizeX, ref Info.EffectiveArea.SizeY));
            Logger.Debug($"QHYCCD: Effective Area: StartX={Info.EffectiveArea.StartX}, StartY={Info.EffectiveArea.StartY}, SizeX={Info.EffectiveArea.SizeX}, SizeY={Info.EffectiveArea.SizeY}");

            if (QhyIncludeOverscan) {
                CameraXSize = (int)Info.FullArea.SizeX;
                CameraYSize = (int)Info.FullArea.SizeY;
                Sdk.SetControlValue(QhySdk.CONTROL_ID.CAM_IGNOREOVERSCAN_INTERFACE, 0d);
            } else {
                CameraXSize = (int)Info.EffectiveArea.SizeX;
                CameraYSize = (int)Info.EffectiveArea.SizeY;
                Sdk.SetControlValue(QhySdk.CONTROL_ID.CAM_IGNOREOVERSCAN_INTERFACE, 1d);
            }

            Logger.Debug($"QHYCCD: Sensor dimensions used: Overscan={QhyIncludeOverscan}, SizeX={CameraXSize}, SizeY={CameraYSize}");
        }

        private uint StartPixelX {
            get => Info.CurImage.StartX;
            set {
                Logger.Debug($"QHYCCD: Setting StartPixelX to {value}");
                Info.CurImage.StartX = value;
            }
        }

        private uint StartPixelY {
            get => Info.CurImage.StartY;
            set {
                Logger.Debug($"QHYCCD: Setting StartPixelY to {value}");
                Info.CurImage.StartY = value;
            }
        }

        private uint ImageSize {
            get => Info.ImageSize;
            set {
                Logger.Debug($"QHYCCD: Setting ImageSize to {value} bytes");
                Info.ImageSize = value;
            }
        }

        private void DriverVersionCheck() {
            // Minimum driver versions. Key: Driver name. Value: Minimum driver version
            Dictionary<string, string> driverDatabase = new Dictionary<string, string> {
                { "QHY5IIISeries_IO", "21.2.20.0" },
                { "QHY5II_IO", "0.0.9.0" },
                { "QHY8LBASE", "0.0.9.0" },
                { "QHY8PBASE", "0.0.9.0" },
                { "QHY9BASE", "0.0.9.0" },
                { "QHY10BASE", "0.0.9.0" },
                { "QHY11BASE", "0.0.9.0" },
                { "QHY12BASE", "0.0.9.0" },
                { "QHY22BASE", "0.0.9.0" },
                { "IMG2PBASE", "0.0.9.0" },
                { "QHY90A_IO", "0.0.9.0" },
                { "QHY695A_IO", "0.0.9.0" },
                { "QHY814A_IO", "0.0.9.0" },
                { "QHY09000A_IO", "0.0.9.0" },
                { "QHY16200A_IO", "0.0.9.0" },
                { "QHY16803A_IO", "0.0.9.0" }
            };

            QhyUsbDriverVersion = string.Empty;

            try {
                ManagementObjectSearcher searchObject = null;
                ManagementObjectCollection objCollection = null;

                foreach (KeyValuePair<string, string> driverInfo in driverDatabase) {
                    string driverName = driverInfo.Key;
                    string minimumVersion = driverInfo.Value;

                    searchObject = new ManagementObjectSearcher($"SELECT DriverVersion FROM Win32_PnPSignedDriver WHERE DeviceName = '{driverName}'");
                    objCollection = searchObject.Get();

                    if (objCollection.Count > 0) {
                        // We are interested in only one instance if there are multiple cameras connected. They should all have the same version.
                        var obj = objCollection.OfType<ManagementObject>().FirstOrDefault();

                        if (!string.IsNullOrEmpty(obj["DriverVersion"].ToString())) {
                            QhyUsbDriverVersion = obj["DriverVersion"].ToString();
                            Logger.Debug($"QHYCCD: {driverName} driver version: {QhyUsbDriverVersion}");
                        }

                        // Emit a warning to the user if the USB3 driver is behind
                        // Per QHY, the USB2 drivers aren't of major concern
                        if (driverName.Equals("QHY5IIISeries_IO")) {
                            var minVer = new Version(minimumVersion);
                            var runVer = new Version(QhyUsbDriverVersion);
                            var compare = minVer.CompareTo(runVer);

                            if (compare > 0) {
                                Logger.Warning($"QHYCCD: Installed USB driver version {QhyUsbDriverVersion} is older than recommended version {minimumVersion}. Operation of the camera may not be reliable and updating is HIGHLY suggested.");
                                Notification.ShowWarning(string.Format(Loc.Instance["LblQhyccdDriverVersionWarning"], QhyUsbDriverVersion, minimumVersion));
                            }
                        }

                        // We found this camera's driver. Move on.
                        break;
                    }
                }

                objCollection.Dispose();
                searchObject.Dispose();
            } catch (Exception e) {
                Logger.Error($"QHYCCD: Fx3DriverVersionCheck failed: {e}");
            }
        }

        ///<summary>
        // Camera models usually accept CONTROL_GAIN values as a normal integer
        // in accordance with the accepted ranges as stated by the values returned
        // by GetQHYCCDParamMinMaxStep().
        // HOWEVER this is not always the case, as with the QHY163M (at least). The
        // QHY163M will state its gain range as being 0..580 inclusive and setting
        // CONTROL_GAIN with a value in that range is accepted by the SDK and acted
        // upon accordingly. But when asked to report its current CONTROL_GAIN value,
        // the QHY163M (and perhaps other models as well) will give the value, but divided
        // by 10. Ie, setting the gain to 420 will result in the gain being reported as 42.0
        // by the SDK.
        //
        // Other camera models (such as the QHY168C) will report absolutely false values for
        // their gain, so we must track that value internally because we will revcieve unreliable
        // answers from the camera.
        //
        // We will venture to detect this kind of situation by issuing a gain setting to the
        // camera and then testing the value returned by GetQHYCCDParam(). If we find that what
        // will flag it necessary to normalize Get'd values.
        //
        // Valid issue as of SDK V2019.02.11.0
        ///</summary>
        private bool QuirkUnreliableGain() {
            /* Save a copy of what our current gain is so we can restore it later */
            double saveGain = Sdk.GetControlValue(QhySdk.CONTROL_ID.CONTROL_GAIN);

            /*
             * Set the gain to the maximum gain value minus one step for the camera. We will then query the camera and
             * test whether we get the value we set or not (which indicates the bug).
             */
            double wantGain = Info.GainMax - Info.GainStep;
            _ = Sdk.SetControlValue(QhySdk.CONTROL_ID.CONTROL_GAIN, wantGain);

            double curGain = Sdk.GetControlValue(QhySdk.CONTROL_ID.CONTROL_GAIN);

            /* Restore our original gain setting */
            _ = Sdk.SetControlValue(QhySdk.CONTROL_ID.CONTROL_GAIN, saveGain);

            if (wantGain != curGain) {
                Logger.Debug("QHYCCD_QUIRK: This camera reports false gain values");
                return true;
            }

            return false;
        }

        ///<summary>
        // QHY Polemasters seem to offset the sensor Offset by +50 points.
        // An Offeset of 1 becomes 51, 80 becomes 130, etc. Here we try to detect this and account for it.
        //
        // Valid issue as of SDK V20180502_0
        ///</summary>
        private void QuirkInflatedOffset() {
            double saveOffset = Sdk.GetControlValue(QhySdk.CONTROL_ID.CONTROL_OFFSET);

            double wantOffset = 1;
            double gotOffset;

            _ = Sdk.SetControlValue(QhySdk.CONTROL_ID.CONTROL_OFFSET, wantOffset);
            gotOffset = Sdk.GetControlValue(QhySdk.CONTROL_ID.CONTROL_OFFSET) - wantOffset;

            if (gotOffset != 0) {
                Logger.Debug($"QHYCCD_QUIRK: This camera inflates its Offset by {gotOffset}");
                Info.InflatedOff = (int)gotOffset;
            } else {
                Info.InflatedOff = 0;
            }

            /* Restore our original gain setting */
            _ = Sdk.SetControlValue(QhySdk.CONTROL_ID.CONTROL_OFFSET, saveOffset);
        }

        ///<summary>
        // Some QHY cameras (such as the QHY163M) report that the CONTROL_USBTRAFFIC
        // control is available and the SDK will even tell you min and max values
        // for it, however it is managed by the camera and any attempt to set a value
        // for CONTROL_USBTRAFFIC will silently fail. Here we will detect this case so
        // that we can disable the UI control for USB bandwidth if this issue is present.
        //
        // Valid issue as of SDK V20180502_0
        ///</summary>
        private bool QuirkNoUSBTraffic() {
            double wantUSB;
            double saveUSB = Sdk.GetControlValue(QhySdk.CONTROL_ID.CONTROL_USBTRAFFIC);

            if (saveUSB != Info.USBMax) {
                wantUSB = Info.USBMax;
            } else {
                wantUSB = Info.USBMax - Info.USBStep;
            }
            _ = Sdk.SetControlValue(QhySdk.CONTROL_ID.CONTROL_USBTRAFFIC, wantUSB);
            double gotUSB = Sdk.GetControlValue(QhySdk.CONTROL_ID.CONTROL_USBTRAFFIC);

            /* Check to see if it really changed */
            if (gotUSB == saveUSB) {
                Logger.Debug("QHYCCD_QUIRK: This camera does not really allow CONTROL_USBTRAFFIC settings");
                return true;
            }

            /* Restore the original USB traffic setting */
            _ = Sdk.SetControlValue(QhySdk.CONTROL_ID.CONTROL_USBTRAFFIC, saveUSB);
            return false;
        }
    }
}