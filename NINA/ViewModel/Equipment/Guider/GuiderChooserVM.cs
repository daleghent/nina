#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

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
using NINA.Model.MyGuider.PHD2;
using NINA.Utility;
using System;
using System.IO;
using System.Collections.Generic;

namespace NINA.ViewModel.Equipment.Guider {

    internal class GuiderChooserVM : DeviceChooserVM {
        private readonly ICameraMediator cameraMediator;
        private readonly ITelescopeMediator telescopeMediator;
        private readonly IWindowServiceFactory windowServiceFactory;

        public GuiderChooserVM(IProfileService profileService, ICameraMediator cameraMediator, ITelescopeMediator telescopeMediator, IWindowServiceFactory windowServiceFactory) : base(profileService) {
            this.cameraMediator = cameraMediator;
            this.profileService = profileService;
            this.telescopeMediator = telescopeMediator;
            this.windowServiceFactory = windowServiceFactory;
        }

        public override void GetEquipment() {
            lock (lockObj) {
                var devices = new List<Model.IDevice>();
                devices.Add(new DummyGuider(profileService));
                devices.Add(new PHD2Guider(profileService, windowServiceFactory));
                //devices.Add(new SynchronizedPHD2Guider(profileService, cameraMediator, windowServiceFactory)); Non-Functional with current sequencer
                devices.Add(new DirectGuider(profileService, telescopeMediator));
                devices.Add(new MetaGuideGuider(profileService, windowServiceFactory));

                try {
                    var mgen2 = new MGEN2.MGEN(Path.Combine("FTDI", "ftd2xx.dll"), new MGenLogger());
                    devices.Add(new MGENGuider(mgen2, "Lacerta MGEN Superguider", "Lacerta_MGEN_Superguider", profileService));
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                try {
                    var mgen3 = new MGEN3.MGEN3(Path.Combine("FTDI", "ftd2xx.dll"), Path.Combine("MGEN", "MG3lib.dll"), new MGenLogger());
                    devices.Add(new MGENGuider(mgen3, "Lacerta MGEN-3 Autoguider", "Lacerta_MGEN-3_Autoguider", profileService));
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                Devices = devices;
                DetermineSelectedDevice(profileService.ActiveProfile.GuiderSettings.GuiderName);
            }
        }
    }
}