#region "copyright"

/*
    Copyright � 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using OxyPlot.Series;
using System.Collections.Generic;

namespace NINA.WPF.Base.ViewModel.AutoFocus {

    public class FocusPointComparer : IComparer<ScatterErrorPoint> {

        public int Compare(ScatterErrorPoint x, ScatterErrorPoint y) {
            if (x.X < y.X) {
                return -1;
            } else if (x.X > y.X) {
                return 1;
            } else {
                return 0;
            }
        }
    }
}