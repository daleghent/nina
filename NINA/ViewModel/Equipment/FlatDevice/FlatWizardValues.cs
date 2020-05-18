#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Profile;
using System.Collections.ObjectModel;

namespace NINA.ViewModel.Equipment.FlatDevice {

    public class WizardValueBlock {
        public string Binning { get; set; }

        public ObservableCollection<WizardGridColumn> Columns { get; set; } = new ObservableCollection<WizardGridColumn>();
    }

    public class WizardGridColumn {
        public int ColumnNumber { get; set; }
        public string Header { get; set; }
        public ObservableCollection<FilterTiming> Settings { get; set; } = new ObservableCollection<FilterTiming>();
    }

    public class FilterTiming {
        private double _brightness;
        private double _time;
        private readonly IProfileService _profileService;

        public bool ShowFilterNameOnly { get; }

        public bool IsEmpty { get; private set; }

        public FlatDeviceFilterSettingsKey Key { get; }

        public FilterTiming(double brightness, double time, IProfileService profileService, FlatDeviceFilterSettingsKey key, bool showFilterNameOnly, bool isEmpty) {
            _brightness = brightness;
            _time = time;
            _profileService = profileService;
            Key = key;
            ShowFilterNameOnly = showFilterNameOnly;
            IsEmpty = isEmpty;
        }

        public double Brightness {
            get => _brightness;
            set {
                _brightness = value;
                if (Time <= 0d) return;
                IsEmpty = false;
                var temp = new FlatDeviceFilterSettingsValue(value, Time);
                _profileService.ActiveProfile.FlatDeviceSettings.AddBrightnessInfo(Key, temp);
            }
        }

        public double Time {
            get => _time;
            set {
                _time = value;
                if (Brightness <= 0d) return;
                IsEmpty = false;
                var temp = new FlatDeviceFilterSettingsValue(Brightness, value);
                _profileService.ActiveProfile.FlatDeviceSettings.AddBrightnessInfo(Key, temp);
            }
        }
    }
}