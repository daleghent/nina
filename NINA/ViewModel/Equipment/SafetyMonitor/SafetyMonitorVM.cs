#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Model.MySafetyMonitor;
using NINA.Profile;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel.Equipment.SafetyMonitor {

    internal class SafetyMonitorVM : DockableVM, ISafetyMonitorVM {
        private ISafetyMonitorMediator safetyMonitorMediator;
        private IApplicationStatusMediator applicationStatusMediator;
        private ISafetyMonitor safetyMonitor;
        private DeviceUpdateTimer updateTimer;
        private CancellationTokenSource connectCts;
        private readonly SemaphoreSlim ss = new SemaphoreSlim(1, 1);

        public SafetyMonitorVM(IProfileService profileService, ISafetyMonitorMediator safetyMonitorMediator, IApplicationStatusMediator applicationStatusMediator) : base(profileService) {
            Title = "LblSafetyMonitor";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["ShieldSVG"];

            this.safetyMonitorMediator = safetyMonitorMediator;
            this.safetyMonitorMediator.RegisterHandler(this);
            this.applicationStatusMediator = applicationStatusMediator;
            Task.Run(() => SafetyMonitorChooserVM.GetEquipment());

            ConnectCommand = new AsyncCommand<bool>(() => Connect());
            CancelConnectCommand = new RelayCommand(CancelConnect);
            DisconnectCommand = new AsyncCommand<bool>(() => DisconnectDiag());
            RefreshMonitorListCommand = new RelayCommand(RefreshMonitorList, o => !(safetyMonitor?.Connected == true));

            updateTimer = new DeviceUpdateTimer(
                GetMonitorValues,
                UpdateMonitorValues,
                profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval
            );

            profileService.ProfileChanged += (object sender, EventArgs e) => {
                RefreshMonitorList(null);
            };
        }

        private SafetyMonitorChooserVM safetyMonitorChooserVM;

        public SafetyMonitorChooserVM SafetyMonitorChooserVM {
            get {
                if (safetyMonitorChooserVM == null) {
                    safetyMonitorChooserVM = new SafetyMonitorChooserVM(profileService);
                }
                return safetyMonitorChooserVM;
            }
            set {
                safetyMonitorChooserVM = value;
            }
        }

        public void RefreshMonitorList(object obj) {
            SafetyMonitorChooserVM.GetEquipment();
        }

        private void UpdateMonitorValues(Dictionary<string, object> monitorValues) {
            object o = null;
            monitorValues.TryGetValue(nameof(SafetyMonitorInfo.Connected), out o);
            SafetyMonitorInfo.Connected = (bool)(o ?? false);

            monitorValues.TryGetValue(nameof(SafetyMonitorInfo.IsSafe), out o);
            SafetyMonitorInfo.IsSafe = (bool)(o ?? false);

            BroadcastMonitorInfo();
        }

        private void BroadcastMonitorInfo() {
            safetyMonitorMediator.Broadcast(GetDeviceInfo());
        }

        private Dictionary<string, object> GetMonitorValues() {
            Dictionary<string, object> safetyMonitorValues = new Dictionary<string, object>();
            safetyMonitorValues.Add(nameof(SafetyMonitorInfo.Connected), safetyMonitor?.Connected ?? false);
            safetyMonitorValues.Add(nameof(SafetyMonitorInfo.IsSafe), safetyMonitor?.IsSafe ?? false);

            return safetyMonitorValues;
        }

        public async Task<bool> Connect() {
            await ss.WaitAsync();
            try {
                await Disconnect();
                if (updateTimer != null) {
                    await updateTimer.Stop();
                }

                if (SafetyMonitorChooserVM.SelectedDevice.Id == "No_Device") {
                    profileService.ActiveProfile.SafetyMonitorSettings.Id = SafetyMonitorChooserVM.SelectedDevice.Id;
                    return false;
                }

                applicationStatusMediator.StatusUpdate(
                    new ApplicationStatus() {
                        Source = Title,
                        Status = Locale.Loc.Instance["LblConnecting"]
                    }
                );

                var sm = (ISafetyMonitor)SafetyMonitorChooserVM.SelectedDevice;
                connectCts?.Dispose();
                connectCts = new CancellationTokenSource();
                if (sm != null) {
                    try {
                        var connected = await sm?.Connect(connectCts.Token);
                        connectCts.Token.ThrowIfCancellationRequested();
                        if (connected) {
                            this.safetyMonitor = sm;

                            SafetyMonitorInfo = new SafetyMonitorInfo {
                                Connected = true,
                                IsSafe = sm.IsSafe,
                                Name = sm.Name,
                                Description = sm.Description,
                                DriverInfo = sm.DriverInfo,
                                DriverVersion = sm.DriverVersion
                            };

                            Notification.ShowSuccess(Locale.Loc.Instance["LblSafetyMonitorConnected"]);

                            updateTimer.Interval = profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval;
                            updateTimer.Start();

                            profileService.ActiveProfile.SafetyMonitorSettings.Id = sm.Id;

                            Logger.Info($"Successfully connected Safety Monitor. Id: {sm.Id} Name: {sm.Name} Driver Version: {sm.DriverVersion}");

                            return true;
                        } else {
                            SafetyMonitorInfo.Connected = false;
                            this.safetyMonitor = null;
                            return false;
                        }
                    } catch (OperationCanceledException) {
                        if (SafetyMonitorInfo.Connected) { await Disconnect(); }
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

        private void CancelConnect(object o) {
            connectCts?.Cancel();
        }

        public async Task Disconnect() {
            if (SafetyMonitorInfo.Connected) {
                if (updateTimer != null) {
                    await updateTimer.Stop();
                }
                safetyMonitor?.Disconnect();
                safetyMonitor = null;
                SafetyMonitorInfo = DeviceInfo.CreateDefaultInstance<SafetyMonitorInfo>();
                BroadcastMonitorInfo();
                Logger.Info("Disconnected Safety Monitor");
            }
        }

        private async Task<bool> DisconnectDiag() {
            var diag = MyMessageBox.MyMessageBox.Show(Locale.Loc.Instance["LblDisconnectSafetyMonitor"], "", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);
            if (diag == System.Windows.MessageBoxResult.OK) {
                await Disconnect();
            }
            return true;
        }

        public SafetyMonitorInfo GetDeviceInfo() {
            return SafetyMonitorInfo;
        }

        private SafetyMonitorInfo safetyMonitorInfo;

        public SafetyMonitorInfo SafetyMonitorInfo {
            get {
                if (safetyMonitorInfo == null) {
                    safetyMonitorInfo = DeviceInfo.CreateDefaultInstance<SafetyMonitorInfo>();
                }
                return safetyMonitorInfo;
            }
            set {
                safetyMonitorInfo = value;
                RaisePropertyChanged();
            }
        }

        public IAsyncCommand ConnectCommand { get; private set; }
        public ICommand CancelConnectCommand { get; private set; }
        public ICommand DisconnectCommand { get; private set; }
        public ICommand RefreshMonitorListCommand { get; private set; }
    }
}