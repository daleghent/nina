#region "copyright"

/*
    Copyright © 2016 - 2022 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Equipment.MyRotator;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using NINA.Core.Locale;
using NINA.Equipment.Utility;
using NINA.Equipment.Equipment;
using NINA.Equipment.Interfaces;

namespace NINA.WPF.Base.ViewModel.Equipment.Rotator {

    public class RotatorChooserVM : DeviceChooserVM {
        private readonly IDeviceDispatcher deviceDispatcher;

        public RotatorChooserVM(IProfileService profileService, IDeviceDispatcher deviceDispatcher) : base(profileService) {
            this.deviceDispatcher = deviceDispatcher;
        }

        public override void GetEquipment() {
            lock (lockObj) {
                var ascomInteraction = new ASCOMInteraction(deviceDispatcher, profileService);
                var devices = new List<IDevice>();

                devices.Add(new DummyDevice(Loc.Instance["LblNoRotator"]));

                try {
                    foreach (IRotator rotator in ascomInteraction.GetRotators()) {
                        devices.Add(rotator);
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                devices.Add(new ManualRotator(profileService));

                DetermineSelectedDevice(devices, profileService.ActiveProfile.RotatorSettings.Id);
            }
        }
    }
}