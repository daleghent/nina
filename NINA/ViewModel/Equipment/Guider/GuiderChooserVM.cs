#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model.MyGuider;
using NINA.Utility.Mediator.Interfaces;
using NINA.Profile;
using NINA.Utility.WindowService;

namespace NINA.ViewModel.Equipment.Guider {

    internal class GuiderChooserVM : EquipmentChooserVM, IDeviceChooserVM {
        private readonly ICameraMediator cameraMediator;
        private readonly ITelescopeMediator telescopeMediator;
        private readonly IWindowServiceFactory windowServiceFactory;

        public GuiderChooserVM(IProfileService profileService, ICameraMediator cameraMediator, ITelescopeMediator telescopeMediator, IWindowServiceFactory windowServiceFactory) : base(profileService) {
            this.cameraMediator = cameraMediator;
            this.profileService = profileService;
            this.telescopeMediator = telescopeMediator;
            this.windowServiceFactory = windowServiceFactory;
            GetEquipment();
        }

        public override void GetEquipment() {
            Devices.Clear();
            Devices.Add(new DummyGuider(profileService));
            Devices.Add(new PHD2Guider(profileService, windowServiceFactory));
            Devices.Add(new SynchronizedPHD2Guider(profileService, cameraMediator, windowServiceFactory));
            Devices.Add(new DirectGuider(profileService, telescopeMediator));
            Devices.Add(new MGENGuider(profileService));
            Devices.Add(new MetaGuideGuider(profileService, windowServiceFactory));

            DetermineSelectedDevice(profileService.ActiveProfile.GuiderSettings.GuiderName);
        }
    }
}