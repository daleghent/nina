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
using NINA.Utility;
using NINA.Profile;
using System;
using NINA.Model.MyDome;

namespace NINA.ViewModel.Equipment.Dome {

    internal class DomeChooserVM : EquipmentChooserVM {

        public DomeChooserVM(IProfileService profileService) : base(profileService) {
        }

        public override void GetEquipment() {
            Devices.Clear();

            Devices.Add(new DummyDevice(Locale.Loc.Instance["LblDomeNoSource"]));

            try {
                foreach (IDome dome in ASCOMInteraction.GetDomes(profileService)) {
                    Devices.Add(dome);
                }
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            DetermineSelectedDevice(profileService.ActiveProfile.DomeSettings.Id);
        }
    }
}