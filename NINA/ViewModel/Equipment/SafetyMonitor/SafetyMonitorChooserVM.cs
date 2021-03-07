#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Model.MySafetyMonitor;
using NINA.Profile;
using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.ViewModel.Equipment.SafetyMonitor {

    internal class SafetyMonitorChooserVM : DeviceChooserVM {

        public SafetyMonitorChooserVM(IProfileService profileService) : base(profileService) {
        }

        public override void GetEquipment() {
            lock (lockObj) {
                var devices = new List<Model.IDevice>();

                devices.Add(new DummyDevice(Locale.Loc.Instance["LblNoSafetyMonitor"]));

                try {
                    foreach (ISafetyMonitor safetyMonitor in ASCOMInteraction.GetSafetyMonitors(profileService)) {
                        devices.Add(safetyMonitor);
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                Devices.Add(new NINA.Model.MySafetyMonitor.SafetyMonitorSimulator());
                Devices = devices;
                DetermineSelectedDevice(profileService.ActiveProfile.SafetyMonitorSettings.Id);
            }
        }
    }
}