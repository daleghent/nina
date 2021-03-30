#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace NINA.Model.MySafetyMonitor {

    public class SafetyMonitorInfo : DeviceInfo {
        private bool isSafe;

        public bool IsSafe {
            get { return isSafe; }
            set { isSafe = value; RaisePropertyChanged(); }
        }
    }
}