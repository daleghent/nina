#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model.MyTelescope;
using System;
using System.Globalization;
using System.Windows.Data;

namespace NINA.Utility.Converters {

    public class TrackingModeConverter : IValueConverter {

        public static string TrackingModeToLocalizedString(TrackingMode trackingMode) {
            switch (trackingMode) {
                case TrackingMode.Stopped:
                    return Locale.Loc.Instance["LblTrackingStopped"];
                case TrackingMode.Sidereal:
                    return Locale.Loc.Instance["LblTrackingSidereal"];
                case TrackingMode.Lunar:
                    return Locale.Loc.Instance["LblTrackingLunar"];
                case TrackingMode.Solar:
                    return Locale.Loc.Instance["LblTrackingSolar"];
                case TrackingMode.King:
                    return Locale.Loc.Instance["LblTrackingKing"];
                case TrackingMode.Custom:
                    return Locale.Loc.Instance["LblTrackingCustom"];
            }
            throw new NotSupportedException($"{trackingMode} cannot be converted to a localized string");
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null || !(value is TrackingMode)) {
                return null;
            }
            return TrackingModeToLocalizedString((TrackingMode)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (targetType == typeof(TrackingMode)) {
                return value;
            }
            throw new NotImplementedException($"Cannot convert TrackingMode to {targetType}");
        }
    }
}