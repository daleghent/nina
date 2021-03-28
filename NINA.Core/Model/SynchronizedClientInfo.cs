#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;

#pragma warning disable 1998

namespace NINA.Model.MyGuider.PHD2 {
    /// <summary>
    /// This class holds information in the service about the connected client
    /// </summary>
    public class SynchronizedClientInfo {
        public DateTime ExposureEndTime { get; set; }
        public Guid InstanceID { get; set; }
        public bool IsAlive => DateTime.Now.Subtract(LastPing).TotalSeconds < 5;
        public bool IsExposing { get; set; }
        public bool IsWaitingForDither { get; set; }
        public double LastDownloadTime { get; set; }
        public DateTime LastPing { get; set; }
        public double NextExposureTime { get; set; }
    }
}