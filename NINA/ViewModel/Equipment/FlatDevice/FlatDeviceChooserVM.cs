#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Model.MyFlatDevice;
using NINA.Profile;

namespace NINA.ViewModel.Equipment.FlatDevice {

    internal class FlatDeviceChooserVM : EquipmentChooserVM, IDeviceChooserVM {

        public FlatDeviceChooserVM(IProfileService profileService) : base(profileService) {
            GetEquipment();
        }

        public override void GetEquipment() {
            Devices.Clear();

            Devices.Add(new DummyDevice(Locale.Loc.Instance["LblFlatDeviceNoDevice"]));
            Devices.Add(new AllProSpikeAFlat(profileService));
            Devices.Add(new AlnitakFlipFlatSimulator(profileService));
            Devices.Add(new AlnitakFlatDevice(profileService));
            Devices.Add(new ArteskyFlatBox(profileService));
            Devices.Add(new PegasusAstroFlatMaster(profileService));
            DetermineSelectedDevice(profileService.ActiveProfile.FlatDeviceSettings.Id);
        }
    }
}