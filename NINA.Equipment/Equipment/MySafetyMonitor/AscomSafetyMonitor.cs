﻿#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using ASCOM.DriverAccess;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MySafetyMonitor {

    internal class AscomSafetyMonitor : AscomDevice<SafetyMonitor>, ISafetyMonitor {

        public AscomSafetyMonitor(string id, string name) : base(id, name) {
        }

        public bool IsSafe {
            get {
                if (Connected) {
                    var isSafe = device.IsSafe;
                    Logger.Trace($"AscomSafetyMonitor - IsSafe: {isSafe}");
                    return isSafe;
                } else {
                    return false;
                }
            }
        }

        protected override string ConnectionLostMessage => Loc.Instance["LblSafetyMonitorConnectionLost"];

        protected override SafetyMonitor GetInstance(string id) {
            return new SafetyMonitor(id);
        }
    }
}