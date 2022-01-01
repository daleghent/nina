#region "copyright"

/*
    Copyright © 2016 - 2022 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using System.Collections.Generic;
using NINA.Core.Locale;
using NINA.Equipment.Utility;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Equipment;

namespace NINA.WPF.Base.ViewModel.Equipment.Focuser {

    public class FocuserFactory : IDeviceFactory {
        private readonly IProfileService profileService;
        private readonly IDeviceDispatcher deviceDispatcher;

        public FocuserFactory(IProfileService profileService, IDeviceDispatcher deviceDispatcher) {
            this.profileService = profileService;
            this.deviceDispatcher = deviceDispatcher;
        }

        public IList<IDevice> GetDevices() {
            var ascomInteraction = new ASCOMInteraction(deviceDispatcher, profileService);
            var result = new List<IDevice> {
                new DummyDevice(Loc.Instance["LblNoFocuser"]),
                new UltimatePowerboxV2(profileService)
            };
            try {
                result.AddRange(ascomInteraction.GetFocusers());
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            return result;
        }
    }
}