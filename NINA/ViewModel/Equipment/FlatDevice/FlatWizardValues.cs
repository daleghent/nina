#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
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