#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Altair;
using NINA.Core.Enum;
using NINA.Model.ImageData;
using NINA.Model.MyCamera.ToupTekAlike;
using NINA.Profile;
using NINA.Utility;
using NINA.Utility.Notification;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyCamera {

    public class ToupTekAlikeCamera : BaseINPC, ICamera {
        private ToupTekAlikeFlag flags;
        private IToupTekAlikeCameraSDK sdk;

        public ToupTekAlikeCamera(ToupTekAlikeDeviceInfo deviceInfo, IToupTekAlikeCameraSDK sdk, IProfileService profileService) {
            this.profileService = profileService;
            this.sdk = sdk;
            this.Id = deviceInfo.id;
            this.Name = deviceInfo.displayname;
            this.Description = deviceInfo.model.name;
            this.PixelSizeX = Math.Round(deviceInfo.model.xpixsz, 2);
            this.PixelSizeY = Math.Round(deviceInfo.model.ypixsz, 2);

            this.flags = (ToupTekAlikeFlag)deviceInfo.model.flag;

            Category = sdk.Category;
        }

        private IProfileService profileService;

        public string Category { get; }

        public bool HasShutter {
            get {
                return false;
            }
        }

        public double Temperature {
            get {
                sdk.get_Temperature(out var temp);
                return temp / 10.0;
            }
        }

        public double TemperatureSetPoint {
            get {
                if (CanSetTemperature) {
                    sdk.get_Option(ToupTekAlikeOption.OPTION_TECTARGET, out var target);
                    return target / 10.0;
                } else {
                    return double.NaN;
                }
            }
            set {
                if (CanSetTemperature) {
                    if (sdk.put_Option(ToupTekAlikeOption.OPTION_TECTARGET, (int)(value * 10))) {
                        RaisePropertyChanged();
                    }
                }
            }
        }

        private short binX;

        public short BinX {
            get {
                return binX;
            }

            set {
                binX = value;
                RaisePropertyChanged();
            }
        }

        private short binY;

        public short BinY {
            get {
                return binY;
            }

            set {
                binY = value;
                RaisePropertyChanged();
            }
        }

        public string SensorName {
            get {
                return string.Empty;
            }
        }

        public SensorType SensorType { get; private set; }

        public short BayerOffsetX => 0;

        public short BayerOffsetY => 0;

        public int CameraXSize { get; private set; }

        public int CameraYSize { get; private set; }

        public double ExposureMin {
            get {
                sdk.get_ExpTimeRange(out var min, out var max, out var def);
                return min / 1000000.0;
            }
        }

        public double ExposureMax {
            get {
                sdk.get_ExpTimeRange(out var min, out var max, out var def);
                return max / 1000000.0;
            }
        }

        public double ElectronsPerADU => double.NaN;

        public short MaxBinX { get; private set; }

        public short MaxBinY { get; private set; }

        public double PixelSizeX { get; }

        public double PixelSizeY { get; }

        private bool canGetTemperature;

        public bool CanGetTemperature {
            get {
                return canGetTemperature;
            }
            private set {
                canGetTemperature = value;
                RaisePropertyChanged();
            }
        }

        private bool canSetTemperature;

        public bool CanSetTemperature {
            get {
                return canSetTemperature;
            }
            private set {
                canSetTemperature = value;
                RaisePropertyChanged();
            }
        }

        public bool CoolerOn {
            get {
                sdk.get_Option(ToupTekAlikeOption.OPTION_TEC, out var cooler);
                return cooler == 1;
            }

            set {
                if (sdk.put_Option(ToupTekAlikeOption.OPTION_TEC, value ? 1 : 0)) {
                    //Toggle fan, if supported
                    sdk.put_Option(ToupTekAlikeOption.OPTION_FAN, value ? 1 : 0);
                    RaisePropertyChanged();
                }
            }
        }

        private double coolerPower = 0.0;

        public double CoolerPower {
            get {
                return coolerPower;
            }
            private set {
                coolerPower = value;
                RaisePropertyChanged();
            }
        }

        private CancellationTokenSource coolerPowerReadoutCts;

        /// <summary>
        /// This task will update cooler power based on TEC Volatage readout every three seconds
        /// Due to the fact that this value must not be updated more than every two seconds according to the documentation
        /// a helper method is required in case the device polling interval is faster than that.
        /// </summary>
        private void CoolerPowerUpdateTask() {
            Task.Run(async () => {
                coolerPowerReadoutCts?.Dispose();
                coolerPowerReadoutCts = new CancellationTokenSource();
                try {
                    sdk.get_Option(ToupTekAlikeOption.OPTION_TEC_VOLTAGE_MAX, out var maxVoltage);
                    while (true) {
                        coolerPowerReadoutCts.Token.ThrowIfCancellationRequested();

                        sdk.get_Option(ToupTekAlikeOption.OPTION_TEC_VOLTAGE, out var voltage);

                        CoolerPower = 100 * voltage / (double)maxVoltage;

                        //Recommendation to not readout CoolerPower in less than two seconds.
                        await Task.Delay(TimeSpan.FromSeconds(3), coolerPowerReadoutCts.Token);
                    }
                } catch (OperationCanceledException) {
                }
            });
        }

        private bool hasDewHeater;

        public bool HasDewHeater {
            get => hasDewHeater;
            private set {
                hasDewHeater = value;
                RaisePropertyChanged();
            }
        }

        public bool DewHeaterOn {
            get {
                if (HasDewHeater) {
                    sdk.get_Option(ToupTekAlikeOption.OPTION_HEAT, out var heat);
                    return heat > 0;
                } else {
                    return false;
                }
            }
            set {
                if (HasDewHeater) {
                    if (value) {
                        sdk.get_Option(ToupTekAlikeOption.OPTION_HEAT_MAX, out var max);
                        sdk.put_Option(ToupTekAlikeOption.OPTION_HEAT, max);
                    } else {
                        sdk.put_Option(ToupTekAlikeOption.OPTION_HEAT, 0);
                    }
                    RaisePropertyChanged();
                }
            }
        }

        public string CameraState {
            get {
                /* No State available */
                return string.Empty;
            }
        }

        public bool CanSubSample {
            get {
                /*
                 Currently ToupTek Cameras are fixed to certain subsample resolutions, which is incompatible to NINA's approach
                 */
                return false;
            }
        }

        public bool EnableSubSample { get; set; }
        public int SubSampleX { get; set; }
        public int SubSampleY { get; set; }
        public int SubSampleWidth { get; set; }
        public int SubSampleHeight { get; set; }
        public bool CanShowLiveView { get => false; }
        public bool LiveViewEnabled { get => false; set => throw new NotImplementedException(); }

        public bool HasBattery {
            get {
                return false;
            }
        }

        public int BatteryLevel {
            get {
                return -1;
            }
        }

        public int Offset {
            get {
                sdk.get_Option(ToupTekAlikeOption.OPTION_BLACKLEVEL, out var level);
                return level;
            }
            set {
                if (!sdk.put_Option(ToupTekAlikeOption.OPTION_BLACKLEVEL, value)) {
                } else {
                    RaisePropertyChanged();
                }
            }
        }

        public int OffsetMin {
            get {
                return 0;
            }
        }

        public int OffsetMax {
            get {
                return 31 * (1 << BitDepth - 8);
            }
        }

        public int USBLimit {
            get {
                sdk.get_Speed(out var speed);
                return speed;
            }
            set {
                if (value >= USBLimitMin && value <= USBLimitMax) {
                    sdk.put_Speed((ushort)value);
                    RaisePropertyChanged();
                }
            }
        }

        public int USBLimitMin {
            get {
                return 0;
            }
        }

        public int USBLimitMax {
            get {
                return (int)sdk.MaxSpeed;
            }
        }

        private bool canSetOffset;

        public bool CanSetOffset {
            get {
                return canSetOffset;
            }
            set {
                canSetOffset = value;
                RaisePropertyChanged();
            }
        }

        public bool CanSetUSBLimit {
            get {
                return true;
            }
        }

        public bool CanGetGain {
            get {
                return sdk.get_ExpoAGain(out var gain);
            }
        }

        public bool CanSetGain {
            get {
                return GainMax != GainMin;
            }
        }

        public int GainMax {
            get {
                sdk.get_ExpoAGainRange(out var min, out var max, out var def);
                return max;
            }
        }

        public int GainMin {
            get {
                sdk.get_ExpoAGainRange(out var min, out var max, out var def);
                return min;
            }
        }

        public int Gain {
            get {
                sdk.get_ExpoAGain(out var gain);
                return gain;
            }

            set {
                if (value >= GainMin && value <= GainMax) {
                    sdk.put_ExpoAGain((ushort)value);
                    RaisePropertyChanged();
                }
            }
        }

        public IList<string> ReadoutModes => new List<string> { "Default" };

        public short ReadoutMode {
            get => 0;
            set { }
        }

        private short _readoutModeForSnapImages = 0;

        public short ReadoutModeForSnapImages {
            get => _readoutModeForSnapImages;
            set {
                _readoutModeForSnapImages = value;
                RaisePropertyChanged();
            }
        }

        private short _readoutModeForNormalImages = 0;

        public short ReadoutModeForNormalImages {
            get => _readoutModeForNormalImages;
            set {
                _readoutModeForNormalImages = value;
                RaisePropertyChanged();
            }
        }

        public IList<int> Gains {
            get {
                return new List<int>();
            }
        }

        private AsyncObservableCollection<BinningMode> binningModes;

        public AsyncObservableCollection<BinningMode> BinningModes {
            get {
                if (binningModes == null) {
                    binningModes = new AsyncObservableCollection<BinningMode>();
                }
                return binningModes;
            }
            private set {
                binningModes = value;
                RaisePropertyChanged();
            }
        }

        public bool HasSetupDialog => false;

        private string id;

        public string Id {
            get {
                return id;
            }
            set {
                id = value;
                RaisePropertyChanged();
            }
        }

        private string name;

        public string Name {
            get {
                return name;
            }
            set {
                name = value;
                RaisePropertyChanged();
            }
        }

        private bool _connected;

        public bool Connected {
            get {
                return _connected;
            }
            set {
                _connected = value;
                if (!_connected) {
                    coolerPowerReadoutCts?.Cancel();
                }

                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HasSetupDialog));
            }
        }

        private string description;

        public string Description {
            get {
                return description;
            }
            set {
                description = value;
                RaisePropertyChanged();
            }
        }

        public string DriverInfo {
            get {
                return $"{Category} SDK";
            }
        }

        public string DriverVersion {
            get {
                return sdk?.Version() ?? string.Empty;
            }
        }

        public void AbortExposure() {
            StopExposure();
        }

        private void ReadOutBinning() {
            /* Found no way to readout available binning modes. Assume 4x4 for all cams for now */
            BinningModes.Clear();
            MaxBinX = 4;
            MaxBinY = 4;
            for (short i = 1; i <= MaxBinX; i++) {
                BinningModes.Add(new BinningMode(i, i));
            }
            BinX = 1;
            BinY = 1;
        }

        public Task<bool> Connect(CancellationToken ct) {
            return Task<bool>.Run(() => {
                var success = false;
                try {
                    imageReadyTCS?.TrySetCanceled();
                    imageReadyTCS = null;

                    sdk = sdk.Open(this.Id);
                    success = true;

                    /* Use maximum bit depth */
                    if (!sdk.put_Option(ToupTekAlikeOption.OPTION_BITDEPTH, 1)) {
                        throw new Exception($"{Category} - Could not set bit depth");
                    }

                    /* Use RAW Mode */
                    if (!sdk.put_Option(ToupTekAlikeOption.OPTION_RAW, 1)) {
                        throw new Exception($"{Category} - Could not set RAW mode");
                    }

                    sdk.put_AutoExpoEnable(false);

                    ReadOutBinning();

                    sdk.get_Size(out var width, out var height);
                    this.CameraXSize = width;
                    this.CameraYSize = height;

                    /* Readout flags */
                    if ((this.flags & ToupTekAlikeFlag.FLAG_TEC_ONOFF) != 0) {
                        /* Can set Target Temp */
                        CanSetTemperature = true;
                        sdk.get_Option(ToupTekAlikeOption.OPTION_TECTARGET, out var target);
                        if (target >= -280 && target <= 100) {
                            TemperatureSetPoint = target;
                        } else {
                            TemperatureSetPoint = 20;
                        }
                        CoolerPowerUpdateTask();
                    }

                    if ((this.flags & ToupTekAlikeFlag.FLAG_GETTEMPERATURE) != 0) {
                        /* Can get Target Temp */
                        CanGetTemperature = true;
                    }

                    if ((this.flags & ToupTekAlikeFlag.FLAG_BLACKLEVEL) != 0) {
                        CanSetOffset = true;
                    }

                    if ((this.flags & ToupTekAlikeFlag.FLAG_HEAT) != 0) {
                        HasDewHeater = true;
                    }

                    if ((this.flags & ToupTekAlikeFlag.FLAG_LOW_NOISE) != 0) {
                        HasLowNoiseMode = true;
                    }

                    if (((this.flags & ToupTekAlikeFlag.FLAG_CG) != 0) || ((this.flags & ToupTekAlikeFlag.FLAG_CGHDR) != 0)) {
                        Logger.Trace($"{Category} - Camera has High Conversion Gain option");
                        HasHighGain = true;
                        HighGainMode = true;
                    } else {
                        HasHighGain = false;
                    }

                    if ((this.flags & ToupTekAlikeFlag.FLAG_TRIGGER_SOFTWARE) == 0) {
                        throw new Exception($"{Category} - This camera is not capable to be triggered by software and is not supported");
                    }

                    if (!sdk.put_Option(ToupTekAlikeOption.OPTION_TRIGGER, 1)) {
                        throw new Exception($"{Category} - Could not set Trigger manual mode");
                    }

                    if (!sdk.StartPullModeWithCallback(new ToupTekAlikeCallback(OnEventCallback))) {
                        throw new Exception($"{Category} - Could not start pull mode");
                    }

                    if (!sdk.put_Option(ToupTekAlikeOption.OPTION_FLUSH, 3)) {
                        Logger.Debug($"{Category} - Unable to flush camera");
                    }

                    if (!sdk.get_RawFormat(out var fourCC, out var bitDepth)) {
                        throw new Exception($"{Category} - Unable to get format information");
                    } else {
                        if (sdk.MonoMode) {
                            SensorType = SensorType.Monochrome;
                        } else {
                            SensorType = GetSensorType(fourCC);
                        }
                    }

                    this.BitDepth = (int)bitDepth;

                    Connected = true;
                    RaiseAllPropertiesChanged();
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
                }
                return success;
            });
        }

        private SensorType GetSensorType(uint fourCC) {
            var bytes = BitConverter.GetBytes(fourCC);
            if (!BitConverter.IsLittleEndian) { Array.Reverse(bytes); }

            var sensor = System.Text.Encoding.ASCII.GetString(bytes);
            if (Enum.TryParse(sensor, true, out SensorType sensorType)) {
                return sensorType;
            }
            return SensorType.RGGB;
        }

        private bool _hasLowNoiseMode;

        public bool HasLowNoiseMode {
            get => _hasLowNoiseMode;
            set {
                _hasLowNoiseMode = value;
                RaisePropertyChanged();
            }
        }

        public bool LowNoiseMode {
            get {
                if (HasLowNoiseMode) {
                    sdk.get_Option(ToupTekAlikeOption.OPTION_LOW_NOISE, out var value);
                    Logger.Trace($"{Category} - Low Noise Mode is set to {value}");
                    return value == 1;
                } else {
                    return false;
                }
            }
            set {
                if (HasLowNoiseMode) {
                    Logger.Debug($"{Category} - Setting Low Noise Mode to {value}");
                    sdk.put_Option(ToupTekAlikeOption.OPTION_LOW_NOISE, value ? 1 : 0);
                    RaisePropertyChanged();
                }
            }
        }

        private bool _hasHighGain;

        public bool HasHighGain {
            get => _hasHighGain;
            set {
                _hasHighGain = value;
                RaisePropertyChanged();
            }
        }

        public bool HighGainMode {
            get {
                if (HasHighGain) {
                    sdk.get_Option(ToupTekAlikeOption.OPTION_CG, out var value);
                    Logger.Trace($"{Category} - Conversion Gain is set to {value}");
                    return value == 1 ? true : false;
                } else {
                    return false;
                }
            }
            set {
                if (HasHighGain) {
                    Logger.Trace($"{Category} - Setting Conversion Gain to {value}");
                    sdk.put_Option(ToupTekAlikeOption.OPTION_CG, value ? 1 : 0);
                    RaisePropertyChanged();
                }
            }
        }

        private void OnEventCallback(ToupTekAlikeEvent nEvent) {
            Logger.Trace($"{Category} - OnEventCallback {nEvent}");
            switch (nEvent) {
                case ToupTekAlikeEvent.EVENT_IMAGE: // Live View Image
                    var id = imageReadyTCS?.Task?.Id ?? -1;
                    Logger.Trace("{Category} - Setting DownloadExposure Result on Task {id}");
                    var success = imageReadyTCS?.TrySetResult(true);
                    Logger.Trace($"{Category} - DownloadExposure Result on Task {id} set successfully: {success}");
                    break;

                case ToupTekAlikeEvent.EVENT_STILLIMAGE: // Still Image
                    Logger.Warning($"{Category} - Still image event received, but not expected to get one!");
                    imageReadyTCS?.TrySetResult(true);
                    break;

                case ToupTekAlikeEvent.EVENT_NOFRAMETIMEOUT:
                    Logger.Error($"{Category} - Timout event occurred!");
                    break;

                case ToupTekAlikeEvent.EVENT_TRIGGERFAIL:
                    Logger.Error($"{Category} - Trigger Fail event received!");
                    break;

                case ToupTekAlikeEvent.EVENT_ERROR: // Error
                    Logger.Error($"{Category} - Camera reported a generic error!");
                    Notification.ShowError("Camera reported a generic error and needs to be reconnected!");
                    Disconnect();
                    break;

                case ToupTekAlikeEvent.EVENT_DISCONNECTED:
                    Logger.Warning($"{Category} - Camera disconnected! Maybe USB connection was interrupted.");
                    Notification.ShowError("Camera disconnected! Maybe USB connection was interrupted.");
                    OnEventDisconnected();
                    break;
            }
        }

        private IExposureData PullImage() {
            /* peek the width and height */
            sdk.get_Option(ToupTekAlikeOption.OPTION_BINNING, out var binning);
            var width = CameraXSize / binning;
            var height = CameraYSize / binning;

            var size = width * height;
            var data = new ushort[size];

            if (!sdk.PullImageV2(data, BitDepth, out var info)) {
                Logger.Error($"{Category} - Failed to pull image");
                return null;
            }

            if (!sdk.put_Option(ToupTekAlikeOption.OPTION_FLUSH, 3)) {
                Logger.Debug($"{Category} - Unable to flush camera");
            }

            var bitScaling = this.profileService.ActiveProfile.CameraSettings.BitScaling;
            if (bitScaling) {
                var shift = 16 - BitDepth;
                for (var i = 0; i < data.Length; i++) {
                    data[i] = (ushort)(data[i] << shift);
                }
            }

            var imageData = new ImageArrayExposureData(
                    input: data,
                    width: width,
                    height: height,
                    bitDepth: bitScaling ? 16 : this.BitDepth,
                    isBayered: this.SensorType != SensorType.Monochrome,
                    metaData: new ImageMetaData());

            return imageData;
        }

        public void Disconnect() {
            coolerPowerReadoutCts?.Cancel();
            Connected = false;
            sdk.Close();
        }

        public async Task WaitUntilExposureIsReady(CancellationToken token) {
            using (token.Register(() => AbortExposure())) {
                await imageReadyTCS.Task;
            }
        }

        public async Task<IExposureData> DownloadExposure(CancellationToken token) {
            if (imageReadyTCS.Task.IsCanceled) { return null; }
            using (token.Register(() => imageReadyTCS.TrySetCanceled())) {
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15))) {
                    using (cts.Token.Register(() => { Logger.Error($"{Category} - No Image Callback Event received"); imageReadyTCS.TrySetResult(true); })) {
                        var imageReady = await imageReadyTCS.Task;
                        return PullImage();
                    }
                }
            }
        }

        public Task<IExposureData> DownloadLiveView(CancellationToken token) {
            throw new System.NotImplementedException();
        }

        public void SetBinning(short x, short y) {
            if (x <= MaxBinX) {
                sdk.put_Option(ToupTekAlikeOption.OPTION_BINNING, x);
            }
        }

        public void SetupDialog() {
        }

        /// <summary>
        /// Sets the exposure time. When given exposure time is out of bounds it will set it to nearest bound.
        /// </summary>
        /// <param name="time">Time in seconds</param>
        private void SetExposureTime(double time) {
            if (time < ExposureMin) {
                time = ExposureMin;
            }
            if (time > ExposureMax) {
                time = ExposureMax;
            }

            var µsTime = (uint)(time * 1000000);
            if (!sdk.put_ExpoTime(µsTime)) {
                throw new Exception($"{Category} - Could not set exposure time");
            }
        }

        public void StartExposure(CaptureSequence sequence) {
            imageReadyTCS?.TrySetCanceled();
            imageReadyTCS = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            Logger.Trace($"{Category} - created new downloadExposure Task with Id {imageReadyTCS.Task.Id}");

            SetExposureTime(sequence.ExposureTime);

            if (!sdk.Trigger(1)) {
                throw new Exception($"{Category} - Failed to trigger camera");
            }
        }

        private TaskCompletionSource<bool> imageReadyTCS;
        private int bitDepth;

        public int BitDepth {
            get {
                return bitDepth;
            }
            private set {
                bitDepth = value;
                RaisePropertyChanged();
            }
        }

        private void OnEventDisconnected() {
            StopExposure();
            Disconnect();
        }

        public void StartLiveView() {
            throw new System.NotImplementedException();
        }

        public void StopExposure() {
            if (!sdk.Trigger(0)) {
                Logger.Warning($"{Category} - Could not stop exposure");
            }
            imageReadyTCS.TrySetCanceled();
        }

        public void StopLiveView() {
            throw new System.NotImplementedException();
        }

        public int USBLimitStep { get => 1; }
    }
}