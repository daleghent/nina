#region "copyright"

/*
    Copyright © 2016 - 2022 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Equipment.MyTelescope;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using NINA.Equipment.Utility;
using NINA.Core.Locale;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Equipment;

namespace NINA.WPF.Base.ViewModel.Equipment.Telescope {

    public class TelescopeChooserVM : DeviceChooserVM {
        private readonly IDeviceDispatcher deviceDispatcher;

        public TelescopeChooserVM(IProfileService profileService, IDeviceDispatcher deviceDispatcher) : base(profileService) {
            this.deviceDispatcher = deviceDispatcher;
        }

        public override void GetEquipment() {
            lock (lockObj) {
                var ascomInteraction = new ASCOMInteraction(deviceDispatcher, profileService);
                var devices = new List<IDevice>();

                devices.Add(new DummyDevice(Loc.Instance["LblNoTelescope"]));

                try {
                    foreach (ITelescope telescope in ascomInteraction.GetTelescopes()) {
                        devices.Add(telescope);
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                DetermineSelectedDevice(devices, profileService.ActiveProfile.TelescopeSettings.Id);
            }
        }
    }
}