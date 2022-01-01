#region "copyright"

/*
    Copyright © 2016 - 2022 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using ASCOM.DriverAccess;
using NINA.Core.Locale;
using NINA.Equipment.ASCOMFacades;
using NINA.Equipment.Interfaces;

namespace NINA.Equipment.Equipment.MySafetyMonitor {

    internal class AscomSafetyMonitor : AscomDevice<SafetyMonitor, ISafetyMonitorFacade, SafetyMonitorFacadeProxy>, ISafetyMonitor {

        public AscomSafetyMonitor(string id, string name, IDeviceDispatcher deviceDispatcher) : base(id, name, deviceDispatcher, DeviceDispatcherType.SafetyMonitor) {
        }

        public bool IsSafe {
            get {
                return GetProperty(nameof(SafetyMonitor.IsSafe), false);
            }
        }

        protected override string ConnectionLostMessage => Loc.Instance["LblSafetyMonitorConnectionLost"];

        protected override SafetyMonitor GetInstance(string id) {
            return DeviceDispatcher.Invoke(DeviceDispatcherType, () => new SafetyMonitor(id));
        }
    }
}