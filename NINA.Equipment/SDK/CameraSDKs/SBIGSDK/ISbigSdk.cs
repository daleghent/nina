using NINA.Equipment.SDK.CameraSDKs.SBIGSDK.SbigSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Threading;

namespace NINA.Equipment.SDK.CameraSDKs.SBIGSDK {

    public struct CameraQueryInfo {
        public SBIG.CameraType CameraType;
        public string Name;
        public string SerialNumber;
        public SBIG.DeviceType DeviceId;
    }

    public struct ReadoutMode {
        public ushort RawMode;
        public SBIG.ReadoutMode Mode;
        public static ReadoutMode Create(int binning) {
            if (binning < 1 || binning > 3) {
                throw new NotSupportedException($"Only binning modes 1-3 are supported");
            }

            SBIG.ReadoutMode readoutMode;
            if (binning == 1) {
                readoutMode = SBIG.ReadoutMode.NoBinning;
            } else if (binning == 2) {
                readoutMode = SBIG.ReadoutMode.Bin2x2;
            } else {
                readoutMode = SBIG.ReadoutMode.Bin3x3;
            }

            // If other binning modes are supported, then the higher bits of RawMode will be set as well
            return new ReadoutMode() {
                RawMode = (ushort)readoutMode,
                Mode = readoutMode
            };
        }
    }

    public struct ReadoutModeConfig {
        public ReadoutMode ReadoutMode;
        public int BinX;
        public int BinY;
        public bool BinningOffChip;
        public ushort Width;
        public ushort Height;
        public double ElectronsPerAdu;
        public double PixelWidthMicrons;
        public double PixelHeightMicrons;
    }

    public enum SBIG_CAMERA_STATE {
        IDLE = 0,
        EXPOSING,
        DOWNLOADING,
        ERROR
    };

    public enum CcdFrameType {
        FULL_FRAME_CCD,
        FRAME_TRANSFER_CCD
    }

    public enum CcdType {
        MONO,
        BAYER_MATRIX,
        TRUSENSE_COLOR_MATRIX
    }

    public enum CommandState {
        IDLE,
        IN_PROGRESS,
        COMPLETE
    }

    public struct CcdCameraInfo {
        public string FirmwareVersion;
        public SBIG.CameraType CameraType;
        public string Name;
        public ReadoutModeConfig[] ReadoutModeConfigs;
        public ushort[] BadColumns;
        public bool HasAntiBloomingGateProtection;
        public string SerialNumber;
        public ushort AdcBits;
        public SBIG.FilterType FilterType;
        public CcdFrameType CcdFrameType;
        public bool HasElectronicShutter;
        public bool SupportsExternalTracker;
        public bool SupportsBTDI;
        public bool HasAO8;
        public bool HasFrameBuffer;
        public bool RequiresStartExposure2;
        public ushort NumberExtraUnbinnedRows;
        public bool IsSTXL;
        public bool HasMechanicalShutter;
        public CcdType CcdType;
    }

    public interface ISbigSdk {

        void InitSdk();

        void ReleaseSdk();

        SBIG.CameraType OpenDevice(SBIG.DeviceType deviceId);

        void CloseDevice();

        string GetSdkVersion();

        void UnivDrvCommand<P>(SBIG.Cmd command, P Params);

        void UnivDrvCommand<P, R>(SBIG.Cmd command, P Params, R pResults) where R : new();

        R UnivDrvCommand<P, R>(SBIG.Cmd command, P Params) where R : new();

        CameraQueryInfo[] QueryUsbCameras();

        CcdCameraInfo GetCameraInfo();

        SBIG.QueryTemperatureStatusResults2 QueryTemperatureStatus();

        void RegulateTemperature(double celcius);

        void DisableTemperatureRegulation();

        CommandState GetExposureState();

        SBIG.StartExposureParams2 StartExposure(ReadoutMode readoutMode, bool darkFrame, double exposureTimeSecs, Point exposureStart, Size exposureSize);

        void EndExposure();

        ushort[] DownloadExposure(SBIG.StartExposureParams2 exposureParams, CancellationToken ct);
    }
}
