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
using NINA.Model.MyTelescope;
using NINA.Utility.Astrometry;
using Nito.AsyncEx;
using ToastNotifications.Utilities;
using Ninject;

namespace NINA.ViewModel.Equipment.Dome {

    internal class DomeVM : DockableVM, IDomeVM, ITelescopeConsumer {

        public DomeVM(
            IProfileService profileService,
            IDomeMediator domeMediator,
            IApplicationStatusMediator applicationStatusMediator,
            ITelescopeMediator telescopeMediator,
            IDeviceChooserVM domeChooserVM,
            IDomeSynchronization domeSynchronization,
            IApplicationResourceDictionary resourceDictionary,
            IDeviceUpdateTimerFactory deviceUpdateTimerFactory) : base(profileService) {
            Title = "LblDome";
            ImageGeometry = (System.Windows.Media.GeometryGroup)resourceDictionary["ObservatorySVG"];

            this.domeMediator = domeMediator;
            this.domeMediator.RegisterHandler(this);
            this.telescopeMediator = telescopeMediator;
            this.telescopeMediator.RegisterConsumer(this);
            this.applicationStatusMediator = applicationStatusMediator;
            this.domeSynchronization = domeSynchronization;
            DomeChooserVM = domeChooserVM;

            ChooseDomeCommand = new AsyncCommand<bool>(() => ChooseDome());
            CancelChooseDomeCommand = new RelayCommand(CancelChooseDome);
            DisconnectCommand = new AsyncCommand<bool>(() => DisconnectDiag());
            RefreshDomeListCommand = new RelayCommand(RefreshDomeList, o => !(Dome?.Connected == true));
            StopCommand = new RelayCommand(StopAll);
            OpenShutterCommand = new AsyncCommand<bool>(OpenShutterVM);
            CloseShutterCommand = new AsyncCommand<bool>(CloseShutterVM);
            SetParkPositionCommand = new RelayCommand(SetParkPosition);
            ParkCommand = new AsyncCommand<bool>(ParkVM);
            ManualSlewCommand = new AsyncCommand<bool>(ManualSlew);
            FindHomeCommand = new AsyncCommand<bool>(FindHome);

            this.updateTimer = deviceUpdateTimerFactory.Create(
                GetDomeValues,
                UpdateDomeValues,
                profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval
            );

            profileService.ProfileChanged += (object sender, EventArgs e) => {
                RefreshDomeList(null);
            };

            DirectFollowToggled = profileService.ActiveProfile.DomeSettings.UseDirectFollowing;
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
                                DriverCanFollow = Dome.DriverCanFollow,
                                CanSetShutter = Dome.CanSetShutter,
                                CanSetPark = Dome.CanSetPark,
                                CanSetAzimuth = Dome.CanSetAzimuth,
                                CanSyncAzimuth = Dome.CanSyncAzimuth,
                                CanPark = Dome.CanPark,
                                CanFindHome = Dome.CanFindHome,
                                AtPark = Dome.AtPark,
                                AtHome = Dome.AtPark,
                                DriverFollowing = Dome.DriverFollowing,
                                Slewing = Dome.Slewing,
                                Azimuth = Dome.Azimuth
                            };

                            RaiseAllPropertiesChanged();
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
                { nameof(DomeInfo.DriverCanFollow), Dome?.DriverCanFollow ?? false },
                { nameof(DomeInfo.CanSetShutter), Dome?.CanSetShutter ?? false },
                { nameof(DomeInfo.CanSetPark), Dome?.CanSetPark ?? false },
                { nameof(DomeInfo.CanSetAzimuth), Dome?.CanSetAzimuth ?? false },
                { nameof(DomeInfo.CanSyncAzimuth), Dome?.CanSyncAzimuth ?? false },
                { nameof(DomeInfo.CanPark), Dome?.CanPark ?? false },
                { nameof(DomeInfo.CanFindHome), Dome?.CanFindHome ?? false },
                { nameof(DomeInfo.AtPark), Dome?.AtPark ?? false },
                { nameof(DomeInfo.AtHome), Dome?.AtHome ?? false },
                { nameof(DomeInfo.DriverFollowing), Dome?.DriverFollowing ?? false },
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

            domeValues.TryGetValue(nameof(DomeInfo.DriverCanFollow), out o);
            DomeInfo.DriverCanFollow = (bool)(o ?? false);

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

            domeValues.TryGetValue(nameof(DomeInfo.DriverFollowing), out o);
            DomeInfo.DriverFollowing = (bool)(o ?? false);

            domeValues.TryGetValue(nameof(DomeInfo.Slewing), out o);
            DomeInfo.Slewing = (bool)(o ?? false);

            domeValues.TryGetValue(nameof(DomeInfo.Azimuth), out o);
            DomeInfo.Azimuth = (double)(o ?? Double.NaN);

            BroadcastDomeInfo();
        }

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
            RaiseAllPropertiesChanged();
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

        public IDeviceChooserVM DomeChooserVM { get; private set; }

        private Task<bool> OpenShutterVM() {
            return OpenShutter(CancellationToken.None);
        }

        public async Task<bool> OpenShutter(CancellationToken cancellationToken) {
            if (Dome.CanSetShutter) {
                Logger.Trace("Opening dome shutter");
                await Dome.OpenShutter(cancellationToken);
                return true;
            } else {
                Logger.Warning("Cannot open shutter. Dome does not support it.");
                return false;
            }
        }

        private Task<bool> CloseShutterVM() {
            return CloseShutter(CancellationToken.None);
        }

        public async Task<bool> CloseShutter(CancellationToken cancellationToken) {
            if (Dome.CanSetShutter) {
                Logger.Trace("Closing dome shutter");
                await Dome.CloseShutter(cancellationToken);
                return true;
            } else {
                Logger.Warning("Cannot close shutter. Dome does not support it.");
                return false;
            }
        }

        private Task<bool> ParkVM() {
            return Park(CancellationToken.None);
        }

        public async Task<bool> Park(CancellationToken cancellationToken) {
            if (Dome.CanPark) {
                Logger.Trace("Parking dome");
                await DisableFollowing(cancellationToken);
                if (profileService.ActiveProfile.DomeSettings.FindHomeBeforePark && Dome.CanFindHome) {
                    await Dome.FindHome(cancellationToken);
                }
                await Dome.Park(cancellationToken);
                return true;
            } else {
                Logger.Error("Cannot park shutter. Dome does not support it.");
                return false;
            }
        }

        private CancellationTokenSource domeRotationCTS;
        private Task domeRotationTask;

        public async Task WaitForDomeSynchronization(CancellationToken cancellationToken) {
            var timeoutCTS = new CancellationTokenSource(TimeSpan.FromSeconds(profileService.ActiveProfile.DomeSettings.DomeSyncTimeoutSeconds));
            var timeoutOrClientCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(timeoutCTS.Token, cancellationToken).Token;
            while (DirectFollowEnabled && !IsSynchronized) {
                if (timeoutOrClientCancellationToken.IsCancellationRequested) {
                    Logger.Warning("Waiting for Dome synchronization cancelled or timed out");
                    return;
                }
                await Task.Delay(TimeSpan.FromSeconds(1), timeoutOrClientCancellationToken);
            }
        }

        private void StopDomeRotate(object p) {
            domeRotationCTS?.Cancel();
            domeRotationCTS = null;
            domeRotationTask = null;
        }

        private void StopAll(object p) {
            StopDomeRotate(null);
            Dome?.StopAll();
            FollowEnabled = false;
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

        private async Task<bool> FindHome(object obj) {
            await Dome?.FindHome(CancellationToken.None);
            return true;
        }

        private bool followEnabled;
        public bool FollowEnabled {
            get {
                if (Dome?.Connected == true) {
                    return followEnabled;
                } else {
                    return false;
                }
            }
            set {
                followEnabled = value;
                OnFollowChanged();
                RaisePropertyChanged();
            }
        }

        private bool directFollowingToggled;
        public bool DirectFollowToggled {
            get {
                if (Dome?.Connected == true) {
                    if (Dome.DriverCanFollow) {
                        return directFollowingToggled;
                    } else {
                        return true;
                    }
                } else {
                    return true;
                }
            }
            set {
                directFollowingToggled = value;
                profileService.ActiveProfile.DomeSettings.UseDirectFollowing = value;
                OnFollowChanged();
                RaisePropertyChanged();
            }
        }

        public bool DirectFollowEnabled {
            get {
                if (Dome?.Connected == true) {
                    return FollowEnabled && DirectFollowToggled;
                } else {
                    return false;
                }
            }
        }

        private void OnFollowChanged() {
            if (Dome?.Connected == true) {
                if (Dome.DriverCanFollow) {
                    Dome.DriverFollowing = FollowEnabled && !DirectFollowToggled;
                }
                if (TelescopeInfo.Connected && DirectFollowEnabled) {
                    SynchronizeWithTelescope(TelescopeInfo);
                }
            }
        }

        private double targetAzimuth = 0.0;
        private void SynchronizeWithTelescope(TelescopeInfo telescopeInfo) {
            if (Dome?.Connected == true) {
                // If domeRotationTask is null or IsCompleted is true, then it is not rotating
                var isRotating = domeRotationTask?.IsCompleted == false;
                if (!isRotating) {
                    var calculatedTargetAzimuth = GetSynchronizedPosition(telescopeInfo);
                    var currentAzimuth = Angle.ByDegree(Dome.Azimuth);
                    var tolerance = Angle.ByDegree(profileService.ActiveProfile.DomeSettings.AzimuthTolerance_degrees);
                    if (!calculatedTargetAzimuth.Equals(currentAzimuth, tolerance)) {
                        Logger.Trace($"Dome direct telescope follow slew. Current azimuth={currentAzimuth}, Target azimuth={targetAzimuth}");
                        targetAzimuth = calculatedTargetAzimuth.Degree;
                        domeRotationCTS = new CancellationTokenSource();
                        domeRotationTask = Dome.SlewToAzimuth(targetAzimuth, domeRotationCTS.Token);
                    }
                }
            }
        }

        private Angle GetSynchronizedPosition(TelescopeInfo telescopeInfo) {
            var targetCoordinates = telescopeInfo.TargetCoordinates ?? telescopeInfo.Coordinates;
            var targetSideOfPier = telescopeInfo.TargetSideOfPier ?? telescopeInfo.SideOfPier;
            return domeSynchronization.TargetDomeAzimuth(
                scopeCoordinates: targetCoordinates,
                localSiderealTime: telescopeInfo.SiderealTime,
                siteLatitude: Angle.ByDegree(telescopeInfo.SiteLatitude),
                siteLongitude: Angle.ByDegree(telescopeInfo.SiteLongitude),
                sideOfPier: targetSideOfPier);
        }

        private bool IsSynchronized {
            get {
                if (Dome?.Connected == true) {
                    var calculatedTargetAzimuth = GetSynchronizedPosition(TelescopeInfo);
                    var currentAzimuth = Angle.ByDegree(Dome.Azimuth);
                    var tolerance = Angle.ByDegree(profileService.ActiveProfile.DomeSettings.AzimuthTolerance_degrees);
                    return calculatedTargetAzimuth.Equals(currentAzimuth, tolerance);
                }
                return false;
            }
        }

        public void UpdateDeviceInfo(TelescopeInfo deviceInfo) {
            try {
                TelescopeInfo = deviceInfo;
                if (TelescopeInfo.Connected && DirectFollowEnabled) {
                    SynchronizeWithTelescope(TelescopeInfo);
                } else if (!TelescopeInfo.Connected) {
                    FollowEnabled = false;
                }
            } catch (Exception e) {
                Notification.ShowError(String.Format(Locale.Loc.Instance["LblDomeFollowFailure"], e.Message));
                FollowEnabled = false;
            }
        }

        private TelescopeInfo telescopeInfo = DeviceInfo.CreateDefaultInstance<TelescopeInfo>();
        public TelescopeInfo TelescopeInfo {
            get {
                return telescopeInfo;
            } set {
                telescopeInfo = value;
                RaisePropertyChanged();
            }
        }

        public void Dispose() {
            this.telescopeMediator?.RemoveConsumer(this);
            this.telescopeMediator = null;
        }

        public async Task<bool> EnableFollowing(CancellationToken cancellationToken) {
            if (!Dome.Connected) {
                return false;
            }

            FollowEnabled = true;
            while (Dome.Slewing && !cancellationToken.IsCancellationRequested) {
                await Task.Delay(1000, cancellationToken);
            }
            return FollowEnabled;
        }

        public async Task<bool> DisableFollowing(CancellationToken cancellationToken) {
            if (!Dome.Connected) {
                return false;
            }

            FollowEnabled = false;
            while (Dome.Slewing && !cancellationToken.IsCancellationRequested) {
                await Task.Delay(1000, cancellationToken);
            }
            return !FollowEnabled;
        }

        private readonly IDeviceUpdateTimer updateTimer;
        private readonly IDomeMediator domeMediator;
        private readonly IApplicationStatusMediator applicationStatusMediator;
        private ITelescopeMediator telescopeMediator;
        private IDomeSynchronization domeSynchronization;
        public IAsyncCommand ChooseDomeCommand { get; private set; }
        public ICommand RefreshDomeListCommand { get; private set; }
        public ICommand CancelChooseDomeCommand { get; private set; }
        public ICommand DisconnectCommand { get; private set; }
        public ICommand StopCommand { get; private set; }
        public ICommand StopDomeRotateCommand { get; private set; }
        public ICommand OpenShutterCommand { get; private set; }
        public ICommand CloseShutterCommand { get; private set; }
        public ICommand ParkCommand { get; private set; }
        public ICommand SetParkPositionCommand { get; private set; }
        public ICommand ManualSlewCommand { get; private set; }
        public ICommand FindHomeCommand { get; private set; }
    }
}