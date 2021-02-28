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
        private readonly ISafetyMonitorMediator safetyMonitorMediator;

        public ApplicationDeviceConnectionVM(IProfileService profileService, ICameraMediator camMediator, ITelescopeMediator teleMediator,
            IFocuserMediator focMediator, IFilterWheelMediator fwMediator, IRotatorMediator rotMediator, IFlatDeviceMediator flatdMediator, IGuiderMediator guidMediator,
            ISwitchMediator swMediator, IWeatherDataMediator weatherMediator,
            IDomeMediator domMediator, ISafetyMonitorMediator safetyMonitorMediator) : base(profileService) {
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
            this.safetyMonitorMediator = safetyMonitorMediator;

            ConnectAllDevicesCommand = new AsyncCommand<bool>(async () => {
                var diag = MyMessageBox.MyMessageBox.Show(Locale.Loc.Instance["LblConnectAll"], "", MessageBoxButton.OKCancel, MessageBoxResult.Cancel);
                if (diag == MessageBoxResult.OK) {
                    return await Task<bool>.Run(async () => {
                        try {
                            Logger.Debug("Connecting to camera");
                            await cameraMediator.Connect();
                        } catch (Exception ex) {
                            Logger.Error(ex);
                        }
                        try {
                            Logger.Debug("Connecting to Filter Wheel");
                            await filterWheelMediator.Connect();
                        } catch (Exception ex) {
                            Logger.Error(ex);
                        }
                        try {
                            Logger.Debug("Connecting to Telescope");
                            await telescopeMediator.Connect();
                        } catch (Exception ex) {
                            Logger.Error(ex);
                        }
                        try {
                            Logger.Debug("Connecting to Focuser");
                            await focuserMediator.Connect();
                        } catch (Exception ex) {
                            Logger.Error(ex);
                        }
                        try {
                            Logger.Debug("Connecting to Rotator");
                            await rotatorMediator.Connect();
                        } catch (Exception ex) {
                            Logger.Error(ex);
                        }
                        try {
                            Logger.Debug("Connecting to Guider");
                            await guiderMediator.Connect();
                        } catch (Exception ex) {
                            Logger.Error(ex);
                        }
                        try {
                            Logger.Debug("Connecting to Flat Device");
                            await flatDeviceMediator.Connect();
                        } catch (Exception ex) {
                            Logger.Error(ex);
                        }
                        try {
                            Logger.Debug("Connecting to Weather Data");
                            await weatherDataMediator.Connect();
                        } catch (Exception ex) {
                            Logger.Error(ex);
                        }
                        try {
                            Logger.Debug("Connecting to Switch");
                            await switchMediator.Connect();
                        } catch (Exception ex) {
                            Logger.Error(ex);
                        }
                        try {
                            Logger.Debug("Connecting to Dome");
                            await domeMediator.Connect();
                        } catch (Exception ex) {
                            Logger.Error(ex);
                        }
                        try {
                            Logger.Debug("Connecting to Safety Monitor");
                            await safetyMonitorMediator.Connect();
                        } catch (Exception ex) {
                            Logger.Error(ex);
                        }
                        AllConnected = true;
                        return true;
                    });
                } else {
                    AllConnected = false;
                    return false;
                }
            });

            DisconnectAllDevicesCommand = new RelayCommand((object o) => {
                var diag = MyMessageBox.MyMessageBox.Show(Locale.Loc.Instance["LblDisconnectAll"], "", MessageBoxButton.OKCancel, MessageBoxResult.Cancel);
                if (diag == MessageBoxResult.OK) {
                    DisconnectEquipment();
                    AllConnected = false;
                }
            });

            ClosingCommand = new RelayCommand(ClosingApplication);
        }

        private bool allConnected = false;

        public bool AllConnected {
            get => allConnected;
            set {
                allConnected = value;
                RaisePropertyChanged();
            }
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
                switchMediator.Disconnect();
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            try {
                weatherDataMediator.Disconnect();
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            try {
                safetyMonitorMediator.Disconnect();
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        public ICommand ClosingCommand { get; private set; }

        public ICommand ConnectAllDevicesCommand { get; private set; }
        public ICommand DisconnectAllDevicesCommand { get; private set; }
    }
}