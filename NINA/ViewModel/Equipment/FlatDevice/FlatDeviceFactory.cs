#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Collections.Generic;
using NINA.Model;
using NINA.Model.MyFlatDevice;
using NINA.Profile;

namespace NINA.ViewModel.Equipment.FlatDevice {

    internal class FlatDeviceFactory : IDeviceFactory {
        private readonly IProfileService profileService;

        public FlatDeviceFactory(IProfileService profileService) {
            this.profileService = profileService;
        }

        public IList<IDevice> GetDevices() {
            return new List<IDevice>
            {
                new DummyDevice(Locale.Loc.Instance["LblFlatDeviceNoDevice"]),
                new AllProSpikeAFlat(profileService),
                new AlnitakFlipFlatSimulator(profileService),
                new AlnitakFlatDevice(profileService),
                new ArteskyFlatBox(profileService),
                new PegasusAstroFlatMaster(profileService)
            };
        }
    }
}