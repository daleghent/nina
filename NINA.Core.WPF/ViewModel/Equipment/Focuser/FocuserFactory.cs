#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using NINA.Model;
using NINA.Model.MyFocuser;
using NINA.Profile;
using NINA.Utility;
using System.Collections.Generic;

namespace NINA.ViewModel.Equipment.Focuser {

    public class FocuserFactory : IDeviceFactory {
        private readonly IProfileService profileService;

        public FocuserFactory(IProfileService profileService) {
            this.profileService = profileService;
        }

        public IList<IDevice> GetDevices() {
            var result = new List<IDevice> {
                new DummyDevice(Locale.Loc.Instance["LblNoFocuser"]),
                new UltimatePowerboxV2(profileService)
            };
            try {
                result.AddRange(ASCOMInteraction.GetFocusers(profileService));
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            return result;
        }
    }
}