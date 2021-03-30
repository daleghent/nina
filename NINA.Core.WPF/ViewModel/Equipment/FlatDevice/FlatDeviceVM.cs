#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Locale;
using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Model.MyFlatDevice;
using NINA.Profile;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel.Equipment.FlatDevice {

    public class FlatDeviceVM : DockableVM, IFlatDeviceVM, ICameraConsumer {
        private IFlatDevice flatDevice;
        private IFlatDeviceSettings flatDeviceSettings;
        private readonly IApplicationStatusMediator applicationStatusMediator;
        private readonly IFlatDeviceMediator flatDeviceMediator;
        private readonly DeviceUpdateTimer updateTimer;
        private readonly ICameraMediator cameraMediator;

        public IDeviceChooserVM FlatDeviceChooserVM { get; }

        public FlatDeviceVM(IProfileService profileService, IFlatDeviceMediator flatDeviceMediator, IApplicationStatusMediator applicationStatusMediator,
            ICameraMediator cameraMediator, IDeviceChooserVM flatDeviceChooserVm, IImageGeometryProvider imageGeometryProvider) : base(profileService) {
            this.applicationStatusMediator = applicationStatusMediator;
            this.flatDeviceMediator = flatDeviceMediator;
            this.flatDeviceMediator.RegisterHandler(this);
            this.cameraMediator = cameraMediator;
            this.cameraMediator.RegisterConsumer(this);
            FlatDeviceChooserVM = flatDeviceChooserVm;
            Task.Run(() => FlatDeviceChooserVM.GetEquipment());

            Title = "LblFlatDevice";
            ImageGeometry = imageGeometryProvider.GetImageGeometry("LightBulbSVG");

            ConnectCommand = new AsyncCommand<bool>(Connect);
            CancelConnectCommand = new RelayCommand(CancelConnectFlatDevice);
            DisconnectCommand = new AsyncCommand<bool>(DisconnectFlatDeviceDialog);
            OpenCoverCommand = new AsyncCommand<bool>(() => OpenCover(CancellationToken.None));
            CloseCoverCommand = new AsyncCommand<bool>(() => CloseCover(CancellationToken.None));
            RefreshFlatDeviceListCommand =
                new RelayCommand(RefreshFlatDeviceList, o => flatDevice?.Connected != true);
            SetBrightnessCommand = new AsyncCommand<bool>(o => SetBrightness(o, CancellationToken.None));
            ToggleLightCommand = new AsyncCommand<bool>(o => ToggleLight(o, CancellationToken.None));
            AddGainCommand = new RelayCommand(AddGain);
            AddBinningCommand = new RelayCommand(AddBinning);
            DeleteGainCommand = new RelayCommand(DeleteGainDialog);
            DeleteBinningCommand = new RelayCommand(DeleteBinningDialog);

            updateTimer = new DeviceUpdateTimer(
                GetFlatDeviceValues,
                UpdateFlatDeviceValues,
                profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval
            );

            flatDeviceSettings = profileService.ActiveProfile.FlatDeviceSettings;
            flatDeviceSettings.PropertyChanged += FlatDeviceSettingsChanged;
            profileService.ProfileChanged += ProfileChanged;
            profileService.ActiveProfile.FilterWheelSettings.PropertyChanged += FilterWheelSettingsChanged;
            UpdateWizardValueBlocks();
        }

        private void FlatDeviceSettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            RaisePropertyChanged(nameof(Gains));
            RaisePropertyChanged(nameof(BinningModes));
            UpdateWizardValueBlocks();
        }

        private void FilterWheelSettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            UpdateWizardValueBlocks();
        }

        private void ProfileChanged(object sender, EventArgs e) {
            flatDeviceSettings.PropertyChanged -= FlatDeviceSettingsChanged;
            profileService.ActiveProfile.FilterWheelSettings.PropertyChanged += FilterWheelSettingsChanged;
            RefreshFlatDeviceList(null);
            flatDeviceSettings = profileService.ActiveProfile.FlatDeviceSettings;
            flatDeviceSettings.PropertyChanged += FlatDeviceSettingsChanged;
            RaisePropertyChanged(nameof(Gains));
            RaisePropertyChanged(nameof(BinningModes));
            UpdateWizardValueBlocks();
        }

        private void BroadcastFlatDeviceInfo() {
            flatDeviceMediator.Broadcast(GetDeviceInfo());
        }

        private double brightness;

        public double Brightness {
            get => brightness;
            set { brightness = value; RaisePropertyChanged(); }
        }

        private bool lightOn;

        public bool LightOn {
            get => lightOn;
            set { lightOn = value; RaisePropertyChanged(); }
        }

        public Task<bool> SetBrightness(double value, CancellationToken token) {
            return SetBrightness((object)(value * 100d), token);
        }

        public Task<bool> SetBrightness(object o, CancellationToken token) {
            if (flatDevice == null || !flatDevice.Connected) return Task.FromResult(false);
            if (!double.TryParse(o.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var result)) return Task.FromResult(false);
            return Task.Run(async () => {
                flatDevice.Brightness = result / 100d;
                await Utility.Utility.Delay(profileService.ActiveProfile.FlatDeviceSettings.SettleTime, token);
                return true;
            }, token);
        }

        private readonly SemaphoreSlim ssConnect = new SemaphoreSlim(1, 1);
        private CancellationTokenSource connectFlatDeviceCts;

        public async Task<bool> Connect() {
            await ssConnect.WaitAsync();
            try {
                await Disconnect();
                if (updateTimer != null) {
                    await updateTimer.Stop();
                }

                var device = FlatDeviceChooserVM.SelectedDevice;
                if (device == null) return false;
                if (device.Id == "No_Device") {
                    profileService.ActiveProfile.FlatDeviceSettings.Id = FlatDeviceChooserVM.SelectedDevice.Id;
                    return false;
                }

                applicationStatusMediator.StatusUpdate(
                    new ApplicationStatus {
                        Source = Title,
                        Status = Loc.Instance["LblConnecting"]
                    }
                );
                var newDevice = (IFlatDevice)device;
                connectFlatDeviceCts?.Dispose();
                connectFlatDeviceCts = new CancellationTokenSource();
                try {
                    var connected = await newDevice.Connect(connectFlatDeviceCts.Token);
                    connectFlatDeviceCts.Token.ThrowIfCancellationRequested();
                    if (connected) {
                        this.flatDevice = newDevice;
                        FlatDeviceInfo = new FlatDeviceInfo {
                            MinBrightness = newDevice.MinBrightness,
                            MaxBrightness = newDevice.MaxBrightness,
                            Brightness = newDevice.Brightness,
                            Connected = newDevice.Connected,
                            CoverState = newDevice.CoverState,
                            Description = newDevice.Description,
                            DriverInfo = newDevice.DriverInfo,
                            DriverVersion = newDevice.DriverVersion,
                            LightOn = newDevice.LightOn,
                            Name = newDevice.Name,
                            SupportsOpenClose = newDevice.SupportsOpenClose
                        };
                        this.Brightness = newDevice.Brightness;

                        Notification.ShowSuccess(Loc.Instance["LblFlatDeviceConnected"]);

                        if (updateTimer != null) {
                            updateTimer.Interval =
                                profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval;
                            updateTimer.Start();
                        }

                        profileService.ActiveProfile.FlatDeviceSettings.Id = newDevice.Id;

                        Logger.Trace(
                            $"Successfully connected flat device. Id: {newDevice.Id} Name: {newDevice.Name} Driver Version: {newDevice.DriverVersion}");

                        return true;
                    } else {
                        FlatDeviceInfo.Connected = false;
                        this.flatDevice = null;
                        return false;
                    }
                } catch (OperationCanceledException) {
                    if (FlatDeviceInfo.Connected) {
                        await Disconnect();
                    }

                    return false;
                } catch (Exception ex) {
                    Logger.Error(ex);
                    return false;
                }
            } finally {
                ssConnect.Release();
                applicationStatusMediator.StatusUpdate(
                    new ApplicationStatus {
                        Source = Title,
                        Status = string.Empty
                    }
                );
            }
        }

        private void CancelConnectFlatDevice(object o) {
            connectFlatDeviceCts?.Cancel();
        }

        public async Task Disconnect() {
            if (!FlatDeviceInfo.Connected) return;
            if (updateTimer != null) {
                await updateTimer.Stop();
            }
            flatDevice?.Disconnect();
            flatDevice = null;
            FlatDeviceInfo = DeviceInfo.CreateDefaultInstance<FlatDeviceInfo>();
            BroadcastFlatDeviceInfo();
            Logger.Trace("Disconnected Flat Device");
        }

        private async Task<bool> DisconnectFlatDeviceDialog() {
            var dialog = MyMessageBox.MyMessageBox.Show(Loc.Instance["LblFlatDeviceDisconnectQuestion"],
                "", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);
            if (dialog == System.Windows.MessageBoxResult.OK) {
                await Disconnect();
            }
            return true;
        }

        private void AddGain(object o) {
            if (!(o is string)) return;
            if (!int.TryParse((string)o, out var gain)) return;
            if (Gains.Contains(gain)) return;
            var binning = profileService.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoBinnings()
                ?.OrderBy(mode => mode?.Name ?? "").FirstOrDefault();
            var filters = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
            if (filters.Count == 0) filters = new ObserveAllCollection<FilterInfo> { new FilterInfo() };

            var key = new FlatDeviceFilterSettingsKey(filters.FirstOrDefault()?.Position, binning, gain);
            var value = new FlatDeviceFilterSettingsValue(1d, 1d);
            profileService.ActiveProfile.FlatDeviceSettings.AddBrightnessInfo(key, value);
            RaisePropertyChanged(nameof(Gains));
            UpdateWizardValueBlocks();
        }

        private void AddBinning(object binning) {
            if (!(binning is BinningMode)) return;
            var gain = Gains.FirstOrDefault();
            var filters = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
            if (filters.Count == 0) filters = new ObserveAllCollection<FilterInfo> { new FilterInfo() };

            var key = new FlatDeviceFilterSettingsKey(filters.FirstOrDefault()?.Position,
                (BinningMode)binning, gain);
            var value = new FlatDeviceFilterSettingsValue(1d, 1d);
            profileService.ActiveProfile.FlatDeviceSettings.AddBrightnessInfo(key, value);
            RaisePropertyChanged(nameof(BinningModes));
            UpdateWizardValueBlocks();
        }

        private void DeleteGainDialog(object gain) {
            if (!(gain is int)) return;

            var dialog = MyMessageBox.MyMessageBox.Show($"{Loc.Instance["LblFlatDeviceAreYouSureGain"]} {gain}?",
                "", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxResult.No);
            if (dialog != System.Windows.MessageBoxResult.Yes) return;

            profileService.ActiveProfile.FlatDeviceSettings.RemoveGain((int)gain);
            RaisePropertyChanged(nameof(Gains));
            UpdateWizardValueBlocks();
        }

        private void DeleteBinningDialog(object o) {
            if (!(o is string)) return;

            BinningMode binningMode;
            if (o.Equals(Loc.Instance["LblNone"])) {
                binningMode = null;
            } else {
                if (!BinningMode.TryParse((string)o, out binningMode)) return;
            }

            var dialog = MyMessageBox.MyMessageBox.Show($"{Loc.Instance["LblFlatDeviceAreYouSureBinning"]} {binningMode}?",
                "", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxResult.No);
            if (dialog != System.Windows.MessageBoxResult.Yes) return;

            profileService.ActiveProfile.FlatDeviceSettings.RemoveBinning(binningMode);
            RaisePropertyChanged(nameof(BinningModes));
            UpdateWizardValueBlocks();
        }

        private readonly SemaphoreSlim ssOpen = new SemaphoreSlim(1, 1);

        public async Task<bool> OpenCover(CancellationToken token) {
            await ssOpen.WaitAsync(token);
            try {
                var device = FlatDeviceChooserVM.SelectedDevice;
                if (device == null || device.Id == "No_Device") return false;
                var flatD = (IFlatDevice)device;
                if (flatD.Connected == false) return false;
                if (!flatD.SupportsOpenClose) return false;
                var result = await flatD.Open(token);
                await Utility.Utility.Delay(profileService.ActiveProfile.FlatDeviceSettings.SettleTime, token);
                return result;
            } catch (Exception ex) {
                Logger.Error(ex);
                return false;
            } finally {
                ssOpen.Release();
            }
        }

        private readonly SemaphoreSlim ssClose = new SemaphoreSlim(1, 1);

        public async Task<bool> CloseCover(CancellationToken token) {
            await ssClose.WaitAsync(token);
            try {
                var device = FlatDeviceChooserVM.SelectedDevice;
                if (device == null || device.Id == "No_Device") return false;
                var flatD = (IFlatDevice)device;
                if (flatD.Connected == false) return false;
                if (!flatD.SupportsOpenClose) return false;
                var result = await flatD.Close(token);
                await Utility.Utility.Delay(profileService.ActiveProfile.FlatDeviceSettings.SettleTime, token);
                return result;
            } catch (Exception ex) {
                Logger.Error(ex);
                return false;
            } finally {
                ssClose.Release();
            }
        }

        private FlatDeviceInfo flatDeviceInfo;

        public FlatDeviceInfo FlatDeviceInfo {
            get {
                if (flatDeviceInfo != null) return flatDeviceInfo;
                flatDeviceInfo = DeviceInfo.CreateDefaultInstance<FlatDeviceInfo>();
                return flatDeviceInfo;
            }
            set {
                flatDeviceInfo = value;
                RaisePropertyChanged();
            }
        }

        public FlatDeviceInfo GetDeviceInfo() {
            return FlatDeviceInfo;
        }

        private void RefreshFlatDeviceList(object obj) {
            FlatDeviceChooserVM.GetEquipment();
        }

        private void UpdateFlatDeviceValues(Dictionary<string, object> flatDeviceValues) {
            object o = null;
            flatDeviceValues.TryGetValue(nameof(FlatDeviceInfo.Connected), out o);
            flatDeviceInfo.Connected = (bool)(o ?? false);
            flatDeviceValues.TryGetValue(nameof(FlatDeviceInfo.CoverState), out o);
            flatDeviceInfo.CoverState = (CoverState)(o ?? CoverState.Unknown);
            flatDeviceValues.TryGetValue(nameof(FlatDeviceInfo.Brightness), out o);
            flatDeviceInfo.Brightness = (double)(o ?? 0.0);
            flatDeviceValues.TryGetValue(nameof(FlatDeviceInfo.LightOn), out o);
            flatDeviceInfo.LightOn = (bool)(o ?? false);

            BroadcastFlatDeviceInfo();
        }

        private Dictionary<string, object> GetFlatDeviceValues() {
            var flatDeviceValues = new Dictionary<string, object>
            {
                {nameof(FlatDeviceInfo.Connected), flatDevice?.Connected ?? false},
                {nameof(FlatDeviceInfo.CoverState), flatDevice?.CoverState ?? CoverState.Unknown},
                {nameof(FlatDeviceInfo.Brightness), flatDevice?.Brightness ?? 0.0},
                {nameof(FlatDeviceInfo.MinBrightness), flatDevice?.MinBrightness ?? 0},
                {nameof(FlatDeviceInfo.MaxBrightness), flatDevice?.MaxBrightness ?? 0},
                {nameof(FlatDeviceInfo.LightOn), flatDevice?.LightOn ?? false},
                {nameof(FlatDeviceInfo.SupportsOpenClose), flatDevice?.SupportsOpenClose ?? false}
            };
            return flatDeviceValues;
        }

        public Task<bool> ToggleLight(object o, CancellationToken token) {
            if (flatDevice == null || flatDevice.Connected == false) return Task.FromResult(false);
            return Task.Run(async () => {
                flatDevice.LightOn = o is bool b && b;
                await Utility.Utility.Delay(profileService.ActiveProfile.FlatDeviceSettings.SettleTime, token);
                return true;
            }, token);
        }

        public WizardGrid WizardGrid { get; } = new WizardGrid();

        public ObservableCollection<int> Gains =>
                new ObservableCollection<int>(profileService.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoGains()?
                    .OrderBy(g => g)
                    .ToList() ?? new List<int>());

        public ObservableCollection<string> BinningModes =>
                new ObservableCollection<string>(profileService.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoBinnings()
                    ?.OrderBy(mode => mode?.Name ?? "")
                    .Select(mode => mode?.Name ?? Loc.Instance["LblNone"])
                    .ToList() ?? new List<string>());

        private void UpdateWizardValueBlocks() {
            var binningModes = profileService.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoBinnings()
                ?.OrderBy(m => m?.Name ?? "");
            var existingBlocks = WizardGrid.Blocks.ToList();
            if (binningModes == null) {
                WizardGrid.RemoveBlocks(existingBlocks);
                RaisePropertyChanged(nameof(WizardGrid));
                return;
            }

            foreach (var binningMode in binningModes) {
                var block = WizardGrid.Blocks.FirstOrDefault(b => Equals(b?.Binning, binningMode));
                if (block == null) {
                    block = new WizardValueBlock { Binning = binningMode };
                    WizardGrid.AddBlock(block);
                } else {
                    existingBlocks.Remove(block);
                }

                var filters = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
                if (filters.Count == 0) filters = new ObserveAllCollection<FilterInfo> { null };
                var existingColumns = block.Columns.ToList();

                //filter name column
                FindOrCreateColumnAndUpdateRows(block, ref existingColumns, filters, binningMode, 0, -1, true);

                //gain columns
                var newGains =
                profileService.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoGains()?.OrderBy(g => g)
                    .ToList() ?? new List<int>();
                for (var i = 1; i <= newGains.Count; i++) {
                    FindOrCreateColumnAndUpdateRows(block, ref existingColumns, filters, binningMode, i, newGains[i - 1], false);
                }
                block.RemoveColumns(existingColumns);
            }
            WizardGrid.RemoveBlocks(existingBlocks);
            RaisePropertyChanged(nameof(WizardGrid));
        }

        private void FindOrCreateColumnAndUpdateRows(WizardValueBlock block, ref List<WizardGridColumn> existingColumns,
                IEnumerable<FilterInfo> filters, BinningMode binningMode, int newColumnNumber, int gain, bool isFilterNameColumn) {
            var column = isFilterNameColumn
                ? block.Columns.FirstOrDefault(c => c?.ColumnNumber == 0)
                : block.Columns.FirstOrDefault(c => c?.Gain == gain);
            if (column == null) {
                column = isFilterNameColumn
                    ? new WizardGridColumn { ColumnNumber = 0, Header = $"{Loc.Instance["LblFilter"]}", Gain = -9000 } //do not use -1, as that is used for simulator cam etc.
                    : new WizardGridColumn { ColumnNumber = newColumnNumber, Header = null, Gain = gain };
                block.AddColumn(column);
            } else {
                column.ColumnNumber = newColumnNumber;
                column.RaiseChanged();
                existingColumns.Remove(column);
            }
            var existingFilterKeys = column.Settings?.Select(s => s.Key).ToList();
            var newFilterKeys = filters.Select(f => new FlatDeviceFilterSettingsKey(f?.Position, binningMode, isFilterNameColumn ? 0 : gain))
                .ToList();

            foreach (var key in newFilterKeys) {
                var timing = column.Settings?.FirstOrDefault(s => Equals(s.Key, key));
                if (timing == null) {
                    timing = new FilterTiming(profileService, key, isFilterNameColumn);
                    column.AddFilterTiming(timing);
                } else {
                    timing.RaiseChanged();
                    existingFilterKeys?.Remove(key);
                }
            }
            column.RemoveFilterTimingByKeys(existingFilterKeys);
        }

        public ICommand RefreshFlatDeviceListCommand { get; }
        public IAsyncCommand ConnectCommand { get; }
        public ICommand CancelConnectCommand { get; }
        public IAsyncCommand DisconnectCommand { get; }
        public IAsyncCommand OpenCoverCommand { get; }
        public IAsyncCommand CloseCoverCommand { get; }
        public ICommand ToggleLightCommand { get; }
        public ICommand SetBrightnessCommand { get; }
        public ICommand AddGainCommand { get; }
        public ICommand AddBinningCommand { get; }
        public ICommand DeleteGainCommand { get; }
        public ICommand DeleteBinningCommand { get; }

        public void Dispose() {
            cameraMediator.RemoveConsumer(this);
        }

        private CameraInfo cameraInfo;

        public CameraInfo CameraInfo {
            get => cameraInfo;
            set {
                cameraInfo = value;
                RaisePropertyChanged();
            }
        }

        public void UpdateDeviceInfo(CameraInfo deviceInfo) {
            CameraInfo = deviceInfo;
        }
    }
}