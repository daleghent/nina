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
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using NINA.Profile;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NINA.Model.MyDome;

namespace NINA.ViewModel.Equipment.Dome {

    internal class DomeVM : DockableVM, IDomeVM {

        public DomeVM(IProfileService profileService, IDomeMediator domeMediator, IApplicationStatusMediator applicationStatusMediator) : base(profileService) {
            Title = "LblDome";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["ObservatorySVG"];

            this.domeMediator = domeMediator;
            this.domeMediator.RegisterHandler(this);
            this.applicationStatusMediator = applicationStatusMediator;

            ChooseDomeCommand = new AsyncCommand<bool>(() => ChooseDome());
            CancelChooseDomeCommand = new RelayCommand(CancelChooseDome);
            DisconnectCommand = new AsyncCommand<bool>(() => DisconnectDiag());
            RefreshDomeListCommand = new RelayCommand(RefreshDomeList, o => !(Dome?.Connected == true));
            StopCommand = new RelayCommand(StopAll);
            DomeRotateCommand = new RelayCommand(DomeRotate);
            StopDomeRotateCommand = new RelayCommand(StopDomeRotate);
            OpenShutterCommand = new AsyncCommand<bool>(OpenShutter);
            CloseShutterCommand = new AsyncCommand<bool>(CloseShutter);
            SetParkPositionCommand = new RelayCommand(SetParkPosition);
            ParkCommand = new AsyncCommand<bool>(Park);
            ManualSlewCommand = new AsyncCommand<bool>(ManualSlew);

            this.updateTimer = new DeviceUpdateTimer(
                GetDomeValues,
                UpdateDomeValues,
                profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval
            );

            profileService.ProfileChanged += (object sender, EventArgs e) => {
                RefreshDomeList(null);
            };
        }

        private CancellationTokenSource cancelChooseDomeSource;

        private readonly SemaphoreSlim ss = new SemaphoreSlim(1, 1);

        private async Task<bool> ChooseDome() {
            await ss.WaitAsync();
            try {
                await Disconnect();
                if (updateTimer != null) {
                    await updateTimer.Stop();
                }

                if (DomeChooserVM.SelectedDevice.Id == "No_Device") {
                    profileService.ActiveProfile.DomeSettings.Id = DomeChooserVM.SelectedDevice.Id;
                    return false;
                }

                applicationStatusMediator.StatusUpdate(
                    new ApplicationStatus() {
                        Source = Title,
                        Status = Locale.Loc.Instance["LblConnecting"]
                    }
                );

                var dome = (IDome)DomeChooserVM.SelectedDevice;
                cancelChooseDomeSource?.Dispose();
                cancelChooseDomeSource = new CancellationTokenSource();
                if (dome != null) {
                    try {
                        var connected = await dome?.Connect(cancelChooseDomeSource.Token);
                        cancelChooseDomeSource.Token.ThrowIfCancellationRequested();
                        if (connected) {
                            Dome = dome;

                            DomeInfo = new DomeInfo {
                                Connected = true,
                                ShutterStatus = Dome.ShutterStatus,
                                DriverCanSlave = Dome.DriverCanSlave,
                                CanSetShutter = Dome.CanSetShutter,
                                CanSetPark = Dome.CanSetPark,
                                CanSetAzimuth = Dome.CanSetAzimuth,
                                CanSyncAzimuth = Dome.CanSyncAzimuth,
                                CanPark = Dome.CanPark,
                                CanFindHome = Dome.CanFindHome,
                                AtPark = Dome.AtPark,
                                AtHome = Dome.AtPark,
                                DriverSlaved = Dome.DriverSlaved,
                                Slewing = Dome.Slewing,
                                Azimuth = Dome.Azimuth
                            };

                            BroadcastDomeInfo();

                            Notification.ShowSuccess(Locale.Loc.Instance["LblDomeConnected"]);

                            updateTimer.Start();

                            profileService.ActiveProfile.DomeSettings.Id = Dome.Id;

                            Logger.Info($"Successfully connected Dome. Id: {Dome.Id} Name: {Dome.Name} Driver Version: {Dome.DriverVersion}");

                            return true;
                        } else {
                            DomeInfo.Connected = false;
                            Dome = null;
                            return false;
                        }
                    } catch (OperationCanceledException) {
                        if (DomeInfo.Connected) { await Disconnect(); }
                        return false;
                    }
                } else {
                    return false;
                }
            } finally {
                ss.Release();
                applicationStatusMediator.StatusUpdate(
                    new ApplicationStatus() {
                        Source = Title,
                        Status = string.Empty
                    }
                );
            }
        }

        private void CancelChooseDome(object o) {
            cancelChooseDomeSource?.Cancel();
        }

        private Dictionary<string, object> GetDomeValues() {
            Dictionary<string, object> domeValues = new Dictionary<string, object> {
                { nameof(DomeInfo.Connected), Dome?.Connected ?? false },
                { nameof(DomeInfo.ShutterStatus), Dome?.ShutterStatus ?? ShutterState.ShutterError },
                { nameof(DomeInfo.DriverCanSlave), Dome?.DriverCanSlave ?? false },
                { nameof(DomeInfo.CanSetShutter), Dome?.CanSetShutter ?? false },
                { nameof(DomeInfo.CanSetPark), Dome?.CanSetPark ?? false },
                { nameof(DomeInfo.CanSetAzimuth), Dome?.CanSetAzimuth ?? false },
                { nameof(DomeInfo.CanSyncAzimuth), Dome?.CanSyncAzimuth ?? false },
                { nameof(DomeInfo.CanPark), Dome?.CanPark ?? false },
                { nameof(DomeInfo.CanFindHome), Dome?.CanFindHome ?? false },
                { nameof(DomeInfo.AtPark), Dome?.AtPark ?? false },
                { nameof(DomeInfo.AtHome), Dome?.AtHome ?? false },
                { nameof(DomeInfo.DriverSlaved), Dome?.DriverSlaved ?? false },
                { nameof(DomeInfo.Slewing), Dome?.Slewing ?? false },
                { nameof(DomeInfo.Azimuth), Dome?.Azimuth ?? Double.NaN }
            };

            return domeValues;
        }

        private void UpdateDomeValues(Dictionary<string, object> domeValues) {
            object o;

            domeValues.TryGetValue(nameof(DomeInfo.Connected), out o);
            DomeInfo.Connected = (bool)(o ?? false);

            domeValues.TryGetValue(nameof(DomeInfo.ShutterStatus), out o);
            DomeInfo.ShutterStatus = (ShutterState)(o ?? ShutterState.ShutterError);

            domeValues.TryGetValue(nameof(DomeInfo.DriverCanSlave), out o);
            DomeInfo.DriverCanSlave = (bool)(o ?? false);

            domeValues.TryGetValue(nameof(DomeInfo.CanSetShutter), out o);
            DomeInfo.CanSetShutter = (bool)(o ?? false);

            domeValues.TryGetValue(nameof(DomeInfo.CanSetPark), out o);
            DomeInfo.CanSetPark = (bool)(o ?? false);

            domeValues.TryGetValue(nameof(DomeInfo.CanSetAzimuth), out o);
            DomeInfo.CanSetAzimuth = (bool)(o ?? false);

            domeValues.TryGetValue(nameof(DomeInfo.CanSyncAzimuth), out o);
            DomeInfo.CanSyncAzimuth = (bool)(o ?? false);

            domeValues.TryGetValue(nameof(DomeInfo.CanPark), out o);
            DomeInfo.CanPark = (bool)(o ?? false);

            domeValues.TryGetValue(nameof(DomeInfo.CanFindHome), out o);
            DomeInfo.CanFindHome = (bool)(o ?? false);

            domeValues.TryGetValue(nameof(DomeInfo.AtPark), out o);
            DomeInfo.AtPark = (bool)(o ?? false);

            domeValues.TryGetValue(nameof(DomeInfo.AtHome), out o);
            DomeInfo.AtHome = (bool)(o ?? false);

            domeValues.TryGetValue(nameof(DomeInfo.DriverSlaved), out o);
            DomeInfo.DriverSlaved = (bool)(o ?? false);

            domeValues.TryGetValue(nameof(DomeInfo.Slewing), out o);
            DomeInfo.Slewing = (bool)(o ?? false);

            domeValues.TryGetValue(nameof(DomeInfo.Azimuth), out o);
            DomeInfo.Azimuth = (double)(o ?? Double.NaN);

            BroadcastDomeInfo();
        }

        public IProfile ActiveProfile => profileService.ActiveProfile;

        private DomeInfo domeInfo;

        public DomeInfo DomeInfo {
            get {
                if (domeInfo == null) {
                    domeInfo = DeviceInfo.CreateDefaultInstance<DomeInfo>();
                }
                return domeInfo;
            }
            set {
                domeInfo = value;
                RaisePropertyChanged();
            }
        }

        public DomeInfo GetDeviceInfo() {
            return DomeInfo;
        }

        private void BroadcastDomeInfo() {
            domeMediator.Broadcast(DomeInfo);
        }

        public void RefreshDomeList(object obj) {
            DomeChooserVM.GetEquipment();
        }

        public Task<bool> Connect() {
            return ChooseDome();
        }

        private async Task<bool> DisconnectDiag() {
            var diag = MyMessageBox.MyMessageBox.Show(Locale.Loc.Instance["LblDomeDisconnect"], "", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);
            if (diag == System.Windows.MessageBoxResult.OK) {
                await Disconnect();
            }
            return true;
        }

        public async Task Disconnect() {
            if (Dome != null) { Logger.Info("Disconnected Dome Device"); }
            if (updateTimer != null) {
                await updateTimer.Stop();
            }
            Dome?.Disconnect();
            Dome = null;
            DomeInfo = DeviceInfo.CreateDefaultInstance<DomeInfo>();
            BroadcastDomeInfo();
            RaisePropertyChanged(nameof(Dome));
        }

        private IDome dome;

        public IDome Dome {
            get {
                return dome;
            }
            private set {
                dome = value;
                RaisePropertyChanged();
            }
        }

        private DomeChooserVM domeChooserVM;

        public DomeChooserVM DomeChooserVM {
            get {
                if (domeChooserVM == null) {
                    domeChooserVM = new DomeChooserVM(profileService);
                    domeChooserVM.GetEquipment();
                }
                return domeChooserVM;
            }
            set => domeChooserVM = value;
        }
        public async Task<bool> OpenShutter() {
            if (Dome.CanSetShutter) {
                Logger.Trace("Opening dome shutter");
                await Dome.OpenShutter(CancellationToken.None);
                return true;
            } else {
                Logger.Error("Cannot open shutter. Dome does not support it.");
                return false;
            }
        }

        public async Task<bool> CloseShutter() {
            if (Dome.CanSetShutter) {
                Logger.Trace("Closing dome shutter");
                await Dome.CloseShutter(CancellationToken.None);
                return true;
            } else {
                Logger.Error("Cannot open shutter. Dome does not support it.");
                return false;
            }
        }

        public async Task<bool> Park() {
            if (Dome.CanPark) {
                Logger.Trace("Parking dome");
                await Dome.Park(CancellationToken.None);
                return true;
            } else {
                Logger.Error("Cannot park shutter. Dome does not support it.");
                return false;
            }
        }

        private CancellationTokenSource domeManualRotationCTS;
        private Task domeManualRotationTask;
        private void DomeRotate(object p) {
            string direction = p.ToString();
            if (domeManualRotationCTS != null || domeManualRotationTask != null) {
                Notification.ShowError("Cannot manually rotate the dome while another manual rotation is in progress");
                return;
            }
            domeManualRotationCTS = new CancellationTokenSource();
            if (direction == "CW") {
                domeManualRotationTask = Dome.StartRotateCW(domeManualRotationCTS.Token);
            } else if (direction == "CCW") {
                domeManualRotationTask = Dome.StartRotateCCW(domeManualRotationCTS.Token);
            } else {
                throw new InvalidOperationException($"{direction} is not a valid direction");
            }
        }

        private void StopDomeRotate(object p) {
            domeManualRotationCTS?.Cancel();
            domeManualRotationCTS = null;
            domeManualRotationTask = null;
        }

        private void StopAll(object p) {
            StopDomeRotate(null);
            Dome?.StopSlewing();
            Dome?.StopShutter();
        }

        private void SetParkPosition(object p) {
            Dome?.SetPark();
        }

        private double targetAzimuthDegrees;

        public double TargetAzimuthDegrees {
            get {
                return targetAzimuthDegrees;
            }

            set {
                targetAzimuthDegrees = value;
                RaisePropertyChanged();
            }
        }

        private async Task<bool> ManualSlew(object obj) {
            if (Dome.CanSetAzimuth) {
                await Dome?.SlewToAzimuth(TargetAzimuthDegrees, CancellationToken.None);
                return true;
            } else {
                return false;
            }
        }

        private readonly DeviceUpdateTimer updateTimer;
        private readonly IDomeMediator domeMediator;
        private readonly IApplicationStatusMediator applicationStatusMediator;
        public IAsyncCommand ChooseDomeCommand { get; private set; }
        public ICommand RefreshDomeListCommand { get; private set; }
        public ICommand CancelChooseDomeCommand { get; private set; }
        public ICommand DisconnectCommand { get; private set; }
        public ICommand StopCommand { get; private set; }
        public ICommand StopDomeRotateCommand { get; private set; }
        public ICommand DomeRotateCommand { get; private set; }
        public ICommand OpenShutterCommand { get; private set; }
        public ICommand CloseShutterCommand { get; private set; }
        public ICommand ParkCommand { get; private set; }
        public ICommand SetParkPositionCommand { get; private set; }
        public ICommand ManualSlewCommand { get; private set; }
    }
}