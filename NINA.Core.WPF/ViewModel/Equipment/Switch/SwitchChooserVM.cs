#region "copyright"

/*
    Copyright © 2016 - 2022 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Equipment.MySwitch;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NINA.Core.Locale;
using NINA.Equipment.Utility;
using NINA.Equipment.Equipment.MySwitch.Eagle;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Equipment.MySwitch.PegasusAstro;
using NINA.Equipment.Equipment;

namespace NINA.WPF.Base.ViewModel.Equipment.Switch {

    public class SwitchChooserVM : DeviceChooserVM {
        private readonly IDeviceDispatcher deviceDispatcher;

        public SwitchChooserVM(IProfileService profileService, IDeviceDispatcher deviceDispatcher) : base(profileService) {
            this.deviceDispatcher = deviceDispatcher;
        }

        public override void GetEquipment() {
            lock (lockObj) {
                {
                    var ascomInteraction = new ASCOMInteraction(deviceDispatcher, profileService);
                    var devices = new List<IDevice>();

                    devices.Add(new DummyDevice(Loc.Instance["LblNoSwitch"]));

                    /* ASCOM */
                    try {
                        foreach (ISwitchHub ascomSwitch in ascomInteraction.GetSwitches()) {
                            devices.Add(ascomSwitch);
                        }
                    } catch (Exception ex) {
                        Logger.Error(ex);
                    }

                    /* PrimaLuceLab EAGLE */
                    devices.Add(new Eagle(profileService));

                    /* Pegasus Astro Ultimate Powerbox V2 */
                    devices.Add(new UltimatePowerBoxV2(profileService));

                    DetermineSelectedDevice(devices, profileService.ActiveProfile.SwitchSettings.Id);
                }
            }
        }
    }
}