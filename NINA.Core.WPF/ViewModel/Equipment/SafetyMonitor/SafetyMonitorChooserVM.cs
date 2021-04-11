#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Equipment.MySafetyMonitor;
using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NINA.Core.Locale;
using NINA.Equipment.Utility;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Equipment;
using NINA.WPF.Base.Model.Equipment.MySafetyMonitor.Simulator;

namespace NINA.WPF.Base.ViewModel.Equipment.SafetyMonitor {

    public class SafetyMonitorChooserVM : DeviceChooserVM {

        public SafetyMonitorChooserVM(IProfileService profileService) : base(profileService) {
        }

        public override void GetEquipment() {
            lock (lockObj) {
                var devices = new List<IDevice>();

                devices.Add(new DummyDevice(Loc.Instance["LblNoSafetyMonitor"]));

                try {
                    foreach (ISafetyMonitor safetyMonitor in ASCOMInteraction.GetSafetyMonitors(profileService)) {
                        devices.Add(safetyMonitor);
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                devices.Add(new SafetyMonitorSimulator());
                Devices = devices;
                DetermineSelectedDevice(profileService.ActiveProfile.SafetyMonitorSettings.Id);
            }
        }
    }
}