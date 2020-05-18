#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel.Equipment.FlatDevice {

    internal class FlatDeviceVM : DockableVM, IFlatDeviceVM, IFilterWheelConsumer {
        private IFlatDevice _flatDevice;
        private IFlatDeviceSettings _flatDeviceSettings;
        private readonly IApplicationStatusMediator _applicationStatusMediator;
        private readonly IFilterWheelMediator _filterWheelMediator;
        private readonly IFlatDeviceMediator _flatDeviceMediator;
        private readonly DeviceUpdateTimer _updateTimer;

        public FlatDeviceVM(IProfileService profileService, IFlatDeviceMediator flatDeviceMediator, IApplicationStatusMediator applicationStatusMediator, IFilterWheelMediator filterWheelMediator) : base(profileService) {
            _applicationStatusMediator = applicationStatusMediator;
            _filterWheelMediator = filterWheelMediator;
            _filterWheelMediator.RegisterConsumer(this);
            _flatDeviceMediator = flatDeviceMediator;
            _flatDeviceMediator.RegisterHandler(this);

            ConnectCommand = new AsyncCommand<bool>(Connect);
            CancelConnectCommand = new RelayCommand(CancelConnectFlatDevice);
            DisconnectCommand = new AsyncCommand<bool>(() => DisconnectFlatDeviceDialog());
            OpenCoverCommand = new AsyncCommand<bool>(OpenCover);
            CloseCoverCommand = new AsyncCommand<bool>(CloseCover);
            RefreshFlatDeviceListCommand =
                new RelayCommand(RefreshFlatDeviceList, o => _flatDevice?.Connected != true);
            SetBrightnessCommand = new RelayCommand(SetBrightness);
            ToggleLightCommand = new RelayCommand(ToggleLight);
            AddGainCommand = new RelayCommand(AddGain);
            AddBinningCommand = new RelayCommand(AddBinning);
            DeleteGainCommand = new RelayCommand(DeleteGainDialog);
            DeleteBinningCommand = new RelayCommand(DeleteBinningDialog);

            FlatDeviceChooserVM = new FlatDeviceChooserVM(profileService);
            FlatDeviceChooserVM.GetEquipment();

            _updateTimer = new DeviceUpdateTimer(
                GetFlatDeviceValues,
                UpdateFlatDeviceValues,
                profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval
            );

            _flatDeviceSettings = profileService.ActiveProfile.FlatDeviceSettings;
            _flatDeviceSettings.PropertyChanged += SettingsChanged;
            profileService.ProfileChanged += (object sender, EventArgs e) => {
                _flatDeviceSettings.PropertyChanged -= SettingsChanged;
                RefreshFlatDeviceList(null);
                _flatDeviceSettings = profileService.ActiveProfile.FlatDeviceSettings;
                _flatDeviceSettings.PropertyChanged += SettingsChanged;
                _wizardGrid = GetWizardValueBlocks();
                RaiseWizardValuesChanged();
            };
        }

        private void BroadcastFlatDeviceInfo() {
            _flatDeviceMediator.Broadcast(GetDeviceInfo());
        }

        private double _brightness;

        public double Brightness {
            get => _brightness;

            set { _brightness = value; RaisePropertyChanged(); }
        }

        public bool LightOn { get; set; }

        public void SetBrightness(double value) {
            SetBrightness((object)(value * 100d));
        }

        public void SetBrightness(object o) {
            if (_flatDevice == null || !_flatDevice.Connected) return;
            if (!double.TryParse(o.ToString(), out var result)) return;
            _flatDevice.Brightness = result / 100d;
        }

        private readonly SemaphoreSlim ssConnect = new SemaphoreSlim(1, 1);
        private CancellationTokenSource _connectFlatDeviceCts;

        public async Task<bool> Connect() {
            await ssConnect.WaitAsync();
            try {
                await Disconnect();
                if (_updateTimer != null) {
                    await _updateTimer.Stop();
                }

                var device = FlatDeviceChooserVM.SelectedDevice;
                if (device == null) return false;
                if (device.Id == "No_Device") {
                    profileService.ActiveProfile.FlatDeviceSettings.Id = FlatDeviceChooserVM.SelectedDevice.Id;
                    return false;
                }

                _applicationStatusMediator.StatusUpdate(
                    new ApplicationStatus() {
                        Source = Title,
                        Status = Loc.Instance["LblConnecting"]
                    }
                );
                var flatDevice = (IFlatDevice)device;
                _connectFlatDeviceCts?.Dispose();
                _connectFlatDeviceCts = new CancellationTokenSource();
                try {
                    var connected = await flatDevice.Connect(_connectFlatDeviceCts.Token);
                    _connectFlatDeviceCts.Token.ThrowIfCancellationRequested();
                    if (connected) {
                        _flatDevice = flatDevice;
                        FlatDeviceInfo = new FlatDeviceInfo {
                            MinBrightness = flatDevice.MinBrightness,
                            MaxBrightness = flatDevice.MaxBrightness,
                            Brightness = flatDevice.Brightness,
                            Connected = flatDevice.Connected,
                            CoverState = flatDevice.CoverState,
                            Description = flatDevice.Description,
                            DriverInfo = flatDevice.DriverInfo,
                            DriverVersion = flatDevice.DriverVersion,
                            LightOn = flatDevice.LightOn,
                            Name = flatDevice.Name,
                            SupportsOpenClose = flatDevice.SupportsOpenClose
                        };
                        this.Brightness = flatDevice.Brightness;

                        Notification.ShowSuccess(Loc.Instance["LblFlatDeviceConnected"]);

                        if (_updateTimer != null) {
                            _updateTimer.Interval =
                                profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval;
                            _updateTimer.Start();
                        }

                        profileService.ActiveProfile.FlatDeviceSettings.Id = flatDevice.Id;

                        Logger.Info(
                            $"Successfully connected Flatdevice. Id: {flatDevice.Id} Name: {flatDevice.Name} Driver Version: {flatDevice.DriverVersion}");

                        return true;
                    } else {
                        FlatDeviceInfo.Connected = false;
                        _flatDevice = null;
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
                _applicationStatusMediator.StatusUpdate(
                    new ApplicationStatus {
                        Source = Title,
                        Status = string.Empty
                    }
                );
            }
        }

        private void CancelConnectFlatDevice(object o) {
            _connectFlatDeviceCts?.Cancel();
        }

        public async Task Disconnect() {
            if (!FlatDeviceInfo.Connected) return;
            if (_updateTimer != null) {
                await _updateTimer.Stop();
            }
            _flatDevice?.Disconnect();
            _flatDevice = null;
            FlatDeviceInfo = DeviceInfo.CreateDefaultInstance<FlatDeviceInfo>();
            BroadcastFlatDeviceInfo();
            Logger.Info("Disconnected Flat Device");
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
            var binning = profileService
                ?.ActiveProfile?.FlatDeviceSettings?.GetBrightnessInfoBinnings()
                ?.OrderBy(mode => mode?.Name ?? "").FirstOrDefault();

            var key = new FlatDeviceFilterSettingsKey(Filters.FirstOrDefault()?.Name, binning, gain);
            var value = new FlatDeviceFilterSettingsValue(1d, 1d);
            profileService?.ActiveProfile?.FlatDeviceSettings?.AddBrightnessInfo(key, value);
            _wizardGrid = GetWizardValueBlocks();
            RaiseWizardValuesChanged();
        }

        private void AddBinning(object binning) {
            if (!(binning is BinningMode)) return;
            var gain = Gains.FirstOrDefault();

            var key = new FlatDeviceFilterSettingsKey(Filters.FirstOrDefault()?.Name,
                (BinningMode)binning, gain);
            var value = new FlatDeviceFilterSettingsValue(1d, 1d);
            profileService.ActiveProfile.FlatDeviceSettings.AddBrightnessInfo(key, value);
            _wizardGrid = GetWizardValueBlocks();
            RaiseWizardValuesChanged();
        }

        private void DeleteGainDialog(object gain) {
            if (!(gain is int)) return;

            var dialog = MyMessageBox.MyMessageBox.Show($"{Loc.Instance["LblFlatDeviceAreYouSureGain"]} {gain}?",
                "", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxResult.No);
            if (dialog != System.Windows.MessageBoxResult.Yes) return;

            profileService.ActiveProfile.FlatDeviceSettings.RemoveGain((int)gain, _filterWheelMediator.GetAllFilters());
            _wizardGrid = GetWizardValueBlocks();
            RaiseWizardValuesChanged();
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
            profileService.ActiveProfile.FlatDeviceSettings.RemoveBinning(binningMode, _filterWheelMediator.GetAllFilters());
            _wizardGrid = GetWizardValueBlocks();
            RaiseWizardValuesChanged();
        }

        private readonly SemaphoreSlim ssOpen = new SemaphoreSlim(1, 1);

        public async Task<bool> OpenCover() {
            await ssOpen.WaitAsync();
            try {
                var device = FlatDeviceChooserVM.SelectedDevice;
                if (device == null || device.Id == "No_Device") return false;
                var flatDevice = (IFlatDevice)device;
                if (flatDevice.Connected == false) return false;
                if (!flatDevice.SupportsOpenClose) return false;
                return await flatDevice.Open(new CancellationToken());
            } catch (Exception ex) {
                Logger.Error(ex);
                return false;
            } finally {
                ssOpen.Release();
            }
        }

        private readonly SemaphoreSlim ssClose = new SemaphoreSlim(1, 1);

        public async Task<bool> CloseCover() {
            await ssClose.WaitAsync();
            try {
                var device = FlatDeviceChooserVM.SelectedDevice;
                if (device == null || device.Id == "No_Device") return false;
                var flatDevice = (IFlatDevice)device;
                if (flatDevice.Connected == false) return false;
                if (!flatDevice.SupportsOpenClose) return false;
                return await flatDevice.Close(new CancellationToken());
            } catch (Exception ex) {
                Logger.Error(ex);
                return false;
            } finally {
                ssClose.Release();
            }
        }

        private FlatDeviceInfo _flatDeviceInfo;

        public FlatDeviceInfo FlatDeviceInfo {
            get {
                if (_flatDeviceInfo != null) return _flatDeviceInfo;
                _flatDeviceInfo = DeviceInfo.CreateDefaultInstance<FlatDeviceInfo>();
                return _flatDeviceInfo;
            }
            set {
                _flatDeviceInfo = value;
                RaisePropertyChanged();
            }
        }

        public FlatDeviceInfo GetDeviceInfo() {
            return _flatDeviceInfo;
        }

        private void RefreshFlatDeviceList(object obj) {
            FlatDeviceChooserVM.GetEquipment();
        }

        public IFlatDeviceChooserVM FlatDeviceChooserVM { get; set; }

        private void UpdateFlatDeviceValues(Dictionary<string, object> flatDeviceValues) {
            object o = null;
            flatDeviceValues.TryGetValue(nameof(FlatDeviceInfo.Connected), out o);
            _flatDeviceInfo.Connected = (bool)(o ?? false);
            flatDeviceValues.TryGetValue(nameof(FlatDeviceInfo.CoverState), out o);
            _flatDeviceInfo.CoverState = (CoverState)(o ?? CoverState.Unknown);
            flatDeviceValues.TryGetValue(nameof(FlatDeviceInfo.Brightness), out o);
            _flatDeviceInfo.Brightness = (double)(o ?? 0.0);
            flatDeviceValues.TryGetValue(nameof(FlatDeviceInfo.MinBrightness), out o);
            _flatDeviceInfo.MinBrightness = (int)(o ?? 0);
            flatDeviceValues.TryGetValue(nameof(FlatDeviceInfo.MaxBrightness), out o);
            _flatDeviceInfo.MaxBrightness = (int)(o ?? 0);
            flatDeviceValues.TryGetValue(nameof(FlatDeviceInfo.LightOn), out o);
            _flatDeviceInfo.LightOn = (bool)(o ?? false);
            flatDeviceValues.TryGetValue(nameof(FlatDeviceInfo.SupportsOpenClose), out o);
            _flatDeviceInfo.SupportsOpenClose = (bool)(o ?? false);

            BroadcastFlatDeviceInfo();
        }

        private Dictionary<string, object> GetFlatDeviceValues() {
            var flatDeviceValues = new Dictionary<string, object>
            {
                {nameof(FlatDeviceInfo.Connected), _flatDevice?.Connected ?? false},
                {nameof(FlatDeviceInfo.CoverState), _flatDevice?.CoverState ?? CoverState.Unknown},
                {nameof(FlatDeviceInfo.Brightness), _flatDevice?.Brightness ?? 0.0},
                {nameof(FlatDeviceInfo.MinBrightness), _flatDevice?.MinBrightness ?? 0},
                {nameof(FlatDeviceInfo.MaxBrightness), _flatDevice?.MaxBrightness ?? 0},
                {nameof(FlatDeviceInfo.LightOn), _flatDevice?.LightOn ?? false},
                {nameof(FlatDeviceInfo.SupportsOpenClose), _flatDevice?.SupportsOpenClose ?? false}
            };
            return flatDeviceValues;
        }

        public void ToggleLight(object o) {
            if (_flatDevice == null || _flatDevice.Connected == false) return;
            _flatDevice.LightOn = o is bool b && b;
        }

        private FilterWheelInfo _filterWheelInfo;

        public void UpdateDeviceInfo(FilterWheelInfo info) {
            if (info == null) return;
            if (info.Equals(_filterWheelInfo)) return;
            _filterWheelInfo = info;
            RaisePropertyChanged(nameof(Filters));
            _wizardGrid = GetWizardValueBlocks();
            RaiseWizardValuesChanged();
        }

        private void RaiseWizardValuesChanged() {
            RaisePropertyChanged(nameof(WizardGrid));
            RaisePropertyChanged(nameof(BinningModes));
            RaisePropertyChanged(nameof(Gains));
        }

        private void SettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            _wizardGrid = GetWizardValueBlocks();
            RaiseWizardValuesChanged();
        }

        private ObservableCollection<WizardValueBlock> _wizardGrid;
        public ObservableCollection<WizardValueBlock> WizardGrid => _wizardGrid ?? (_wizardGrid = GetWizardValueBlocks());

        public ObservableCollection<int> Gains =>
            new ObservableCollection<int>(profileService?.ActiveProfile?.FlatDeviceSettings?.GetBrightnessInfoGains()?.OrderBy(g => g).ToList()
                                          ?? new List<int>());

        public ICollection<FilterInfo> Filters =>
            _filterWheelMediator?.GetAllFilters() ?? new List<FilterInfo> { new FilterInfo() };

        public ObservableCollection<string> BinningModes => new ObservableCollection<string>(profileService
            ?.ActiveProfile?.FlatDeviceSettings?.GetBrightnessInfoBinnings()
            ?.OrderBy(mode => mode?.Name ?? "").Select(mode => mode?.Name ?? Loc.Instance["LblNone"]).ToList() ?? new List<string>());

        private ObservableCollection<WizardValueBlock> GetWizardValueBlocks() {
            var result = new ObservableCollection<WizardValueBlock>();

            foreach (var binningMode in profileService.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoBinnings()) {
                var block = new WizardValueBlock();
                var counter = 0;

                block.Binning = binningMode?.Name ?? Loc.Instance["LblNone"];
                var column = new WizardGridColumn { ColumnNumber = counter, Header = $"{Loc.Instance["LblFilter"]}" };
                foreach (var filter in Filters) {
                    var key = new FlatDeviceFilterSettingsKey(filter.Name, null, 0);
                    column.Settings.Add(new FilterTiming(0d, 0d, profileService, key, true, false));
                }
                block.Columns.Add(column);
                counter++;
                var atLeastOneValue = false;
                foreach (var gain in Gains) {
                    column = new WizardGridColumn { ColumnNumber = counter, Header = $"{Loc.Instance["LblGain"]} {gain}" };
                    foreach (var filter in Filters) {
                        var key = new FlatDeviceFilterSettingsKey(filter.Name, binningMode, gain);
                        var value = profileService.ActiveProfile.FlatDeviceSettings.GetBrightnessInfo(key);
                        atLeastOneValue |= value != null;
                        column.Settings.Add(value != null
                            ? new FilterTiming(value.Brightness, value.Time, profileService, key, false, false)
                            : new FilterTiming(0d, 0d, profileService, key, false, true));
                    }
                    block.Columns.Add(column);
                    counter++;
                }
                if (atLeastOneValue) result.Add(block);
            }
            return result;
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
            _filterWheelMediator.RemoveConsumer(this);
        }
    }
}
