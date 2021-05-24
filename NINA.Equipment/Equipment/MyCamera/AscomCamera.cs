#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using ASCOM;
using ASCOM.DeviceInterface;
using ASCOM.DriverAccess;
using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SensorType = NINA.Core.Enum.SensorType;
using NINA.Core.Model.Equipment;
using NINA.Equipment.Utility;
using NINA.Core.Locale;
using NINA.Equipment.Model;
using NINA.Image.Interfaces;
using NINA.Image.ImageData;
using NINA.Equipment.Interfaces;

namespace NINA.Equipment.Equipment.MyCamera {

    internal class AscomCamera : AscomDevice<Camera>, ICamera, IDisposable {

        public AscomCamera(string cameraId, string name, IProfileService profileService) : base(cameraId, name) {
            this.profileService = profileService;
        }

        private IProfileService profileService;

        private void Initialize() {
            _hasBayerOffset = true;
            _hasCCDTemperature = true;
            _hasCooler = true;
            CanSetGain = true;
            CanGetGain = true;
            _canGetGainMinMax = true;
            _hasLastExposureInfo = true;
            _hasPercentCompleted = true;
            BinningModes = new AsyncObservableCollection<BinningMode>();
            for (short i = 1; i <= MaxBinX; i++) {
                if (CanAsymmetricBin) {
                    for (short j = 1; j <= MaxBinY; j++) {
                        BinningModes.Add(new BinningMode(i, j));
                    }
                } else {
                    BinningModes.Add(new BinningMode(i, i));
                }
            }

            Gains.Clear();
            try {
                var gains = device.Gains;
                foreach (object o in device.Gains) {
                    if (o.GetType() == typeof(string)) {
                        var gain = Regex.Match(o.ToString(), @"\d+").Value;
                        Gains.Add(int.Parse(gain, CultureInfo.InvariantCulture));
                    }
                }
            } catch (Exception) {
            }

            //Determine Offset Capabilities
            try {
                //Check if Offset is implemented at all in ICameraV3 Driver
                _ = device.Offset;
                CanSetOffset = true;
            } catch (PropertyNotImplementedException) {
                Logger.Trace("Offset is not implemented in this driver");
                CanSetOffset = false;
            } catch (ASCOM.InvalidOperationException) {
                Logger.Trace("Offset is not implemented in this driver");
                CanSetOffset = false;
            } catch (Exception) {
                Logger.Trace("Offset is not implemented in this driver");
                CanSetOffset = false;
            }

            if (CanSetOffset) {
                try {
                    //Check if offset mode is in value mode
                    _ = device.OffsetMin;
                    offsetValueMode = true;
                } catch (PropertyNotImplementedException) {
                    Logger.Trace("Offset operates in index mode");
                    offsetValueMode = false;
                }

                if (!offsetValueMode) {
                    try {
                        //Check when mode is likely to be index mode if the offsets property is implemented
                        var arrayListOffsets = device.Offsets;

                        //Map offsets to integer values
                        offsets.Clear();
                        try {
                            foreach (object o in arrayListOffsets) {
                                if (o.GetType() == typeof(string)) {
                                    var offset = Regex.Match(o.ToString(), @"\d+").Value;
                                    offsets.Add(int.Parse(offset, CultureInfo.InvariantCulture));
                                }
                            }
                        } catch (Exception) {
                        }
                    } catch (PropertyNotImplementedException) {
                        Logger.Error("Offset is implemented, but neither OffsetMin/Max nor Offsets are implemented.");
                        CanSetOffset = false;
                    }
                }
            }
        }

        public bool CanSubSample {
            get {
                return true;
            }
        }

        public bool EnableSubSample { get; set; }
        public int SubSampleX { get; set; }
        public int SubSampleY { get; set; }
        public int SubSampleWidth { get; set; }
        public int SubSampleHeight { get; set; }

        private bool _hasBayerOffset;

        public short BayerOffsetX {
            get {
                short offset = -1;
                try {
                    if (Connected && _hasBayerOffset) {
                        offset = device.BayerOffsetX;
                    }
                } catch (PropertyNotImplementedException) {
                    _hasBayerOffset = false;
                }
                return offset;
            }
        }

        public short BayerOffsetY {
            get {
                short offset = -1;
                try {
                    if (Connected && _hasBayerOffset) {
                        offset = device.BayerOffsetY;
                    }
                } catch (PropertyNotImplementedException) {
                    _hasBayerOffset = false;
                }
                return offset;
            }
        }

        public short BinX {
            get {
                if (Connected) {
                    return device.BinX;
                } else {
                    return -1;
                }
            }
            set {
                if (Connected) {
                    try {
                        device.BinX = value;
                    } catch (InvalidValueException ex) {
                        Logger.Error(ex);
                        Notification.ShowError(ex.Message);
                    }
                    RaisePropertyChanged();
                }
            }
        }

        public short BinY {
            get {
                if (Connected) {
                    return device.BinY;
                } else {
                    return -1;
                }
            }
            set {
                if (Connected) {
                    try {
                        device.BinY = value;
                    } catch (InvalidValueException ex) {
                        Logger.Error(ex);
                        Notification.ShowError(ex.Message);
                    }
                    RaisePropertyChanged();
                }
            }
        }

        public string CameraState {
            get {
                string state;
                try {
                    if (Connected) {
                        state = device.CameraState.ToString();
                    } else {
                        state = CameraStates.cameraIdle.ToString();
                    }
                } catch (NotConnectedException ex) {
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
                    state = CameraStates.cameraError.ToString();
                }
                return state;
            }
        }

        public int CameraXSize {
            get {
                int size = -1;
                if (Connected) {
                    size = device.CameraXSize;
                }
                return size;
            }
        }

        public int CameraYSize {
            get {
                int size = -1;
                if (Connected) {
                    size = device.CameraYSize;
                }
                return size;
            }
        }

        public bool CanAbortExposure {
            get {
                if (Connected) {
                    return device.CanAbortExposure;
                } else {
                    return false;
                }
            }
        }

        public bool CanShowLiveView { get => false; }

        public bool CanAsymmetricBin {
            get {
                if (Connected) {
                    return device.CanAsymmetricBin;
                } else {
                    return false;
                }
            }
        }

        public bool CanFastReadout {
            get {
                if (Connected) {
                    try {
                        return device.CanFastReadout;
                    } catch (PropertyNotImplementedException) {
                        ASCOMInteraction.LogComplianceIssue($"{nameof(CanFastReadout)} GET");
                    }
                }
                return false;
            }
        }

        public bool CanGetCoolerPower {
            get {
                if (Connected) {
                    return device.CanGetCoolerPower;
                } else {
                    return false;
                }
            }
        }

        public bool CanPulseGuide {
            get {
                if (Connected) {
                    return device.CanPulseGuide;
                } else {
                    return false;
                }
            }
        }

        public bool CanSetTemperature {
            get {
                if (Connected) {
                    return device.CanSetCCDTemperature;
                } else {
                    return false;
                }
            }
        }

        public bool CanStopExposure {
            get {
                if (Connected) {
                    return device.CanStopExposure;
                } else {
                    return false;
                }
            }
        }

        private bool _hasCCDTemperature;

        public double Temperature {
            get {
                double val = -1;
                try {
                    if (Connected && _hasCCDTemperature) {
                        val = device.CCDTemperature;
                    }
                } catch (InvalidValueException) {
                    _hasCCDTemperature = false;
                } catch (PropertyNotImplementedException) {
                    _hasCCDTemperature = false;
                }
                return val;
            }
        }

        private bool _hasCooler;

        public bool CoolerOn {
            get {
                bool val = false;
                try {
                    if (Connected && _hasCooler) {
                        val = device.CoolerOn;
                    }
                } catch (Exception) {
                    _hasCooler = false;
                }
                return val;
            }
            set {
                try {
                    if (Connected && _hasCooler) {
                        device.CoolerOn = value;
                        RaisePropertyChanged();
                    }
                } catch (Exception) {
                    _hasCooler = false;
                }
            }
        }

        public double CoolerPower {
            get {
                if (Connected && CanGetCoolerPower) {
                    return device.CoolerPower;
                } else {
                    return -1;
                }
            }
        }

        public bool HasDewHeater {
            get {
                return false;
            }
        }

        public bool DewHeaterOn {
            get {
                return false;
            }
            set {
            }
        }

        private bool canReadElectronsPerADU = true;

        public double ElectronsPerADU {
            get {
                double val = double.NaN;
                if (canReadElectronsPerADU && Connected) {
                    try {
                        val = device.ElectronsPerADU;
                    } catch (Exception) {
                        val = double.NaN;
                        canReadElectronsPerADU = false;
                    }
                }
                return val;
            }
        }

        public double ExposureMax {
            get {
                double val = -1;
                if (Connected) {
                    try {
                        val = device.ExposureMax;
                    } catch (InvalidValueException) {
                    }
                }
                return val;
            }
        }

        public double ExposureMin {
            get {
                double val = -1;
                if (Connected) {
                    try {
                        val = device.ExposureMin;
                    } catch (InvalidValueException) {
                    }
                }
                return val;
            }
        }

        public double ExposureResolution {
            get {
                double val = -1;
                if (Connected) {
                    try {
                        val = device.ExposureResolution;
                    } catch (InvalidValueException) {
                    }
                }
                return val;
            }
        }

        public bool FastReadout {
            get {
                bool val = false;
                if (Connected && CanFastReadout) {
                    try {
                        val = device.FastReadout;
                    } catch (PropertyNotImplementedException) {
                        ASCOMInteraction.LogComplianceIssue($"{nameof(FastReadout)} GET");
                    }
                }
                return val;
            }
            set {
                if (Connected && CanFastReadout) {
                    try {
                        device.FastReadout = value;
                    } catch (PropertyNotImplementedException) {
                        ASCOMInteraction.LogComplianceIssue($"{nameof(FastReadout)} SET");
                    }
                }
            }
        }

        public double FullWellCapacity {
            get {
                double val = -1;
                if (Connected) {
                    val = device.FullWellCapacity;
                }
                return val;
            }
        }

        private bool _canGetGain;

        public bool CanGetGain {
            get {
                return _canGetGain;
            }
            set {
                _canGetGain = value;
                RaisePropertyChanged();
            }
        }

        private bool _canSetGain;

        public bool CanSetGain {
            get {
                return _canSetGain;
            }
            set {
                _canSetGain = value;
                RaisePropertyChanged();
            }
        }

        public int Gain {
            get {
                int val = -1;
                if (Connected && CanGetGain) {
                    try {
                        if (Gains.Count > 0) {
                            val = (int)Gains[device.Gain];
                        } else {
                            val = device.Gain;
                        }
                    } catch (PropertyNotImplementedException) {
                        CanGetGain = false;
                        Logger.Info("ASCOM - Driver does not implement Gain GET");
                    }
                }
                return val;
            }
            set {
                if (Connected && CanSetGain) {
                    try {
                        if (Gains.Count > 0) {
                            short idx = (short)Gains.IndexOf(value);
                            if (idx >= 0) {
                                device.Gain = idx;
                            }
                        } else {
                            device.Gain = (short)value;
                        }
                    } catch (PropertyNotImplementedException) {
                        CanSetGain = false;
                        Logger.Info("ASCOM - Driver does not implement Gain SET");
                    } catch (InvalidValueException ex) {
                        Logger.Error(ex.Message);
                        Notification.ShowWarning(ex.Message);
                    } catch (Exception) {
                        CanSetGain = false;
                    }
                    RaisePropertyChanged();
                }
            }
        }

        public IList<string> ReadoutModes {
            get {
                IList<string> readoutModes = new List<string>();

                if (!CanFastReadout && ReadoutModesArrayList.Count > 1) {
                    foreach (string mode in ReadoutModesArrayList) {
                        readoutModes.Add(mode);
                    }
                } else if (CanFastReadout) {
                    readoutModes.Add(Loc.Instance["LblNormal"]);
                    readoutModes.Add(Loc.Instance["LblFast"]);
                } else {
                    readoutModes.Add("Default");
                }

                return readoutModes;
            }
        }

        private short readoutModeForSnapImages;

        public short ReadoutModeForSnapImages {
            get => readoutModeForSnapImages;
            set {
                readoutModeForSnapImages = value;
                RaisePropertyChanged();
            }
        }

        private short readoutModeForNormalImages;

        public short ReadoutModeForNormalImages {
            get => readoutModeForNormalImages;
            set {
                readoutModeForNormalImages = value;
                RaisePropertyChanged();
            }
        }

        private bool _canGetGainMinMax;

        public int GainMax {
            get {
                int val = -1;
                if (Connected) {
                    if (_canGetGainMinMax) {
                        try {
                            val = device.GainMax;
                        } catch (PropertyNotImplementedException) {
                            _canGetGainMinMax = false;
                        } catch (ASCOM.InvalidOperationException) {
                            _canGetGainMinMax = false;
                        }
                    }

                    if (!_canGetGainMinMax) {
                        if (this.Gains.Count > 0) {
                            val = Gains.Aggregate((l, r) => l > r ? l : r);
                        }
                    }
                }
                return val;
            }
        }

        public int GainMin {
            get {
                int val = -1;
                if (Connected) {
                    if (_canGetGainMinMax) {
                        try {
                            val = device.GainMin;
                        } catch (PropertyNotImplementedException) {
                            _canGetGainMinMax = false;
                        } catch (ASCOM.InvalidOperationException) {
                            _canGetGainMinMax = false;
                        }
                    }

                    if (!_canGetGainMinMax) {
                        if (this.Gains.Count > 0) {
                            val = Gains.Aggregate((l, r) => l < r ? l : r);
                        }
                    }
                }
                return val;
            }
        }

        private IList<int> _gains;

        public IList<int> Gains {
            get {
                if (_gains == null) {
                    _gains = new List<int>();
                }
                return _gains;
            }
        }

        public bool HasShutter {
            get {
                if (Connected) {
                    return device.HasShutter;
                } else {
                    return false;
                }
            }
        }

        public double HeatSinkTemperature {
            get {
                if (Connected) {
                    return device.HeatSinkTemperature;
                } else {
                    return double.MinValue;
                }
            }
        }

        public object ImageArray {
            get {
                if (Connected) {
                    return device.ImageArray;
                } else {
                    return null;
                }
            }
        }

        public object ImageArrayVariant {
            get {
                if (Connected) {
                    return device.ImageArrayVariant;
                } else {
                    return null;
                }
            }
        }

        public bool ImageReady {
            get {
                if (Connected) {
                    return device.ImageReady;
                } else {
                    return false;
                }
            }
        }

        public short InterfaceVersion {
            get {
                short val = -1;
                try {
                    val = device.InterfaceVersion;
                } catch (DriverException) {
                }
                return val;
            }
        }

        public bool IsPulseGuiding {
            get {
                if (Connected) {
                    return device.IsPulseGuiding;
                } else {
                    return false;
                }
            }
        }

        private bool _hasLastExposureInfo;

        public double LastExposureDuration {
            get {
                double val = -1;
                try {
                    if (Connected && _hasLastExposureInfo) {
                        val = device.LastExposureDuration;
                    }
                } catch (ASCOM.InvalidOperationException) {
                } catch (PropertyNotImplementedException) {
                    _hasLastExposureInfo = false;
                }
                return val;
            }
        }

        public string LastExposureStartTime {
            get {
                string val = string.Empty;
                try {
                    if (Connected && _hasLastExposureInfo) {
                        val = device.LastExposureStartTime;
                    }
                } catch (ASCOM.InvalidOperationException) {
                } catch (PropertyNotImplementedException) {
                    _hasLastExposureInfo = false;
                }
                return val;
            }
        }

        public int MaxADU {
            get {
                if (Connected) {
                    return device.MaxADU;
                } else {
                    return -1;
                }
            }
        }

        public short MaxBinX {
            get {
                if (Connected) {
                    return device.MaxBinX;
                } else {
                    return -1;
                }
            }
        }

        public short MaxBinY {
            get {
                if (Connected) {
                    return device.MaxBinY;
                } else {
                    return -1;
                }
            }
        }

        public int NumX {
            get {
                if (Connected) {
                    return device.NumX;
                } else {
                    return -1;
                }
            }
            set {
                if (Connected) {
                    if (!profileService.ActiveProfile.CameraSettings.ASCOMAllowUnevenPixelDimension || SensorType != SensorType.Monochrome) {
                        if (value % 2 > 0) {
                            value--;
                        }
                    }
                    device.NumX = value;
                    RaisePropertyChanged();
                }
            }
        }

        public int NumY {
            get {
                if (Connected) {
                    return device.NumY;
                } else {
                    return -1;
                }
            }
            set {
                if (Connected) {
                    if (!profileService.ActiveProfile.CameraSettings.ASCOMAllowUnevenPixelDimension || SensorType != SensorType.Monochrome) {
                        if (value % 2 > 0) {
                            value--;
                        }
                    }
                    device.NumY = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool _hasPercentCompleted;

        public short PercentCompleted {
            get {
                short val = -1;
                try {
                    if (_hasPercentCompleted) {
                        val = device.PercentCompleted;
                    }
                } catch (ASCOM.InvalidOperationException) {
                } catch (PropertyNotImplementedException) {
                    _hasPercentCompleted = false;
                }
                return val;
            }
        }

        public double PixelSizeX {
            get {
                if (Connected) {
                    return device.PixelSizeX;
                } else {
                    return -1;
                }
            }
        }

        public double PixelSizeY {
            get {
                if (Connected) {
                    return device.PixelSizeY;
                } else {
                    return -1;
                }
            }
        }

        public short ReadoutMode {
            get {
                if (Connected) {
                    try {
                        return device.ReadoutMode;
                    } catch (PropertyNotImplementedException) {
                        ASCOMInteraction.LogComplianceIssue($"{nameof(ReadoutMode)} GET");
                    }
                    return -1;
                } else {
                    return -1;
                }
            }
            set {
                if (Connected && (value != ReadoutMode) && (value < ReadoutModes.Count)) {
                    try {
                        device.ReadoutMode = value;
                    } catch (InvalidValueException ex) {
                        Logger.Error(ex);
                        Notification.ShowError(ex.Message);
                    } catch (PropertyNotImplementedException) {
                        ASCOMInteraction.LogComplianceIssue($"{nameof(ReadoutMode)} SET");
                    }
                }
            }
        }

        public ArrayList ReadoutModesArrayList {
            get {
                ArrayList val = new ArrayList();
                if (Connected && !CanFastReadout) {
                    val = device.ReadoutModes;
                }
                return val;
            }
        }

        public string SensorName {
            get {
                string val = string.Empty;
                if (Connected) {
                    try {
                        val = device.SensorName;
                    } catch (PropertyNotImplementedException) {
                        Logger.Info("ASCOM - Driver does not implement SensorName");
                    }
                }
                return val;
            }
        }

        public SensorType SensorType {
            get {
                if (Connected) {
                    try {
                        SensorType type;
                        switch (device.SensorType) {
                            case ASCOM.DeviceInterface.SensorType.Monochrome:
                                type = SensorType.Monochrome;
                                break;

                            case ASCOM.DeviceInterface.SensorType.Color:
                                type = SensorType.Color;
                                break;

                            case ASCOM.DeviceInterface.SensorType.RGGB:
                                type = SensorType.RGGB;
                                break;

                            case ASCOM.DeviceInterface.SensorType.CMYG:
                                type = SensorType.CMYG;
                                break;

                            case ASCOM.DeviceInterface.SensorType.CMYG2:
                                type = SensorType.CMYG2;
                                break;

                            case ASCOM.DeviceInterface.SensorType.LRGB:
                                type = SensorType.LRGB;
                                break;

                            default:
                                type = SensorType.Monochrome;
                                break;
                        }

                        return type;
                    } catch (PropertyNotImplementedException) {
                        Logger.Info("ASCOM - Driver does not implement SensorType. Assuming monochrome sensor");
                    }
                }
                return SensorType.Monochrome;
            }
        }

        public double TemperatureSetPoint {
            get {
                double val = double.NaN;
                if (Connected && CanSetTemperature) {
                    val = device.SetCCDTemperature;
                }
                return val;
            }
            set {
                if (Connected && CanSetTemperature) {
                    try {
                        device.SetCCDTemperature = value;
                        RaisePropertyChanged();
                    } catch (InvalidValueException ex) {
                        Logger.Error(ex);
                        Notification.ShowError(ex.Message);
                    }
                }
            }
        }

        public int StartX {
            get {
                if (Connected) {
                    return device.StartX;
                } else {
                    return -1;
                }
            }
            set {
                if (Connected) {
                    device.StartX = value;
                    RaisePropertyChanged();
                }
            }
        }

        public int StartY {
            get {
                if (Connected) {
                    return device.StartY;
                } else {
                    return -1;
                }
            }
            set {
                if (Connected) {
                    device.StartY = value;
                    RaisePropertyChanged();
                }
            }
        }

        public ArrayList SupportedActions {
            get {
                ArrayList val = new ArrayList();
                try {
                    if (Connected) {
                        val = device?.SupportedActions;
                    }
                } catch (DriverException) {
                }
                return val;
            }
        }

        public AsyncObservableCollection<BinningMode> BinningModes { get; private set; }

        public async Task WaitUntilExposureIsReady(CancellationToken token) {
            using (token.Register(() => AbortExposure())) {
                while (!ImageReady) {
                    await CoreUtil.Wait(TimeSpan.FromMilliseconds(100), token);
                }
            }
        }

        public async Task<IExposureData> DownloadExposure(CancellationToken token) {
            using (MyStopWatch.Measure("ASCOM Download")) {
                return await Task.Run(async () => {
                    try {
                        while (!ImageReady) {
                            await CoreUtil.Wait(TimeSpan.FromMilliseconds(100), token);
                        }

                        return new Flipped2DExposureData(
                            flipped2DArray: (int[,])ImageArray,
                            bitDepth: BitDepth,
                            isBayered: SensorType != SensorType.Monochrome,
                            metaData: new ImageMetaData());
                    } catch (OperationCanceledException) {
                    } catch (Exception ex) {
                        Notification.ShowError(ex.Message);
                        Logger.Error(ex);
                    }
                    return null;
                });
            }
        }

        public void StartExposure(CaptureSequence sequence) {
            if (EnableSubSample) {
                StartX = SubSampleX / BinX;
                StartY = SubSampleY / BinY;
                NumX = SubSampleWidth / BinX;
                NumY = SubSampleHeight / BinY;
            } else {
                StartX = 0;
                StartY = 0;
                NumX = CameraXSize / BinX;
                NumY = CameraYSize / BinY;
            }

            bool isSnap = sequence.ImageType == CaptureSequence.ImageTypes.SNAPSHOT;

            if (CanFastReadout) {
                FastReadout = isSnap ? readoutModeForSnapImages != 0 : readoutModeForNormalImages != 0;
            } else {
                ReadoutMode =
                    isSnap
                        ? readoutModeForSnapImages
                        : readoutModeForNormalImages;
            }

            var isLightFrame = !(sequence.ImageType == CaptureSequence.ImageTypes.DARK ||
                              sequence.ImageType == CaptureSequence.ImageTypes.BIAS ||
                              sequence.ImageType == CaptureSequence.ImageTypes.DARKFLAT);

            device.StartExposure(sequence.ExposureTime, isLightFrame);
        }

        public void StopExposure() {
            if (CanStopExposure) {
                try {
                    device.StopExposure();
                } catch (Exception e) {
                    Logger.Error(e);
                    Notification.ShowError(e.Message);
                }
            }
        }

        public void AbortExposure() {
            if (CanAbortExposure) {
                try {
                    device.AbortExposure();
                } catch (Exception e) {
                    Logger.Error(e);
                    Notification.ShowError(e.Message);
                }
            }
        }

        public void SetBinning(short x, short y) {
            BinX = x;
            BinY = y;

            var newX = CameraXSize / x;
            var newY = CameraYSize / y;
            NumX = newX;
            NumY = newY;
        }

        private IList<int> offsets = new List<int>();
        private bool offsetValueMode = true;
        private bool canSetOffset = true;

        public bool CanSetOffset {
            get {
                return canSetOffset;
            }
            private set {
                canSetOffset = value;
                RaisePropertyChanged();
            }
        }

        public int OffsetMin {
            get {
                var offsetMin = 0;
                if (Connected && CanSetOffset) {
                    if (offsetValueMode) {
                        offsetMin = device.OffsetMin;
                    } else {
                        if (offsets.Count > 0) {
                            offsetMin = offsets.Aggregate((l, r) => l < r ? l : r);
                        }
                    }
                }
                return offsetMin;
            }
        }

        public int OffsetMax {
            get {
                var offsetMax = 0;
                if (Connected && CanSetOffset) {
                    if (offsetValueMode) {
                        offsetMax = device.OffsetMax;
                    } else {
                        if (offsets.Count > 0) {
                            offsetMax = offsets.Aggregate((l, r) => l > r ? l : r);
                        }
                    }
                }
                return offsetMax;
            }
        }

        public bool CanSetUSBLimit {
            get {
                return false;
            }
        }

        public int Offset {
            get {
                var offset = -1;
                if (Connected && CanSetOffset) {
                    if (offsetValueMode) {
                        offset = device.Offset;
                    } else {
                        offset = offsets[device.Offset];
                    }
                }
                return offset;
            }
            set {
                try {
                    if (Connected && CanSetOffset) {
                        if (offsetValueMode) {
                            if (value < OffsetMin) {
                                value = OffsetMin;
                            }
                            if (value > OffsetMax) {
                                value = OffsetMax;
                            }

                            device.Offset = value;
                        } else {
                            var idx = offsets.IndexOf(value);
                            if (idx >= 0) {
                                device.Offset = idx;
                            }
                        }
                        RaisePropertyChanged();
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
                }
            }
        }

        public int USBLimit {
            get {
                return -1;
            }
            set {
            }
        }

        public int USBLimitMax => -1;
        public int USBLimitMin => -1;
        public int USBLimitStep => -1;

        public void StartLiveView() {
            throw new System.NotImplementedException();
        }

        public Task<IExposureData> DownloadLiveView(CancellationToken token) {
            throw new System.NotImplementedException();
        }

        public void StopLiveView() {
            throw new System.NotImplementedException();
        }

        protected override Task PostConnect() {
            Initialize();
            return Task.CompletedTask;
        }

        protected override Camera GetInstance(string id) {
            return new Camera(id);
        }

        public bool LiveViewEnabled { get => false; set => throw new System.NotImplementedException(); }

        public int BatteryLevel => -1;

        public bool HasBattery => false;

        public int BitDepth {
            get {
                return (int)profileService.ActiveProfile.CameraSettings.BitDepth;
            }
        }

        protected override string ConnectionLostMessage => Loc.Instance["LblCameraConnectionLost"];
    }
}