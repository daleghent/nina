#region "copyright"

/*
    Copyright ? 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Core.Utility.Notification;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NINA.Equipment.Equipment.MyDome;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Astrometry;
using System.ComponentModel;
using NINA.Equipment.Equipment.MySafetyMonitor;
using NINA.Core.Locale;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Core.Model;
using NINA.Core.MyMessageBox;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Equipment;
using Nito.AsyncEx;

namespace NINA.WPF.Base.ViewModel.Equipment.Dome {

    public class DomeVM : DockableVM, IDomeVM, ITelescopeConsumer, ISafetyMonitorConsumer {

        public DomeVM(
            IProfileService profileService,
            IDomeMediator domeMediator,
            IApplicationStatusMediator applicationStatusMediator,
            ITelescopeMediator telescopeMediator,
            IDeviceChooserVM domeChooserVM,
            IDomeFollower domeFollower,
            ISafetyMonitorMediator safetyMonitorMediator,
            IApplicationResourceDictionary resourceDictionary,
            IDeviceUpdateTimerFactory deviceUpdateTimerFactory) : base(profileService) {
            Title = Loc.Instance["LblDome"];
            ImageGeometry = (System.Windows.Media.GeometryGroup)resourceDictionary["ObservatorySVG"];

            this.domeMediator = domeMediator;
            this.domeMediator.RegisterHandler(this);
            this.telescopeMediator = telescopeMediator;
            this.telescopeMediator.RegisterConsumer(this);
            this.applicationStatusMediator = applicationStatusMediator;
            this.safetyMonitorMediator = safetyMonitorMediator;
            this.safetyMonitorMediator.RegisterConsumer(this);
            DomeChooserVM = domeChooserVM;
            _ = Rescan();
            this.domeFollower = domeFollower;
            this.domeFollower.PropertyChanged += DomeFollower_PropertyChanged;

            ChooseDomeCommand = new AsyncCommand<bool>(() => ChooseDome());
            CancelChooseDomeCommand = new RelayCommand(CancelChooseDome);
            DisconnectCommand = new AsyncCommand<bool>(() => DisconnectDiag());
            RefreshDomeListCommand = new AsyncCommand<bool>(async o => { await Rescan(); return true; }, o => !(Dome?.Connected == true));
            StopCommand = new AsyncCommand<bool>(StopAll);
            OpenShutterCommand = new AsyncCommand<bool>(OpenShutterVM);
            CloseShutterCommand = new AsyncCommand<bool>(CloseShutterVM);
            SetParkPositionCommand = new RelayCommand(SetParkPosition);
            ParkCommand = new AsyncCommand<bool>(ParkVM);
            ManualSlewCommand = new AsyncCommand<bool>(() => ManualSlew(TargetAzimuthDegrees));
            RotateCWCommand = new AsyncCommand<bool>(() => RotateRelative(RotateDegrees));
            RotateCCWCommand = new AsyncCommand<bool>(() => RotateRelative(-RotateDegrees));
            FindHomeCommand = new AsyncCommand<bool>(FindHome);
            SyncCommand = new RelayCommand(SyncAzimuth);

            this.updateTimer = deviceUpdateTimerFactory.Create(
                GetDomeValues,
                UpdateDomeValues,
                profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval
            );

            profileService.ProfileChanged += (object sender, EventArgs e) => {
                AsyncContext.Run(Rescan);
            };
        }

        public async Task Rescan() {
            await Task.Run(() => DomeChooserVM.GetEquipment());
        }

        private void DomeFollower_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(IDomeFollower.IsFollowing)) {
                if (!this.domeFollower.IsFollowing) {
                    this.FollowEnabled = false;
                }
            }
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
                        Status = Loc.Instance["LblConnecting"]
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

                            Notification.ShowSuccess(Loc.Instance["LblDomeConnected"]);

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

        public Task<bool> Connect() {
            return ChooseDome();
        }

        private async Task<bool> DisconnectDiag() {
            var diag = MyMessageBox.Show(Loc.Instance["LblDomeDisconnect"], "", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);
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
                if (SafetyMonitorInfo.Connected && !SafetyMonitorInfo.IsSafe) {
                    Logger.Error("Cannot open dome shutter due to unsafe conditions");
                    Notification.ShowError(Loc.Instance["LblDomeCloseOnUnsafeWarning"]);
                    return false;
                }

                Logger.Info($"Opening dome shutter. Shutter state after opening {DomeInfo.ShutterStatus}");
                await Dome.OpenShutter(cancellationToken);
                Logger.Info($"Opened dome shutter. Shutter state after opening {DomeInfo.ShutterStatus}");
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
                Logger.Info($"Closing dome shutter. Shutter state before closing {DomeInfo.ShutterStatus}");
                await Dome.CloseShutter(cancellationToken);
                Logger.Info($"Closed dome shutter. Shutter state after closing {DomeInfo.ShutterStatus}");
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
                Logger.Info("Parking dome");
                await DisableFollowing(cancellationToken);
                if (profileService.ActiveProfile.DomeSettings.FindHomeBeforePark && Dome.CanFindHome) {
                    Logger.Info("Finding home before parking");
                    await Dome.FindHome(cancellationToken);
                }
                await Dome.Park(cancellationToken);
                return true;
            } else {
                Logger.Error("Cannot park shutter. Dome does not support it.");
                return false;
            }
        }

        public async Task WaitForDomeSynchronization(CancellationToken cancellationToken) {
            await this.domeFollower.WaitForDomeSynchronization(cancellationToken);
        }

        private async Task<bool> StopAll(object p) {
            Logger.Info("Stopping all dome movement");
            try {
                await this.domeFollower.Stop();
            } catch (Exception ex) {
                Logger.Error("Stopping dome follower failed", ex.Message);
            }
            try {
                await Dome?.StopAll();
            } catch (Exception ex) {
                Logger.Error("Stopping all Dome actions failed", ex.Message);
            }
            FollowEnabled = false;
            return true;
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

        public double RotateDegrees {
            get {
                return profileService.ActiveProfile.DomeSettings.RotateDegrees;
            }

            set {
                if (profileService.ActiveProfile.DomeSettings.RotateDegrees != value) {
                    profileService.ActiveProfile.DomeSettings.RotateDegrees = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool CanSyncAzimuth {
            get {
                if (Dome?.Connected != true || TelescopeInfo?.Connected != true) {
                    return false;
                }
                return Dome.CanSyncAzimuth;
            }
        }

        public async Task<bool> SlewToAzimuth(double degrees, CancellationToken token) {
            if (Dome?.Connected == true) {
                Logger.Info($"Slewing dome to azimuth {degrees}°");
                await Dome?.SlewToAzimuth(degrees, token);
                return true;
            }
            return false;
        }

        private async Task<bool> ManualSlew(double degrees) {
            if (Dome.CanSetAzimuth) {
                this.FollowEnabled = false;
                Logger.Info($"Manually slewing dome to azimuth {degrees}°");
                return await SlewToAzimuth(degrees, CancellationToken.None);
            } else {
                return false;
            }
        }

        private async Task<bool> RotateRelative(double degrees) {
            if (Dome.CanSetAzimuth) {
                this.FollowEnabled = false;
                var targetAzimuth = AstroUtil.EuclidianModulus(this.Dome.Azimuth + degrees, 360.0);
                Logger.Info($"Rotating dome relatively by {degrees}°");
                return await SlewToAzimuth(targetAzimuth, CancellationToken.None);
            } else {
                return false;
            }
        }

        private async Task<bool> FindHome(object obj) {
            Logger.Info($"Finding dome home position");
            await DisableFollowing(CancellationToken.None);
            await Dome?.FindHome(CancellationToken.None);
            return true;
        }

        private void SyncAzimuth(object obj) {
            if (CanSyncAzimuth) {
                var calculatedTargetAzimuth = this.domeFollower.GetSynchronizedPosition(TelescopeInfo);
                Dome.SyncToAzimuth(calculatedTargetAzimuth.Degree);
            }
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
                if (followEnabled != value) {
                    followEnabled = value;
                    OnFollowChanged(followEnabled);
                    RaisePropertyChanged();
                }
            }
        }

        private void OnFollowChanged(bool followEnabled) {
            if (followEnabled && Dome?.Connected == true) {
                this.domeFollower.Start();
                Logger.Info($"Dome following enabled");
            } else {
                this.domeFollower.Stop();
                Logger.Info($"Dome following stopped");
            }
        }

        public void UpdateDeviceInfo(TelescopeInfo deviceInfo) {
            TelescopeInfo = deviceInfo;
        }

        private TelescopeInfo telescopeInfo = DeviceInfo.CreateDefaultInstance<TelescopeInfo>();

        public TelescopeInfo TelescopeInfo {
            get {
                return telescopeInfo;
            }
            private set {
                telescopeInfo = value;
                RaisePropertyChanged();
            }
        }

        private SafetyMonitorInfo safetyMonitorInfo = DeviceInfo.CreateDefaultInstance<SafetyMonitorInfo>();

        public SafetyMonitorInfo SafetyMonitorInfo {
            get {
                return safetyMonitorInfo;
            }
            private set {
                safetyMonitorInfo = value;
                RaisePropertyChanged();
            }
        }

        public void Dispose() {
            this.telescopeMediator?.RemoveConsumer(this);
            this.telescopeMediator = null;
            this.safetyMonitorMediator?.RemoveConsumer(this);
            this.safetyMonitorMediator = null;
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

        private object lockObj = new object();
        private Task closeShutterTask;

        public void UpdateDeviceInfo(SafetyMonitorInfo deviceInfo) {
            SafetyMonitorInfo = deviceInfo;
            if (Dome?.Connected == true && profileService.ActiveProfile.DomeSettings.CloseOnUnsafe) {
                //Close dome when state switches from safe to unsafe
                if (deviceInfo.Connected && !deviceInfo.IsSafe && Dome?.ShutterStatus == ShutterState.ShutterOpen) {
                    lock (lockObj) {
                        if (closeShutterTask == null || closeShutterTask.IsCompleted) {
                            closeShutterTask = Task.Run(async () => {
                                Logger.Warning("Closing dome shutter due to unsafe conditions");
                                Notification.ShowWarning(Loc.Instance["LblDomeCloseOnUnsafeWarning"]);
                                await StopAll(null);
                                return CloseShutter(CancellationToken.None);
                            });
                        }
                    }
                }
            }
        }

        private readonly IDeviceUpdateTimer updateTimer;
        private readonly IDomeMediator domeMediator;
        private readonly IApplicationStatusMediator applicationStatusMediator;
        private readonly IDomeFollower domeFollower;
        private ITelescopeMediator telescopeMediator;
        private ISafetyMonitorMediator safetyMonitorMediator;
        public IAsyncCommand ChooseDomeCommand { get; private set; }
        public ICommand RefreshDomeListCommand { get; private set; }
        public ICommand CancelChooseDomeCommand { get; private set; }
        public ICommand DisconnectCommand { get; private set; }
        public ICommand StopCommand { get; private set; }
        public ICommand OpenShutterCommand { get; private set; }
        public ICommand CloseShutterCommand { get; private set; }
        public ICommand ParkCommand { get; private set; }
        public ICommand SetParkPositionCommand { get; private set; }
        public ICommand ManualSlewCommand { get; private set; }
        public ICommand FindHomeCommand { get; private set; }
        public ICommand RotateCWCommand { get; private set; }
        public ICommand RotateCCWCommand { get; private set; }
        public ICommand SyncCommand { get; private set; }
    }
}