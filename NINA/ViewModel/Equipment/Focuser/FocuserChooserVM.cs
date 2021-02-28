#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Profile;

namespace NINA.ViewModel.Equipment.Focuser {

    internal class FocuserChooserVM : EquipmentChooserVM, IDeviceChooserVM {
        private IDeviceFactory focuserFactory;

        public FocuserChooserVM(IProfileService profileService, IDeviceFactory focuserFactory) : base(profileService) {
            this.focuserFactory = focuserFactory;
        }

        public override void GetEquipment() {
            Devices.Clear();

            foreach (var device in focuserFactory.GetDevices()) {
                Devices.Add(device);
            }

            DetermineSelectedDevice(profileService.ActiveProfile.FocuserSettings.Id);
        }
    }
}