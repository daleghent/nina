#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Profile;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel.Interfaces;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace NINA.ViewModel {

    internal class ApplicationDeviceConnectionVM : BaseVM, IApplicationDeviceConnectionVM {
        private readonly ICameraMediator cameraMediator;
        private readonly ITelescopeMediator telescopeMediator;
        private readonly IDomeMediator domeMediator;
        private readonly IFilterWheelMediator filterWheelMediator;
        private readonly IFocuserMediator focuserMediator;
        private readonly IRotatorMediator rotatorMediator;
        private readonly IFlatDeviceMediator flatDeviceMediator;
        private readonly IGuiderMediator guiderMediator;
        private readonly IWeatherDataMediator weatherDataMediator;
        private readonly ISwitchMediator switchMediator;

        public ApplicationDeviceConnectionVM(IProfileService profileService, ICameraMediator camMediator, ITelescopeMediator teleMediator,
            IFocuserMediator focMediator, IFilterWheelMediator fwMediator, IRotatorMediator rotMediator, IFlatDeviceMediator flatdMediator, IGuiderMediator guidMediator,
            ISwitchMediator swMediator, IWeatherDataMediator weatherMediator,
            IDomeMediator domMediator) : base(profileService) {
            cameraMediator = camMediator;
            telescopeMediator = teleMediator;
            focuserMediator = focMediator;
            filterWheelMediator = fwMediator;
            rotatorMediator = rotMediator;
            flatDeviceMediator = flatdMediator;
            guiderMediator = guidMediator;
            switchMediator = swMediator;
            domeMediator = domMediator;
            weatherDataMediator = weatherMediator;

            ConnectAllDevicesCommand = new AsyncCommand<bool>(async () => {
                var diag = MyMessageBox.MyMessageBox.Show(Locale.Loc.Instance["LblReconnectAll"], "", MessageBoxButton.OKCancel, MessageBoxResult.Cancel);
                if (diag == MessageBoxResult.OK) {
                    return await Task<bool>.Run(async () => {
                        var cam = cameraMediator.Connect();
                        var fw = filterWheelMediator.Connect();
                        var telescope = telescopeMediator.Connect();
                        var focuser = focuserMediator.Connect();
                        var rotator = rotatorMediator.Connect();
                        var flatdevice = flatDeviceMediator.Connect();
                        var guider = guiderMediator.Connect();
                        var weather = weatherDataMediator.Connect();
                        var swtch = switchMediator.Connect();
                        var dome = domeMediator.Connect();
                        await Task.WhenAll(cam, fw, telescope, focuser, rotator, flatdevice, guider, weather, swtch, dome);
                        return true;
                    });
                } else {
                    return false;
                }
            });

            DisconnectAllDevicesCommand = new RelayCommand((object o) => {
                var diag = MyMessageBox.MyMessageBox.Show(Locale.Loc.Instance["LblDisconnectAll"], "", MessageBoxButton.OKCancel, MessageBoxResult.Cancel);
                if (diag == MessageBoxResult.OK) {
                    DisconnectEquipment();
                }
            });

            ClosingCommand = new RelayCommand(ClosingApplication);
        }

        private void ClosingApplication(object o) {
            DisconnectEquipment();
        }

        public void DisconnectEquipment() {
            try {
                cameraMediator.Disconnect();
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            try {
                telescopeMediator.Disconnect();
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            try {
                domeMediator.Disconnect();
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            try {
                filterWheelMediator.Disconnect();
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            try {
                focuserMediator.Disconnect();
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            try {
                rotatorMediator.Disconnect();
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            try {
                flatDeviceMediator.Disconnect();
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            try {
                guiderMediator.Disconnect();
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            try {
                weatherDataMediator.Disconnect();
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        public ICommand ClosingCommand { get; private set; }

        public ICommand ConnectAllDevicesCommand { get; private set; }
        public ICommand DisconnectAllDevicesCommand { get; private set; }
    }
}