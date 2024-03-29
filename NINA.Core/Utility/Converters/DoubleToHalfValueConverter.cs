#region "copyright"

/*
    Copyright � 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Globalization;
using System.Windows.Data;

namespace NINA.Core.Utility.Converters {

    public class DoubleToHalfValueConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            double outVal;
            double.TryParse(value.ToString(), out outVal);
            return outVal / 2;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            double outVal;
            double.TryParse(value.ToString(), out outVal);
            return outVal * 2;
        }
    }
}