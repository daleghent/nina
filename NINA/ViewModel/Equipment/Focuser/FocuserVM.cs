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
using NINA.Model.MyFocuser;
using NINA.Profile;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NINA.Locale;

namespace NINA.ViewModel.Equipment.Focuser {

    internal class FocuserVM : DockableVM, IFocuserVM {
        private readonly DeviceUpdateTimer updateTimer;
        private readonly IFocuserMediator focuserMediator;
        private readonly IApplicationStatusMediator applicationStatusMediator;
        private readonly IProgress<ApplicationStatus> progress;

        public FocuserVM(IProfileService profileService, IFocuserMediator focuserMediator, IApplicationStatusMediator applicationStatusMediator, IDeviceChooserVM focuserChooserVm, IImageGeometryProvider imageGeometryProvider) : base(profileService) {
            Title = "LblFocuser";
            ImageGeometry = imageGeometryProvider.GetImageGeometry("FocusSVG");

            this.focuserMediator = focuserMediator;
            this.focuserMediator.RegisterHandler(this);
            this.applicationStatusMediator = applicationStatusMediator;
            FocuserChooserVM = focuserChooserVm;
            FocuserChooserVM.GetEquipment();

            ChooseFocuserCommand = new AsyncCommand<bool>(() => ChooseFocuser());
            CancelChooseFocuserCommand = new RelayCommand(CancelChooseFocuser);
            DisconnectCommand = new AsyncCommand<bool>(() => DisconnectDiag());
            RefreshFocuserListCommand = new RelayCommand(RefreshFocuserList, o => Focuser?.Connected != true);
            MoveFocuserInSmallCommand = new AsyncCommand<int>(() => MoveFocuserRelativeInternal((int)Math.Round(profileService.ActiveProfile.FocuserSettings.AutoFocusStepSize / -2d)), (p) => FocuserInfo.Connected && !FocuserInfo.IsMoving);
            MoveFocuserInLargeCommand = new AsyncCommand<int>(() => MoveFocuserRelativeInternal(profileService.ActiveProfile.FocuserSettings.AutoFocusStepSize * -5), (p) => FocuserInfo.Connected && !FocuserInfo.IsMoving);
            MoveFocuserOutSmallCommand = new AsyncCommand<int>(() => MoveFocuserRelativeInternal((int)Math.Round(profileService.ActiveProfile.FocuserSettings.AutoFocusStepSize / 2d)), (p) => FocuserInfo.Connected && !FocuserInfo.IsMoving);
            MoveFocuserOutLargeCommand = new AsyncCommand<int>(() => MoveFocuserRelativeInternal(profileService.ActiveProfile.FocuserSettings.AutoFocusStepSize * 5), (p) => FocuserInfo.Connected && !FocuserInfo.IsMoving);
            MoveFocuserCommand = new AsyncCommand<int>(() => MoveFocuserInternal(TargetPosition), (p) => FocuserInfo.Connected && !FocuserInfo.IsMoving);
            HaltFocuserCommand = new RelayCommand((object o) => moveCts?.Cancel());
            ToggleTempCompCommand = new RelayCommand(ToggleTempComp);

            updateTimer = new DeviceUpdateTimer(
                GetFocuserValues,
                UpdateFocuserValues,
                profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval
            );

            profileService.ProfileChanged += (object sender, EventArgs e) => {
                RefreshFocuserList(null);
            };

            progress = new Progress<ApplicationStatus>(p => {
                p.Source = this.Title;
                this.applicationStatusMediator.StatusUpdate(p);
            });
        }

        private void ToggleTempComp(object obj) {
            if (FocuserInfo.Connected) {
                Focuser.TempComp = (bool)obj;
            }
        }

        public void ToggleTempComp(bool tempComp) {
            if (FocuserInfo.Connected) {
                Focuser.TempComp = tempComp;
            }
        }

        private void HaltFocuser() {
            Logger.Info("Halting Focuser");
            if (Focuser?.Connected != true) return;
            try {
                Focuser.Halt();
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        private CancellationTokenSource moveCts;

        private Task<int> MoveFocuserInternal(int position) {
            moveCts?.Dispose();
            moveCts = new CancellationTokenSource();
            return MoveFocuser(position, moveCts.Token);
        }

        private Task<int> MoveFocuserRelativeInternal(int position) {
            moveCts?.Dispose();
            moveCts = new CancellationTokenSource();
            return MoveFocuserRelative(position, moveCts.Token);
        }

        public async Task<int> MoveFocuser(int position, CancellationToken ct) {
            var pos = -1;

            await Task.Run(async () => {
                try {
                    using (ct.Register(() => HaltFocuser())) {
                        var tempComp = false;
                        if (Focuser.TempCompAvailable && Focuser.TempComp) {
                            tempComp = true;
                            ToggleTempComp(false);
                        }

                        Logger.Info($"Moving Focuser to position {position}");
                        progress.Report(new ApplicationStatus() { Status = string.Format(Loc.Instance["LblFocuserMoveToPosition"], position) });

                        while (Focuser.Position != position) {
                            FocuserInfo.IsMoving = true;
                            ct.ThrowIfCancellationRequested();
                            await Focuser.Move(position, ct);
                        }

                        FocuserInfo.Position = this.Position;
                        pos = this.Position;
                        ToggleTempComp(tempComp);
                        BroadcastFocuserInfo();

                        //Wait for focuser to settle
                        if (profileService.ActiveProfile.FocuserSettings.FocuserSettleTime > 0) {
                            FocuserInfo.IsSettling = true;
                            await Utility.Utility.Wait(TimeSpan.FromSeconds(profileService.ActiveProfile.FocuserSettings.FocuserSettleTime), ct, progress, Loc.Instance["LblSettle"]);
                        }
                    }
                } catch (OperationCanceledException) {
                    Logger.Info("Focuser move cancelled");
                } finally {
                    FocuserInfo.IsSettling = false;
                    FocuserInfo.IsMoving = false;
                    progress.Report(new ApplicationStatus() { Status = string.Empty });
                }
            }, ct);
            return pos;
        }

        public async Task<int> MoveFocuserRelative(int offset, CancellationToken ct) {
            if (Focuser?.Connected != true) return -1;
            var pos = Position + offset;
            pos = await MoveFocuser(pos, ct);
            return pos;
        }

        private CancellationTokenSource cancelChooseFocuserCts;

        private readonly SemaphoreSlim ss = new SemaphoreSlim(1, 1);

        private static IFocuser GetBacklashCompensationFocuser(IProfileService profileService, IFocuser focuser) {
            switch (profileService.ActiveProfile.FocuserSettings.BacklashCompensationModel) {
                case Utility.Enum.BacklashCompensationModel.ABSOLUTE:
                    return new AbsoluteBacklashCompensationDecorator(profileService, focuser);

                case Utility.Enum.BacklashCompensationModel.OVERSHOOT:
                    return new OvershootBacklashCompensationDecorator(profileService, focuser);

                default:
                    return focuser;
            }
        }

        private async Task<bool> ChooseFocuser() {
            await ss.WaitAsync();
            try {
                await Disconnect();

                if (FocuserChooserVM.SelectedDevice == null) return false;

                if (FocuserChooserVM.SelectedDevice.Id == "No_Device") {
                    profileService.ActiveProfile.FocuserSettings.Id = FocuserChooserVM.SelectedDevice.Id;
                    return false;
                }

                progress.Report(new ApplicationStatus { Status = Loc.Instance["LblConnecting"] });

                var newFocuser = GetBacklashCompensationFocuser(profileService, (IFocuser)FocuserChooserVM.SelectedDevice);
                cancelChooseFocuserCts?.Dispose();
                cancelChooseFocuserCts = new CancellationTokenSource();
                try {
                    var connected = await newFocuser.Connect(cancelChooseFocuserCts.Token);
                    cancelChooseFocuserCts.Token.ThrowIfCancellationRequested();
                    if (connected) {
                        Focuser = newFocuser;

                        FocuserInfo = new FocuserInfo {
                            Connected = true,
                            IsMoving = Focuser.IsMoving,
                            Name = Focuser.Name,
                            Position = Position,
                            StepSize = Focuser.StepSize,
                            TempCompAvailable = Focuser.TempCompAvailable,
                            TempComp = Focuser.TempComp,
                            Temperature = Focuser.Temperature
                        };

                        Notification.ShowSuccess(Loc.Instance["LblFocuserConnected"]);

                        updateTimer.Interval = profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval;
                        updateTimer.Start();

                        TargetPosition = Position;
                        profileService.ActiveProfile.FocuserSettings.Id = Focuser.Id;

                        Logger.Info($"Successfully connected Focuser. Id: {Focuser.Id} Name: {Focuser.Name} Driver Version: {Focuser.DriverVersion}");

                        return true;
                    } else {
                        FocuserInfo.Connected = false;
                        Focuser = null;
                        return false;
                    }
                } catch (OperationCanceledException) {
                    if (FocuserInfo.Connected) { await Disconnect(); }
                    return false;
                }
            } finally {
                ss.Release();
                progress.Report(new ApplicationStatus { Status = string.Empty });
            }
        }

        private void CancelChooseFocuser(object o) {
            cancelChooseFocuserCts?.Cancel();
        }

        private Dictionary<string, object> GetFocuserValues() {
            var focuserValues = new Dictionary<string, object>
            {
                {nameof(FocuserInfo.Connected), focuser?.Connected ?? false},
                {nameof(FocuserInfo.Position), Position},
                {nameof(FocuserInfo.Temperature), focuser?.Temperature ?? double.NaN},
                {nameof(FocuserInfo.IsMoving), focuser?.IsMoving ?? false},
                {nameof(FocuserInfo.TempComp), focuser?.TempComp ?? false}
            };
            return focuserValues;
        }

        private void UpdateFocuserValues(Dictionary<string, object> focuserValues) {
            focuserValues.TryGetValue(nameof(FocuserInfo.Connected), out var o);
            FocuserInfo.Connected = (bool)(o ?? false);

            focuserValues.TryGetValue(nameof(FocuserInfo.Position), out o);
            FocuserInfo.Position = (int)(o ?? 0);

            focuserValues.TryGetValue(nameof(FocuserInfo.Temperature), out o);
            FocuserInfo.Temperature = (double)(o ?? double.NaN);

            focuserValues.TryGetValue(nameof(FocuserInfo.IsMoving), out o);
            FocuserInfo.IsMoving = (bool)(o ?? false);

            focuserValues.TryGetValue(nameof(FocuserInfo.TempComp), out o);
            FocuserInfo.TempComp = (bool)(o ?? false);

            BroadcastFocuserInfo();
        }

        private FocuserInfo focuserInfo;

        public FocuserInfo FocuserInfo {
            get => focuserInfo ?? (focuserInfo = DeviceInfo.CreateDefaultInstance<FocuserInfo>());
            set {
                focuserInfo = value;
                RaisePropertyChanged();
            }
        }

        private void BroadcastFocuserInfo() {
            focuserMediator.Broadcast(FocuserInfo);
        }

        private int targetPosition;

        public int TargetPosition {
            get => targetPosition;
            set {
                targetPosition = value;
                RaisePropertyChanged();
            }
        }

        public int Position => Focuser?.Position ?? 0;

        private async Task<bool> DisconnectDiag() {
            var diag = MyMessageBox.MyMessageBox.Show(Locale.Loc.Instance["LblDisconnectFocuser"], "", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);
            if (diag == System.Windows.MessageBoxResult.OK) {
                await Disconnect();
            }
            return true;
        }

        public async Task Disconnect() {
            await updateTimer.Stop();
            Focuser?.Disconnect();
            Focuser = null;
            FocuserInfo = DeviceInfo.CreateDefaultInstance<FocuserInfo>();
            BroadcastFocuserInfo();
            RaisePropertyChanged(nameof(Focuser));
            Logger.Info("Disconnected Focuser");
        }

        public void RefreshFocuserList(object obj) {
            FocuserChooserVM.GetEquipment();
        }

        public Task<bool> Connect() {
            return ChooseFocuser();
        }

        public FocuserInfo GetDeviceInfo() {
            return FocuserInfo;
        }

        private IFocuser focuser;

        public IFocuser Focuser {
            get => focuser;
            private set {
                focuser = value;
                RaisePropertyChanged();
            }
        }

        public IDeviceChooserVM FocuserChooserVM { get; }
        public ICommand RefreshFocuserListCommand { get; }
        public IAsyncCommand ChooseFocuserCommand { get; }
        public ICommand CancelChooseFocuserCommand { get; }
        public ICommand DisconnectCommand { get; }
        public ICommand MoveFocuserCommand { get; }
        public ICommand MoveFocuserInSmallCommand { get; }
        public ICommand MoveFocuserInLargeCommand { get; }
        public ICommand MoveFocuserOutSmallCommand { get; }
        public ICommand MoveFocuserOutLargeCommand { get; }
        public ICommand HaltFocuserCommand { get; }
        public ICommand ToggleTempCompCommand { get; }
    }
}