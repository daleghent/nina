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

    public struct DeviceQueryInfo {
        public SBIG.CameraType CameraType;
        public string Name;
        public string SerialNumber;
        public SBIG.DeviceType DeviceId;
        public FilterWheelInfo? FilterWheelInfo;
    }

    public struct DeviceInfo {
        public SBIG.DeviceType DeviceId;
        public SBIG.CameraType CameraType;
        public CcdCameraInfo? CameraInfo;
        public FilterWheelInfo? FilterWheelInfo;
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

    public struct FilterWheelInfo {
        public SBIG.CfwModelSelect Model;
        public SBIG.CfwPosition Position;
        public SBIG.CfwStatus Status;
        public uint FirmwareVersion;
        public uint FilterCount;
    }

    public struct FilterWheelStatus {
        public SBIG.CfwPosition Position;
        public SBIG.CfwStatus Status;
    }

    public class SBIGExposureData {
        public ushort[] Data;
        public ushort Width;
        public ushort Height;
    }

    public interface ISbigSdk {

        DeviceInfo OpenDevice(SBIG.DeviceType deviceId);

        void CloseDevice(SBIG.DeviceType deviceId);

        string GetSdkVersion();

        void UnivDrvCommand<P>(SBIG.Cmd command, P Params);

        void UnivDrvCommand<P, R>(SBIG.Cmd command, P Params, R pResults) where R : new();

        R UnivDrvCommand<P, R>(SBIG.Cmd command, P Params) where R : new();

        DeviceQueryInfo[] QueryUsbDevices();

        CcdCameraInfo GetCameraInfo(SBIG.DeviceType deviceId);

        FilterWheelInfo? GetFilterWheelInfo(SBIG.DeviceType deviceId);

        SBIG.QueryTemperatureStatusResults2 QueryTemperatureStatus(SBIG.DeviceType deviceId);

        void RegulateTemperature(SBIG.DeviceType deviceId, double celcius);

        void DisableTemperatureRegulation(SBIG.DeviceType deviceId);

        CommandState GetExposureState(SBIG.DeviceType deviceId);

        void StartExposure(SBIG.DeviceType deviceId, ReadoutMode readoutMode, bool darkFrame, double exposureTimeSecs, Point exposureStart, Size exposureSize);

        void EndExposure(SBIG.DeviceType deviceId);

        SBIGExposureData DownloadExposure(SBIG.DeviceType deviceId, CancellationToken ct);

        void SetFilterWheelPosition(SBIG.DeviceType deviceId, ushort position);

        FilterWheelStatus GetFilterWheelStatus(SBIG.DeviceType deviceId);
    }
}
