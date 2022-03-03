#region "copyright"
/*
    Copyright © 2016 - 2022 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using NINA.Core.Enum;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Model;
using NINA.Equipment.SDK.CameraSDKs.PlayerOneSDK;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MyCamera {
    public class PlayerOneCamera : BaseINPC, ICamera {
        
        private IPlayerOneSDK sdk;
        private IProfileService profileService;
        private readonly IExposureDataFactory exposureDataFactory;

        public PlayerOneCamera(int id, string name, string driverVersion, IPlayerOneSDK sdk, IProfileService profileService, IExposureDataFactory exposureDataFactory) {
            this.Name = name;
            this.sdk = sdk;
            this.Category = "Player One";
            this.Description = "Native driver implementation for Player One Cameras";
            this.DriverInfo = "Player One native driver";
            this.DriverVersion = driverVersion;
            this.profileService = profileService;
            this.exposureDataFactory = exposureDataFactory;
            this.Id = Category + "_" + id.ToString();
        }

        public bool Connected {
            get => sdk.Connected;
        }

        private void Initialize() {
            BinningModes = new AsyncObservableCollection<BinningMode>();
            var binnings = sdk.GetBinningInfo();
            if (binnings.Length > 0) {
                Array.Sort(binnings);
                MaxBinX = (short)binnings.Max();
                MaxBinY = (short)binnings.Max();

                foreach (var bin in binnings) {
                    BinningModes.Add(new BinningMode((short)bin, (short)bin));
                }
            } else {
                MaxBinX = 1;
                MaxBinY = 1;
                BinningModes.Add(new BinningMode(1, 1));
            }

            var size = sdk.GetPixelSize();
            if (size <= 0) {
                PixelSizeX = double.NaN;
            } else {
                PixelSizeX = size;
            }

            (CameraXSize, CameraYSize) = sdk.GetDimensions();

            SensorType = sdk.GetSensorInfo();

            CanSetTemperature = sdk.HasTemperatureControl();
        }


        public int BitDepth { get => sdk.GetBitDepth(); }

        public SensorType SensorType { get; private set; }

        public int CameraXSize { get; private set; }

        public int CameraYSize { get; private set; }

        public double PixelSizeX { get; private set; }

        public double PixelSizeY { get => PixelSizeX; }

        public double ExposureMin {
            get {
                return sdk.GetMinExposureTime();
            }
        }

        public double ExposureMax {
            get {
                return Math.Ceiling(sdk.GetMaxExposureTime());
            }
        }

        private short bin = 1;

        public short BinX {
            get {
                return bin;
            }
            set {
                if (value <= 0) { value = 1; }
                if (value > MaxBinX) { value = MaxBinX; }
                bin = value;
                RaisePropertyChanged();
            }
        }

        public short BinY {
            get {
                return bin;
            }
            set {
                if (value <= 0) { value = 1; }
                if (value > MaxBinY) { value = MaxBinY; }
                bin = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<BinningMode> BinningModes { get; private set; }

        public short MaxBinX { get; private set; }

        public short MaxBinY { get; private set; }

        public bool CanGetGain {
            get {
                return true;
            }
        }

        public bool CanSetGain {
            get {
                return true;
            }
        }

        public int Gain {
            get {
                return sdk.GetGain();
            }
            set {
                if (sdk.SetGain(value)) {
                    RaisePropertyChanged();
                }
            }
        }

        public int GainMax {
            get {
                return sdk.GetMaxGain();
            }
        }

        public int GainMin {
            get {
                return sdk.GetMinGain();
            }
        }

        public bool CanSetOffset {
            get {
                return true;
            }
        }

        public bool CanSetUSBLimit {
            get {
                return true;
            }
        }

        public int Offset {
            get {
                return sdk.GetOffset();
            }
            set {
                if (sdk.SetOffset(value)) {
                    RaisePropertyChanged();
                }
            }
        }

        public int OffsetMin {
            get {
                return sdk.GetMinOffset();
            }
        }

        public int OffsetMax {
            get {
                return sdk.GetMaxOffset();
            }
        }

        public int USBLimit {
            get {
                return sdk.GetUSBLimit();
            }
            set {
                if (sdk.SetUSBLimit(value)) {
                    RaisePropertyChanged();
                }
            }
        }

        public int USBLimitMin {
            get {
                return sdk.GetMinUSBLimit();
            }
        }

        public int USBLimitMax {
            get {
                return sdk.GetMaxUSBLimit();
            }
        }

        public int USBLimitStep {
            get {
                return 1;
            }
        }

        public void SetBinning(short x, short y) {
            BinX = x;
            BinY = y;
        }

        public async Task<bool> Connect(CancellationToken token) {
            return await Task.Run(() => {
                try {
                    sdk.Connect();
                    if (Connected) {
                        Initialize();
                        RaisePropertyChanged(nameof(Connected));
                        RaiseAllPropertiesChanged();
                        return true;
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }
                return false;
            });
        }

        public void Disconnect() {
            try {
                sdk.Disconnect();
                RaisePropertyChanged(nameof(Connected));
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        public void StartExposure(CaptureSequence sequence) {
            if (EnableSubSample) {
                sdk.SetROI(SubSampleX, SubSampleY, SubSampleWidth, SubSampleHeight, BinX);
            } else {
                sdk.SetROI(0, 0, CameraXSize, CameraYSize, BinX);
            }

            try { exposureCts?.Dispose(); } catch (Exception) { }

            exposureCts = new CancellationTokenSource();

            var (x, y, width, height, binning) = sdk.GetROI();
            exposureTaskWidth = width;
            exposureTaskHeight = height;
            exposureTaskTime = sequence.ExposureTime;
            sdk.StartExposure(sequence.ExposureTime, width, height);
            
        }

        private CancellationTokenSource exposureCts;
        private int exposureTaskWidth;
        private int exposureTaskHeight;
        private double exposureTaskTime;

        public async Task WaitUntilExposureIsReady(CancellationToken token) {
            using (token.Register(() => AbortExposure())) {
                while(!sdk.IsExposureReady()) {
                    await CoreUtil.Wait(TimeSpan.FromMilliseconds(10), token);
                }
            }
        }

        public void StopExposure() {
            try {
                exposureCts?.Cancel();
            } catch (Exception) { }
        }

        public void AbortExposure() {
            try {
                exposureCts?.Cancel();
            } catch (Exception) { }
        }

        public async Task<IExposureData> DownloadExposure(CancellationToken token) {
            var data = await sdk.GetExposure(exposureTaskTime, exposureTaskWidth, exposureTaskHeight, token);
            if (data == null) { return null; }
            
            var (x, y, width, height, binning) = sdk.GetROI();

            return exposureDataFactory.CreateImageArrayExposureData(
                        input: data,
                        width: width,
                        height: height,
                        bitDepth: this.BitDepth,
                        isBayered: SensorType != SensorType.Monochrome,
                        metaData: new ImageMetaData());
        }

        #region "Temperature Control"

        public bool CanSetTemperature { get; private set; }

        public bool CoolerOn {
            get {
                if (CanSetTemperature) {
                    return sdk.GetCoolerOnOff();
                }
                return false;
            }
            set {
                if (CanSetTemperature) {
                    if (sdk.SetCooler(value)) {
                        RaisePropertyChanged();
                    }
                }
            }
        }

        public double CoolerPower {
            get {
                if (CanSetTemperature) {
                    return sdk.GetCoolerPower();
                }
                return double.NaN;
            }
        }

        public double Temperature {
            get {
                if (CanSetTemperature) {
                    return sdk.GetTemperature();
                }
                return double.NaN;
            }
        }

        public double TemperatureSetPoint {
            get {
                if (CanSetTemperature) {
                    return sdk.GetTargetTemperature();
                }
                return double.NaN;
            }
            set {
                if (CanSetTemperature) {
                    if (sdk.SetTargetTemperature(value)) {
                        RaisePropertyChanged();
                    }
                }
            }
        }

        #endregion "Temperature Control"

        #region "Meta Data"

        public string Id { get; }
        public string Name { get; }
        public string Category { get; }
        public string Description { get; }
        public string DriverInfo { get; }
        public string DriverVersion { get; }

        #endregion "Meta Data"

        #region "Subsample"

        public bool CanSubSample { get => true; }
        public bool EnableSubSample { get; set; }
        public int SubSampleX { get; set; }
        public int SubSampleY { get; set; }
        public int SubSampleWidth { get; set; }
        public int SubSampleHeight { get; set; }

        #endregion "Subsample"

        #region "LiveView"

        // Live view is deprecated
        public bool CanShowLiveView { get => false; }

        public void StartLiveView(CaptureSequence sequence) {
            throw new NotImplementedException();
        }

        public Task<IExposureData> DownloadLiveView(CancellationToken token) {
            throw new NotImplementedException();
        }

        public void StopLiveView() {
            throw new NotImplementedException();
        }

        public bool LiveViewEnabled { get => false; set => throw new NotImplementedException(); }

        #endregion "LiveView"

        #region "Unsupported Features"

        public CameraStates CameraState { get => CameraStates.NoState; }

        public bool HasSetupDialog { get => false; }

        public void SetupDialog() {
        }

        public string SensorName { get => string.Empty; }

        public bool HasShutter { get => false; }

        public bool HasBattery { get => false; }
        public int BatteryLevel { get => -1; }
        public double ElectronsPerADU => double.NaN;
        public short BayerOffsetX { get; } = 0;
        public short BayerOffsetY { get; } = 0;

        public IList<string> ReadoutModes => new List<string> { "Default" };

        public short ReadoutMode {
            get => 0;
            set { }
        }

        public short ReadoutModeForSnapImages { get; set; }

        public short ReadoutModeForNormalImages { get; set; }

        public IList<int> Gains {
            get {
                return new List<int>();
            }
        }

        // So far there is no adjustable dew heater available
        public bool HasDewHeater { get => false; }

        public bool DewHeaterOn { get => false; set { } }

        #endregion "Unsupported Features"
    
}
}
