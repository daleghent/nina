#region "copyright"

/*
    Copyright © 2016 - 2022 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Plugin {

    public static class Constants {
        public static readonly string CoreExtensionsFolder = Path.Combine(CoreUtil.APPLICATIONDIRECTORY, "Plugins");
        public static readonly string UserExtensionsFolder = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "Plugins");
        public static readonly string StagingFolder = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "PluginStaging");
        public static readonly string DeletionFolder = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "PluginDeletion");
    }
}