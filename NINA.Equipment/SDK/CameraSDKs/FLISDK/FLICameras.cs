#region "copyright"

/*
    Copyright � 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using System.Collections.Generic;
using System.Linq;

namespace FLI {

    public static class FLICameras {

        public static List<string> GetCameras() {
            List<string> cams = new List<string>();
            uint domain = (uint)(LibFLI.FLIDomains.DEV_CAMERA | LibFLI.FLIDomains.IF_USB);

            cams = LibFLI.N_FLIList(domain);
            Logger.Debug(string.Format("FLI: Found {0} camera(s)", cams.Count));

            return cams;
        }
    }
}