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
using NINA.Model.MySwitch;
using NINA.Utility;
using NINA.Profile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.ViewModel.Equipment.Switch {

    internal class SwitchChooserVM : DeviceChooserVM {

        public SwitchChooserVM(IProfileService profileService) : base(profileService) {
        }

        public override void GetEquipment() {
            lock (lockObj) {
                {
                    var devices = new List<Model.IDevice>();

                    devices.Add(new DummyDevice(Locale.Loc.Instance["LblNoSwitch"]));

                    /* ASCOM */
                    try {
                        foreach (ISwitchHub ascomSwitch in ASCOMInteraction.GetSwitches(profileService)) {
                            devices.Add(ascomSwitch);
                        }
                    } catch (Exception ex) {
                        Logger.Error(ex);
                    }

                    /* PrimaLuceLab EAGLE */
                    devices.Add(new Eagle(profileService));

                    /* Pegasus Astro Ultimate Powerbox V2 */
                    devices.Add(new UltimatePowerBoxV2(profileService));

                    Devices = devices;
                    DetermineSelectedDevice(profileService.ActiveProfile.SwitchSettings.Id);
                }
            }
        }
    }
}