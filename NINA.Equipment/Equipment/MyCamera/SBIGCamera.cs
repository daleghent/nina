using NINA.Core.Enum;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Model;
using NINA.Equipment.SDK.CameraSDKs.SBIGSDK;
using NINA.Equipment.SDK.CameraSDKs.SBIGSDK.SbigSharp;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MyCamera {
    public class SBIGCamera : BaseINPC, ICamera {

        private readonly CameraQueryInfo queriedCameraInfo;
        private readonly ISbigSdk sdk;

        public SBIGCamera(ISbigSdk sdk, CameraQueryInfo queriedCameraInfo) {
            this.sdk = sdk;
            this.BinningModes = new AsyncObservableCollection<BinningMode>();
            this.queriedCameraInfo = queriedCameraInfo;
            this.Id = queriedCameraInfo.SerialNumber;
            this.Name = queriedCameraInfo.Name;
            this.DriverVersion = sdk.GetSdkVersion();
            this.Description = $"{queriedCameraInfo.Name} on {queriedCameraInfo.DeviceId}";
        }

        public bool HasSetupDialog => false;

        public string Id { get; private set; }

        public string Name { get; private set; }

        public string Category => "SBIG Legacy";

        private string _description;
        public string Description {
            get => _description;
            private set {
                if (_description != value) {
                    _description = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string DriverInfo => "Native driver for legacy SBIG cameras";

        public string DriverVersion { get; private set; }

        private bool _connected = false;
        public bool Connected {
            get => _connected;
            set {
                if (_connected != value) {
                    _connected = value;
                    RaiseAllPropertiesChanged();
                }
            }
        }

        public Task<bool> Connect(CancellationToken ct) {
            if (Connected) {
                return Task.FromResult(true);
            }

            return Task.Run(() => {
                Logger.Info($"SBIGCCD: Attempting to connect {this.queriedCameraInfo.DeviceId}");
                sdk.InitSdk();
                try {
                    var cameraType = sdk.OpenDevice(this.queriedCameraInfo.DeviceId);
                    if (cameraType == SBIG.CameraType.NoCamera) {
                        throw new InvalidOperationException($"SBIGCCD: Cannot connect {this.queriedCameraInfo.DeviceId} since it is not a camera");
                    }

                    var cameraInfo = sdk.GetCameraInfo();
                    SetCameraProperties(cameraInfo);
                    Connected = true;
                    if (CanSetTemperature) {
                        CoolerOn = false;
                    }

                    Logger.Info($"SBIGCCD: Successfully connected {this.queriedCameraInfo.DeviceId}");
                    return true;
                } catch (Exception e) {
                    Logger.Error($"SBIGCCD: Failed to connect {this.queriedCameraInfo.DeviceId}", e);
                    Notification.ShowError($"Failed to connect {this.queriedCameraInfo.DeviceId}, {e}");
                    Connected = false;
                    sdk.ReleaseSdk();
                    return false;
                }
            }, ct);
        }

        public void Disconnect() {
            if (!Connected) {
                return;
            }

            try {
                CoolerOn = false;
                sdk.CloseDevice();
            } catch (Exception e) {
                Logger.Error($"SBIGCCD: Failed while trying to close device {this.queriedCameraInfo.DeviceId}. Ignoring", e);
            } finally {
                Connected = false;
                sdk.ReleaseSdk();
            }
        }

        private void SetCameraProperties(CcdCameraInfo cameraInfo) {
            Description = $"{queriedCameraInfo.Name} on {queriedCameraInfo.DeviceId}, Firmware Version={cameraInfo.FirmwareVersion}";
            SensorName = cameraInfo.CameraType.ToString();
            HasShutter = cameraInfo.HasMechanicalShutter;
            if (cameraInfo.CcdType == CcdType.MONO) {
                SensorType = SensorType.Monochrome;
            } else if (cameraInfo.CcdType == CcdType.BAYER_MATRIX) {
                SensorType = SensorType.BGGR;
            } else {
                SensorType = SensorType.LRGB;
            }
            BitDepth = cameraInfo.AdcBits;

            var unbinnedMode = SbigSdk.GetReadoutModeConfig(cameraInfo.ReadoutModeConfigs, SBIG.ReadoutMode.NoBinning);
            if (!unbinnedMode.HasValue) {
                throw new NotSupportedException($"SBIG camera doesn't have an unbinned mode, which is not expected");
            }

            BinX = 1;
            BinY = 1;
            PixelSizeX = unbinnedMode.Value.PixelWidthMicrons;
            PixelSizeY = unbinnedMode.Value.PixelHeightMicrons;
            CameraXSize = unbinnedMode.Value.Width;
            CameraYSize = unbinnedMode.Value.Height;

            // For simplicity, only support on chip 2x2 and 3x3 binning modes
            // If there's user demand we could enable off chip, non-square, and greater than 3 binning modes
            var bin2Mode = SbigSdk.GetReadoutModeConfig(cameraInfo.ReadoutModeConfigs, SBIG.ReadoutMode.Bin2x2);
            var bin3Mode = SbigSdk.GetReadoutModeConfig(cameraInfo.ReadoutModeConfigs, SBIG.ReadoutMode.Bin3x3);
            if (bin3Mode.HasValue) {
                MaxBinX = MaxBinY = 3;
            } else if (bin2Mode.HasValue) {
                MaxBinX = MaxBinY = 2;
            }

            BinningModes.Clear();
            for (short i = 1; i <= MaxBinX; ++i) {
                BinningModes.Add(new BinningMode(i, i));
            } 
        }

        private bool _hasShutter = false;
        public bool HasShutter {
            get => _hasShutter;
            private set {
                Logger.Debug($"SBIGCCD: Setting HasShutter to {value}");
                if (_hasShutter != value) {
                    _hasShutter = value;
                    RaisePropertyChanged();
                }
            }
        }

        private short _binning = 1;
        public short BinX {
            get => _binning;
            set {
                Logger.Debug($"SBIGCCD: Setting BinX to {value}");
                if (value < 1 || value > MaxBinX) {
                    throw new InvalidOperationException($"BinX {value} must be between 1 and {MaxBinX} inclusive");
                }
                if (_binning != value) {
                    _binning = value;
                    RaisePropertyChanged();
                }
            }
        }

        public short BinY {
            get => _binning;
            set {
                if (value < 1 || value > MaxBinX) {
                    throw new InvalidOperationException($"BinY {value} must be between 1 and {MaxBinY} inclusive");
                }
                // For simplification, we only support symmetric binning modes. The camera may support MxN binning though
                BinX = value;
            }
        }

        private string _sensorName;
        public string SensorName { 
            get => _sensorName;
            set {
                if (value != _sensorName) {
                    _sensorName = value;
                    RaisePropertyChanged();
                }
            }
        }

        private SensorType _sensorType = SensorType.Monochrome;
        public SensorType SensorType {
            get => _sensorType;
            private set {
                Logger.Debug($"SBIGCCD: Setting SensorType to {value}");
                if (_sensorType != value) {
                    _sensorType = value;
                    RaisePropertyChanged();
                }
            }

        }

        public short BayerOffsetX => 0;

        public short BayerOffsetY => 0;

        private int _cameraXSize = 0;
        public int CameraXSize {
            get => _cameraXSize;
            private set {
                if (_cameraXSize != value) {
                    Logger.Debug($"SBIGCCD: Setting CameraXSize to {value}");
                    _cameraXSize = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int _cameraYSize = 0;
        public int CameraYSize {
            get => _cameraYSize;
            private set {
                if (_cameraYSize != value) {
                    Logger.Debug($"SBIGCCD: Setting CameraYSize to {value}");
                    _cameraYSize = value;
                    RaisePropertyChanged();
                }
            }
        }

        // Exposures are in hundredths of a second, so this is the shortest it can go
        // NOTE: Some cameras may support millisecond precision for exposures shorter than 500ms, but it is not implemented here yet
        public double ExposureMin => 0.01d;

        // Exposures are in hundredths of a second using 24 bits
        public double ExposureMax => (1 << 24) / 100.0d;

        private short _maxBinning = 1;
        public short MaxBinX {
            get => _maxBinning;
            private set {
                if (value != _maxBinning) {
                    Logger.Debug($"SBIGCCD: Setting MaxBinning to {value}");
                    _maxBinning = value;
                    RaisePropertyChanged();
                }
            }
        }

        public short MaxBinY {
            get => MaxBinX;
            private set {
                MaxBinX = value;
            }
        }

        private double _pixelSizeX;
        public double PixelSizeX {
            get => _pixelSizeX;
            private set {
                Logger.Debug($"SBIGCCD: Setting PixelSizeX to {value}");
                if (value != _pixelSizeX) {
                    _pixelSizeX = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double _pixelSizeY;
        public double PixelSizeY {
            get => _pixelSizeY;
            private set {
                Logger.Debug($"SBIGCCD: Setting PixelSizeY to {value}");
                if (value != _pixelSizeY) {
                    _pixelSizeY = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool HasDewHeater => false;

        private string _cameraState;

        public string CameraState {
            get => _cameraState;
            private set {
                Logger.Debug($"SBIGCCD: Setting CameraState to {value}");
                if (value != _cameraState) {
                    this._cameraState = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool CanSubSample => true;
        public bool EnableSubSample { get; set; }
        public int SubSampleX { get; set; }
        public int SubSampleY { get; set; }
        public int SubSampleWidth { get; set; }
        public int SubSampleHeight { get; set; }

        private int _bitDepth = 16;
        public int BitDepth {
            get => _bitDepth;
            private set {
                Logger.Debug($"SBIGCCD: Setting BitDepth to {value}");
                if (value != _bitDepth) {
                    this._bitDepth = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double _electronsPerADU;
        public double ElectronsPerADU {
            get => _electronsPerADU;
            private set {
                Logger.Debug($"SBIGCCD: Setting ElectronsPerADU to {value}");
                if (value != _electronsPerADU) {
                    this._electronsPerADU = value;
                    RaisePropertyChanged();
                }
            }
        }

        public IList<string> ReadoutModes => new List<string>() { "Default" };

        public short ReadoutMode {
            get => 0;
            set { }
        }

        private short _readoutModeForSnapImages;

        public short ReadoutModeForSnapImages {
            get => _readoutModeForSnapImages;
            set {
                _readoutModeForSnapImages = value;
                RaisePropertyChanged();
            }
        }

        private short _readoutModeForNormalImages;

        public short ReadoutModeForNormalImages {
            get => _readoutModeForNormalImages;
            set {
                _readoutModeForNormalImages = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<BinningMode> BinningModes { get; }

        public bool CanSetTemperature => true;

        public double Temperature {
            get {
                var tempStatus = sdk.QueryTemperatureStatus();
                return tempStatus.imagingCCDTemperature;
            }
        }

        private double _temperatureSetPoint = 0.0d;
        public double TemperatureSetPoint {
            get {
                if (CoolerOn) {
                    var tempStatus = sdk.QueryTemperatureStatus();
                    var ccdSetPoint = tempStatus.ccdSetpoint;
                    if (ccdSetPoint != _temperatureSetPoint) {
                        _temperatureSetPoint = ccdSetPoint;
                        RaisePropertyChanged();
                    }
                }
                return _temperatureSetPoint;
            }
            set {
                if (_temperatureSetPoint != value) {
                    _temperatureSetPoint = value;
                    if (CoolerOn) {
                        sdk.RegulateTemperature(value);
                    }

                    RaisePropertyChanged();
                }
            }
        }

        public bool CoolerOn {
            get {
                if (Connected) {
                    var tempStatus = sdk.QueryTemperatureStatus();
                    return tempStatus.coolingEnabled == 1;
                } else {
                    return false;
                }
            }
            set {
                if (value) {
                    sdk.RegulateTemperature(TemperatureSetPoint);
                } else {
                    sdk.DisableTemperatureRegulation();
                }
                RaisePropertyChanged();
            }
        }

        public double CoolerPower {
            get {
                if (Connected) {
                    var tempStatus = sdk.QueryTemperatureStatus();
                    return tempStatus.imagingCCDPower;
                } else {
                    return 0.0;
                }
            }
        }

        public void AbortExposure() {
            StopExposure();
        }

        public void SetBinning(short x, short y) {
            if (x != y) {
                throw new NotSupportedException($"Cannot set binning to {x}, {y}. Only symmetric binning modes are supported.");
            }

            BinX = x;
            BinY = y;
        }

        private SBIG.StartExposureParams2 _inProgressExposureParams;
        public void StartExposure(CaptureSequence sequence) {
            var isDarkFrame = sequence.ImageType == CaptureSequence.ImageTypes.DARK ||
                               sequence.ImageType == CaptureSequence.ImageTypes.BIAS ||
                               sequence.ImageType == CaptureSequence.ImageTypes.DARKFLAT;
            var readoutMode = SDK.CameraSDKs.SBIGSDK.ReadoutMode.Create(BinX);
            Point exposureStart;
            Size exposureSize;
            if (EnableSubSample) {
                exposureStart = new Point(SubSampleX / BinX, SubSampleY / BinY);
                exposureSize = new Size(SubSampleWidth / BinX, SubSampleHeight / BinY);
            } else {
                exposureStart = new Point(0, 0);
                exposureSize = new Size(CameraXSize / BinX, CameraYSize / BinY);
            }

            _inProgressExposureParams = sdk.StartExposure(readoutMode, isDarkFrame, sequence.ExposureTime, exposureStart, exposureSize);
        }

        public async Task<IExposureData> DownloadExposure(CancellationToken ct) {
            return await Task.Run(() => {
                try {
                    var exposureData = sdk.DownloadExposure(this._inProgressExposureParams, ct);
                    return new ImageArrayExposureData(
                        input: exposureData,
                        width: this._inProgressExposureParams.width,
                        height: this._inProgressExposureParams.height,
                        bitDepth: BitDepth,
                        isBayered: SensorType != SensorType.Monochrome && BinX == 1 && BinY == 1,
                        metaData: new ImageMetaData());
                } catch (OperationCanceledException) {
                } catch (Exception e) {
                    Logger.Error(e);
                    Notification.ShowError(e.Message);
                }
                return null;
            }, ct);
        }

        public void StopExposure() {
            sdk.EndExposure();
        }

        public async Task WaitUntilExposureIsReady(CancellationToken token) {
            using (token.Register(() => AbortExposure())) {
                while (sdk.GetExposureState() == CommandState.IN_PROGRESS) {
                    await Task.Delay(100, token);
                }
                sdk.EndExposure();
            }
        }

        #region Unsupported Operations

        public bool CanShowLiveView => false;

        public bool LiveViewEnabled => false;

        public bool HasBattery => false;

        public int BatteryLevel => 0;

        public bool CanSetOffset => false;

        public int Offset { get => 0; set => throw new InvalidOperationException(); }

        public int OffsetMin => 0;

        public int OffsetMax => 0;

        public bool CanSetUSBLimit => false;

        public int USBLimit { get => 0; set => throw new InvalidOperationException(); }

        public int USBLimitMin => 0;

        public int USBLimitMax => 0;

        public int USBLimitStep => 1;

        public bool CanGetGain => false;

        public bool CanSetGain => false;

        public int GainMax => 0;

        public int GainMin => 0;

        public int Gain { get => 0; set => throw new InvalidOperationException(); }

        public IList<int> Gains => new List<int>() { 0 };

        public bool DewHeaterOn { get => false; set => throw new InvalidOperationException(); }

        public void StartLiveView() {
            throw new InvalidOperationException();
        }

        public void StopLiveView() {
            throw new NotImplementedException();
        }

        public void SetupDialog() {
            throw new NotImplementedException();
        }

        public Task<IExposureData> DownloadLiveView(CancellationToken ct) {
            throw new NotImplementedException();
        }

        #endregion
    }
}
