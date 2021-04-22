#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Model.Equipment;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace NINA.Profile {

    [Serializable]
    [DataContract]
    public class FlatDeviceSettings : Settings, IFlatDeviceSettings {

        public FlatDeviceSettings() {
            FilterSettings = new Dictionary<FlatDeviceFilterSettingsKey, FlatDeviceFilterSettingsValue>();
        }

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context) {
            if (FilterSettings == null) {
                FilterSettings =
                    new Dictionary<FlatDeviceFilterSettingsKey, FlatDeviceFilterSettingsValue>();
            }
        }

        protected override void SetDefaultValues() {
            Id = "No_Device";
        }

        private string id;

        [DataMember]
        public string Id {
            get => id;
            set {
                if (id == value) return;
                id = value;
                RaisePropertyChanged();
            }
        }

        private string name;

        [DataMember]
        public string Name {
            get => name;
            set {
                if (name == value) return;
                name = value;
                RaisePropertyChanged();
            }
        }

        private string portName;

        [DataMember]
        public string PortName {
            get => portName;
            set {
                if (portName == value) return;
                portName = value;
                RaisePropertyChanged();
            }
        }

        private bool openAtSequenceStart;

        [DataMember]
        public bool OpenAtSequenceStart {
            get => openAtSequenceStart;
            set {
                if (openAtSequenceStart == value) return;
                openAtSequenceStart = value;
                RaisePropertyChanged();
            }
        }

        private bool closeAtSequenceEnd;

        [DataMember]
        public bool CloseAtSequenceEnd {
            get => closeAtSequenceEnd;
            set {
                if (closeAtSequenceEnd == value) return;
                closeAtSequenceEnd = value;
                RaisePropertyChanged();
            }
        }

        private bool openForDarkFlats;

        [DataMember]
        public bool OpenForDarkFlats {
            get => openForDarkFlats;
            set {
                if (openForDarkFlats == value) return;
                openForDarkFlats = value;
                RaisePropertyChanged();
            }
        }

        private bool useWizardTrainedValues;

        [DataMember]
        public bool UseWizardTrainedValues {
            get => useWizardTrainedValues;
            set {
                if (useWizardTrainedValues == value) return;
                useWizardTrainedValues = value;
                RaisePropertyChanged();
            }
        }

        private int settleTime;

        [DataMember]
        public int SettleTime {
            get => settleTime;
            set {
                if (settleTime == value) return;
                settleTime = value;
                RaisePropertyChanged();
            }
        }

        private Dictionary<FlatDeviceFilterSettingsKey, FlatDeviceFilterSettingsValue> filterSettings;

        [DataMember]
        public Dictionary<FlatDeviceFilterSettingsKey, FlatDeviceFilterSettingsValue> FilterSettings {
            get => filterSettings;
            set {
                filterSettings = value;
                RaisePropertyChanged();
            }
        }

        public void AddBrightnessInfo(FlatDeviceFilterSettingsKey key, FlatDeviceFilterSettingsValue value) {
            if (FilterSettings.ContainsKey(key)) {
                FilterSettings[key] = value;
            } else {
                FilterSettings.Add(key, value);
            }

            RaisePropertyChanged(nameof(FilterSettings));
        }

        public FlatDeviceFilterSettingsValue GetBrightnessInfo(FlatDeviceFilterSettingsKey key) {
            return FilterSettings.ContainsKey(key) ? FilterSettings[key] : null;
        }

        public IEnumerable<BinningMode> GetBrightnessInfoBinnings() {
            var result = FilterSettings.Keys.Select(key => key.Binning).ToList();

            return result.Distinct();
        }

        public IEnumerable<int> GetBrightnessInfoGains() {
            var result = FilterSettings.Keys.Select(key => key.Gain).ToList();

            return result.Distinct();
        }

        public void RemoveGain(int gain) {
            var keysToRemove = FilterSettings.Keys
                                                .Where(key => key.Gain == gain).ToList();

            foreach (var key in keysToRemove) {
                FilterSettings.Remove(key);
            }
            RaisePropertyChanged(nameof(FilterSettings));
        }

        public void RemoveBinning(BinningMode binning) {
            var keysToRemove = FilterSettings.Keys
                                                .Where(key => Equals(key.Binning, binning)).ToList();

            foreach (var key in keysToRemove) {
                FilterSettings.Remove(key);
            }
            RaisePropertyChanged(nameof(FilterSettings));
        }
    }

    [Serializable]
    [DataContract]
    public class FlatDeviceFilterSettingsKey {

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public string FilterName { get; set; }

        [DataMember]
        public short? Position { get; set; }

        [DataMember]
        public BinningMode Binning { get; set; }

        [DataMember]
        public int Gain { get; set; }

        public FlatDeviceFilterSettingsKey(short? position, BinningMode binning, int gain) {
            Position = position;
            Binning = binning;
            Gain = gain;
        }

        public override bool Equals(object obj) {
            if (obj == null || this.GetType() != obj.GetType()) {
                return false;
            }

            var other = (FlatDeviceFilterSettingsKey)obj;
            switch (Binning) {
                case null when other.Binning == null:
                    return Position == other.Position && Gain == other.Gain;

                case null:
                    return false;

                default:
                    return Position == other.Position && Binning.Equals(other.Binning) && Gain == other.Gain;
            }
        }

        public override int GetHashCode() {
            //see https://en.wikipedia.org/wiki/Hash_function
            const int primeNumber = 397;
            unchecked {
                var hashCode = Position != null ? Position.GetHashCode() : 0;
                hashCode = (hashCode * primeNumber) ^ (Binning != null ? Binning.GetHashCode() : 0);
                hashCode = (hashCode * primeNumber) ^ Gain.GetHashCode();
                return hashCode;
            }
        }
    }

    [Serializable]
    [DataContract]
    public class FlatDeviceFilterSettingsValue {

        [Obsolete]
        [DataMember]
        public double Brightness { get; set; } = double.NaN;

        [DataMember]
        public int AbsoluteBrightness { get; set; }

        [DataMember]
        public double Time { get; set; }

        public FlatDeviceFilterSettingsValue(int brightness, double time) {
            AbsoluteBrightness = brightness;
            Time = time;
        }
    }
}