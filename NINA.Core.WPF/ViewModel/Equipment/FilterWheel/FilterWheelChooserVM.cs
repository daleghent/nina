#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FLI;
using NINA.Model;
using NINA.Model.MyFilterWheel;
using NINA.Utility;
using NINA.Profile;
using QHYCCD;
using System;
using System.Collections.Generic;
using NINA.Utility.AtikSDK;
using ZWOptical.EFWSDK;

namespace NINA.ViewModel.Equipment.FilterWheel {

    public class FilterWheelChooserVM : DeviceChooserVM {

        public FilterWheelChooserVM(IProfileService profileService) : base(profileService) {
        }

        public override void GetEquipment() {
            lock (lockObj) {
                var devices = new List<Model.IDevice>();

                devices.Add(new DummyDevice(Locale.Loc.Instance["LblNoFilterwheel"]));

                /*
                 * FLI
                 */
                try {
                    Logger.Trace("Adding FLI filter wheels");
                    List<string> fwheels = FLIFilterWheels.GetFilterWheels();

                    if (fwheels.Count > 0) {
                        foreach (var entry in fwheels) {
                            var fwheel = new FLIFilterWheel(entry, profileService);

                            if (!string.IsNullOrEmpty(fwheel.Name)) {
                                Logger.Debug($"Adding FLI Filter Wheel {fwheel.Id} (as {fwheel.Name})");
                                devices.Add(fwheel);
                            }
                        }
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                // Atik EFW
                try {
                    Logger.Trace("Adding Atik EFW filter wheels");
                    for (int i = 0; i < 10; i++) {
                        if (AtikCameraDll.ArtemisEfwIsPresent(i)) {
                            var wheel = new AtikFilterWheel(i, profileService);
                            Logger.Debug($"Adding Atik Filter Wheel {i} as {wheel.Name}");
                            devices.Add(wheel);
                        }
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                // Atik internal Wheels
                try {
                    Logger.Trace("Adding Atik internal filter wheels");
                    var atikDevices = AtikCameraDll.GetDevicesCount();
                    Logger.Trace($"Cameras found: {atikDevices}");
                    for (int i = 0; i < atikDevices; i++) {
                        var wheel = new AtikInternalFilterWheel(i, profileService);
                        if (wheel.CameraHasInternalFilterWheel) {
                            Logger.Debug($"Adding Atik internal Filter Wheel {i} as {wheel.Name}");
                            devices.Add(wheel);
                        }
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                /*
                 * QHY - Integrated or 4-pin connected filter wheels only
                 */
                try {
                    var qhy = new QHYFilterWheels();
                    Logger.Trace("Adding QHY integrated/4-pin filter wheels");
                    List<string> fwheels = qhy.GetFilterWheels();

                    if (fwheels.Count > 0) {
                        foreach (var entry in fwheels) {
                            var fwheel = new QHYFilterWheel(entry, profileService);

                            if (!string.IsNullOrEmpty(fwheel.Name)) {
                                Logger.Debug($"Adding QHY Filter Wheel {fwheel.Id} (as {fwheel.Name})");
                                devices.Add(fwheel);
                            }
                        }
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                /* ZWO filter wheels */
                try {
                    Logger.Trace("Adding ZWOptical filter wheels");

                    var wheels = EFWdll.GetNum();

                    for (int i = 0; i < wheels; i++) {
                        var fw = new ASIFilterWheel(i, profileService);
                        Logger.Debug($"Adding ZWOptical Filter Wheel {i})");
                        devices.Add(fw);
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                /*
                 * ASCOM devices
                 */
                try {
                    foreach (IFilterWheel fw in ASCOMInteraction.GetFilterWheels(profileService)) {
                        devices.Add(fw);
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                devices.Add(new ManualFilterWheel(this.profileService));

                Devices = devices;
                DetermineSelectedDevice(profileService.ActiveProfile.FilterWheelSettings.Id);
            }
        }
    }
}