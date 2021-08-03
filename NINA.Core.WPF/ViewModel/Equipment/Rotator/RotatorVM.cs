#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Equipment.MyRotator;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Core.Utility.Notification;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NINA.Astrometry;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Core.Model;
using NINA.Core.Locale;
using NINA.Core.MyMessageBox;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Equipment.Equipment;
using NINA.Equipment.Interfaces;

namespace NINA.WPF.Base.ViewModel.Equipment.Rotator {

    public class RotatorVM : DockableVM, IRotatorVM {

        public RotatorVM(
            IProfileService profileService,
            IRotatorMediator rotatorMediator,
            IDeviceChooserVM rotatorChooserVM,
            IApplicationResourceDictionary resourceDictionary,
            IApplicationStatusMediator applicationStatusMediator) : base(profileService) {
            Title = Loc.Instance["LblRotator"];
            ImageGeometry = (System.Windows.Media.GeometryGroup)resourceDictionary["RotatorSVG"];

            this.rotatorMediator = rotatorMediator;
            this.rotatorMediator.RegisterHandler(this);
            this.applicationStatusMediator = applicationStatusMediator;
            RotatorChooserVM = rotatorChooserVM;
            Task.Run(() => RotatorChooserVM.GetEquipment());

            ConnectCommand = new AsyncCommand<bool>(() => Connect());
            CancelConnectCommand = new RelayCommand(CancelConnectRotator);
            DisconnectCommand = new AsyncCommand<bool>(() => DisconnectDiag());
            RefreshRotatorListCommand = new RelayCommand(RefreshRotatorList, o => !(rotator?.Connected == true));
            MoveCommand = new AsyncCommand<float>(() => Move(TargetPosition), (p) => RotatorInfo.Connected && RotatorInfo.Synced);
            MoveMechanicalCommand = new AsyncCommand<float>(() => MoveMechanical(TargetPosition), (p) => RotatorInfo.Connected);
            HaltCommand = new RelayCommand(Halt);
            ReverseCommand = new RelayCommand(Reverse);

            updateTimer = new DeviceUpdateTimer(
                GetRotatorValues,
                UpdateRotatorValues,
                profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval
            );

            profileService.ProfileChanged += (object sender, EventArgs e) => {
                RefreshRotatorList(null);
            };
        }

        private void Reverse(object obj) {
            var reverse = (bool)obj;
            rotator.Reverse = reverse;
            profileService.ActiveProfile.RotatorSettings.Reverse = reverse;
        }

        private void Halt(object obj) {
            _moveCts?.Cancel();
            rotator?.Halt();
        }

        public void Sync(float skyAngle) {
            if (RotatorInfo.Connected) {
                Logger.Info($"Syncing Rotator to Sky Angle {skyAngle}°");
                rotator.Sync(skyAngle);
                RotatorInfo.Position = rotator.Position;
                RotatorInfo.Synced = true;
                BroadcastRotatorInfo();
            }
        }

        public async Task<float> Move(float requestedPosition) {
            _moveCts?.Dispose();
            _moveCts = new CancellationTokenSource();
            float pos = float.NaN;
            await Task.Run(async () => {
                try {
                    RotatorInfo.IsMoving = true;

                    var adjustedTargetPosition = GetTargetPosition(requestedPosition);
                    if (Math.Abs(adjustedTargetPosition - requestedPosition) > 0.1) {
                        Logger.Info($"Adjusted rotator target to {adjustedTargetPosition}");
                        Notification.ShowInformation(String.Format(Loc.Instance["LblRotatorRangeAdjusted"], adjustedTargetPosition));
                    }

                    applicationStatusMediator.StatusUpdate(
                        new ApplicationStatus() {
                            Source = Title,
                            Status = string.Format(Loc.Instance["LblMovingRotatorToPosition"], Math.Round(adjustedTargetPosition, 2))
                        }
                    );

                    Logger.Debug($"Move rotator to {adjustedTargetPosition}°");

                    rotator.MoveAbsolute(adjustedTargetPosition);
                    while (RotatorInfo.IsMoving || ((Math.Abs(RotatorInfo.Position - adjustedTargetPosition) > 1) && (Math.Abs(RotatorInfo.Position - adjustedTargetPosition) < 359))) {
                        _moveCts.Token.ThrowIfCancellationRequested();
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        Logger.Trace($"Waiting for rotator to reach destination. IsMoving: {RotatorInfo.IsMoving} - Current Position {RotatorInfo.Position} - Target Position {adjustedTargetPosition}");
                    }
                    RotatorInfo.Position = adjustedTargetPosition;
                    pos = adjustedTargetPosition;
                    BroadcastRotatorInfo();
                } catch (OperationCanceledException) {
                } finally {
                    applicationStatusMediator.StatusUpdate(
                        new ApplicationStatus() {
                            Source = Title,
                            Status = string.Empty
                        }
                    );
                }
            });
            return pos;
        }

        public async Task<float> MoveMechanical(float requestedPosition) {
            return await MoveMechanical(requestedPosition, TimeSpan.FromSeconds(1));
        }

        public async Task<float> MoveMechanical(float requestedPosition, TimeSpan waitTime) {
            _moveCts?.Dispose();
            _moveCts = new CancellationTokenSource();
            float pos = float.NaN;
            await Task.Run(async () => {
                try {
                    RotatorInfo.IsMoving = true;

                    var adjustedTargetPosition = GetTargetMechanicalPosition(requestedPosition);
                    if (Math.Abs(adjustedTargetPosition - requestedPosition) > 0.1) {
                        Logger.Info($"Adjusted rotator mechanical target to {adjustedTargetPosition}");
                        Notification.ShowInformation(String.Format(Loc.Instance["LblRotatorRangeAdjusted"], adjustedTargetPosition));
                    }

                    applicationStatusMediator.StatusUpdate(
                        new ApplicationStatus() {
                            Source = Title,
                            Status = string.Format(Loc.Instance["LblMovingRotatorToMechanicalPosition"], Math.Round(adjustedTargetPosition, 2))
                        }
                    );

                    Logger.Debug($"Move rotator mechanical to {adjustedTargetPosition}°");
                    rotator.MoveAbsoluteMechanical(adjustedTargetPosition);
                    while (RotatorInfo.IsMoving || ((Math.Abs(RotatorInfo.MechanicalPosition - adjustedTargetPosition) > 1) && (Math.Abs(RotatorInfo.MechanicalPosition - adjustedTargetPosition) < 359))) {
                        _moveCts.Token.ThrowIfCancellationRequested();
                        await Task.Delay(waitTime);
                        Logger.Trace($"Waiting for rotator to reach destination. IsMoving: {RotatorInfo.IsMoving} - Current Position {RotatorInfo.MechanicalPosition} - Target Position {adjustedTargetPosition}");
                    }
                    RotatorInfo.Position = adjustedTargetPosition;
                    pos = adjustedTargetPosition;
                    BroadcastRotatorInfo();
                } catch (OperationCanceledException) {
                } finally {
                    applicationStatusMediator.StatusUpdate(
                        new ApplicationStatus() {
                            Source = Title,
                            Status = string.Empty
                        }
                    );
                }
            });
            return pos;
        }

        public async Task<float> MoveRelative(float offset) {
            return await MoveRelative(offset, TimeSpan.FromSeconds(1));
        }

        public async Task<float> MoveRelative(float offset, TimeSpan waitTime) {
            if (rotator?.Connected == true) {
                return await MoveMechanical(rotator.MechanicalPosition + offset, waitTime);
            }
            return -1;
        }

        private void UpdateRotatorValues(Dictionary<string, object> rotatorValues) {
            object o = null;
            rotatorValues.TryGetValue(nameof(RotatorInfo.Connected), out o);
            RotatorInfo.Connected = (bool)(o ?? false);

            rotatorValues.TryGetValue(nameof(RotatorInfo.Position), out o);
            RotatorInfo.Position = (float)(o ?? 0f);

            rotatorValues.TryGetValue(nameof(RotatorInfo.StepSize), out o);
            RotatorInfo.StepSize = (float)(o ?? 0f);

            rotatorValues.TryGetValue(nameof(RotatorInfo.IsMoving), out o);
            RotatorInfo.IsMoving = (bool)(o ?? false);

            rotatorValues.TryGetValue(nameof(RotatorInfo.Reverse), out o);
            RotatorInfo.Reverse = (bool)(o ?? false);

            rotatorValues.TryGetValue(nameof(RotatorInfo.Synced), out o);
            RotatorInfo.Synced = (bool)(o ?? false);

            rotatorValues.TryGetValue(nameof(RotatorInfo.MechanicalPosition), out o);
            RotatorInfo.MechanicalPosition = (float)(o ?? 0f);

            BroadcastRotatorInfo();
        }

        private Dictionary<string, object> GetRotatorValues() {
            Dictionary<string, object> rotatorValues = new Dictionary<string, object>();
            rotatorValues.Add(nameof(RotatorInfo.Connected), rotator?.Connected ?? false);
            rotatorValues.Add(nameof(RotatorInfo.Position), rotator?.Position ?? 0);
            rotatorValues.Add(nameof(RotatorInfo.IsMoving), rotator?.IsMoving ?? false);
            rotatorValues.Add(nameof(RotatorInfo.StepSize), rotator?.StepSize ?? 0);
            rotatorValues.Add(nameof(RotatorInfo.Reverse), rotator?.Reverse ?? false);
            rotatorValues.Add(nameof(RotatorInfo.Synced), rotator?.Synced ?? false);
            rotatorValues.Add(nameof(RotatorInfo.MechanicalPosition), rotator?.MechanicalPosition ?? 0f);

            return rotatorValues;
        }

        private DeviceUpdateTimer updateTimer;
        private RotatorInfo rotatorInfo;

        public RotatorInfo RotatorInfo {
            get {
                if (rotatorInfo == null) {
                    rotatorInfo = DeviceInfo.CreateDefaultInstance<RotatorInfo>();
                }
                return rotatorInfo;
            }
            set {
                rotatorInfo = value;
                RaisePropertyChanged();
            }
        }

        public RotatorInfo GetDeviceInfo() {
            return RotatorInfo;
        }

        public void RefreshRotatorList(object obj) {
            RotatorChooserVM.GetEquipment();
        }

        public IDeviceChooserVM RotatorChooserVM { get; private set; }

        private float targetPosition;

        public float TargetPosition {
            get { return targetPosition; }
            set { targetPosition = value; RaisePropertyChanged(); }
        }

        private IRotator rotator;
        private IRotatorMediator rotatorMediator;
        private IApplicationStatusMediator applicationStatusMediator;

        public IAsyncCommand ConnectCommand { get; private set; }
        public ICommand CancelConnectCommand { get; private set; }
        public ICommand DisconnectCommand { get; private set; }
        public ICommand RefreshRotatorListCommand { get; private set; }
        public IAsyncCommand MoveCommand { get; private set; }
        public IAsyncCommand MoveMechanicalCommand { get; private set; }
        public ICommand HaltCommand { get; private set; }
        public ICommand ReverseCommand { get; private set; }

        private CancellationTokenSource _connectRotatorCts;
        private CancellationTokenSource _moveCts;
        private readonly SemaphoreSlim ss = new SemaphoreSlim(1, 1);

        public async Task<bool> Connect() {
            await ss.WaitAsync();
            try {
                await Disconnect();
                if (updateTimer != null) {
                    await updateTimer.Stop();
                }

                if (RotatorChooserVM.SelectedDevice.Id == "No_Device") {
                    profileService.ActiveProfile.RotatorSettings.Id = RotatorChooserVM.SelectedDevice.Id;
                    return false;
                }

                applicationStatusMediator.StatusUpdate(
                    new ApplicationStatus() {
                        Source = Title,
                        Status = Loc.Instance["LblConnecting"]
                    }
                );

                var rotator = (IRotator)RotatorChooserVM.SelectedDevice;
                _connectRotatorCts?.Dispose();
                _connectRotatorCts = new CancellationTokenSource();
                if (rotator != null) {
                    try {
                        var connected = await rotator?.Connect(_connectRotatorCts.Token);
                        _connectRotatorCts.Token.ThrowIfCancellationRequested();
                        if (connected) {
                            this.rotator = rotator;

                            if (this.rotator.CanReverse) {
                                this.rotator.Reverse = profileService.ActiveProfile.RotatorSettings.Reverse;
                            }

                            RotatorInfo = new RotatorInfo {
                                Connected = true,
                                IsMoving = rotator.IsMoving,
                                Name = rotator.Name,
                                Description = rotator.Description,
                                Position = rotator.Position,
                                StepSize = rotator.StepSize,
                                DriverInfo = rotator.DriverInfo,
                                DriverVersion = rotator.DriverVersion,
                                CanReverse = rotator.CanReverse,
                                Reverse = rotator.Reverse
                            };

                            Notification.ShowSuccess(Loc.Instance["LblRotatorConnected"]);

                            updateTimer.Interval = profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval;
                            updateTimer.Start();

                            TargetPosition = rotator.Position;
                            profileService.ActiveProfile.RotatorSettings.Id = rotator.Id;
                            profileService.ActiveProfile.RotatorSettings.Reverse = this.rotator.Reverse;

                            Logger.Info($"Successfully connected Rotator. Id: {rotator.Id} Name: {rotator.Name} Driver Version: {rotator.DriverVersion}");

                            return true;
                        } else {
                            RotatorInfo.Connected = false;
                            this.rotator = null;
                            return false;
                        }
                    } catch (OperationCanceledException) {
                        if (RotatorInfo.Connected) { await Disconnect(); }
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

        private void CancelConnectRotator(object o) {
            _connectRotatorCts?.Cancel();
        }

        public async Task Disconnect() {
            if (RotatorInfo.Connected) {
                if (updateTimer != null) {
                    await updateTimer.Stop();
                }
                rotator?.Disconnect();
                rotator = null;
                RotatorInfo = DeviceInfo.CreateDefaultInstance<RotatorInfo>();
                BroadcastRotatorInfo();
                Logger.Info("Disconnected Rotator");
            }
        }

        private async Task<bool> DisconnectDiag() {
            var diag = MyMessageBox.Show(Loc.Instance["LblDisconnectRotator"], "", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);
            if (diag == System.Windows.MessageBoxResult.OK) {
                await Disconnect();
            }
            return true;
        }

        private void BroadcastRotatorInfo() {
            rotatorMediator.Broadcast(GetDeviceInfo());
        }

        public float GetTargetPosition(float position) {
            if (!rotator.Synced) {
                // This indicates a code bug from the caller, so this message is not localized
                throw new Exception("Rotator not synced!");
            }

            // Focuser position should be in [0, 360)
            position = AstroUtil.EuclidianModulus(position, 360);
            var offset = rotator.MechanicalPosition - rotator.Position;
            var mechanicalPosition = AstroUtil.EuclidianModulus(position + offset, 360);
            var targetMechanicalPosition = GetTargetMechanicalPosition(mechanicalPosition);
            return AstroUtil.EuclidianModulus(targetMechanicalPosition - offset + 360, 360);
        }

        public float GetTargetMechanicalPosition(float position) {
            // Focuser position should be in [0, 360)
            position = AstroUtil.EuclidianModulus(position, 360);
            var rangeType = profileService.ActiveProfile.RotatorSettings.RangeType;
            var rangeStart = profileService.ActiveProfile.RotatorSettings.RangeStartMechanicalPosition;
            float rangeStartDistance = AstroUtil.EuclidianModulus(position - rangeStart + 360, 360);
            float targetMechanicalPosition;
            if (rangeType == Core.Enum.RotatorRangeTypeEnum.FULL) {
                targetMechanicalPosition = position;
            } else if (rangeType == Core.Enum.RotatorRangeTypeEnum.HALF) {
                if (rangeStartDistance < 180.0) {
                    targetMechanicalPosition = position;
                } else {
                    targetMechanicalPosition = position + 180;
                }
            } else if (rangeType == Core.Enum.RotatorRangeTypeEnum.QUARTER) {
                if (rangeStartDistance < 90.0) {
                    targetMechanicalPosition = position;
                } else if (rangeStartDistance < 180.0) {
                    targetMechanicalPosition = position + 270;
                } else if (rangeStartDistance < 270.0) {
                    targetMechanicalPosition = position + 180;
                } else {
                    targetMechanicalPosition = position + 90;
                }
            } else {
                throw new NotImplementedException();
            }

            return AstroUtil.EuclidianModulus(targetMechanicalPosition, 360);
        }
    }
}