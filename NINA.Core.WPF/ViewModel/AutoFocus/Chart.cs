#region "copyright"

/*
    Copyright © 2016 - 2022 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.WPF.Base.ViewModel.AutoFocus {

    public class Chart {

        public Chart(string name, string filePath) {
            this.Name = name;
            this.FilePath = filePath;
        }

        public string FilePath { get; set; }
        public string Name { get; set; }
    }

    public class ChartComparer : IComparer<Chart> {

        public int Compare(Chart x, Chart y) {
            return string.Compare(x.FilePath, y.FilePath) * -1;
        }
    }
}

namespace NINA.WPF.Base.ViewModel.Imaging {

    public class AutoFocusToolVM {

        [Obsolete("Class has been moved to different namespace. Replace once all plugins are adapted")]
        public class Chart {

            public Chart(string name, string filePath) {
                this.Name = name;
                this.FilePath = filePath;
            }

            public string FilePath { get; set; }
            public string Name { get; set; }
        }

        [Obsolete("Class has been moved to different namespace. Replace once all plugins are adapted")]
        public class ChartComparer : IComparer<Chart> {

            public int Compare(Chart x, Chart y) {
                return string.Compare(x.FilePath, y.FilePath) * -1;
            }
        }
    }
}