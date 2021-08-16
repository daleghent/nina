using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces;
using NINA.Equipment.SDK.CameraSDKs.SBIGSDK;
using NINA.Equipment.SDK.CameraSDKs.SBIGSDK.SbigSharp;
using NINA.Profile.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MyFilterWheel {
    public class SBIGFilterWheel : BaseINPC, IFilterWheel {
        private readonly DeviceQueryInfo queriedDeviceInfo;
        private readonly SDK.CameraSDKs.SBIGSDK.FilterWheelInfo sbigFilterWheelInfo;
        private readonly ISbigSdk sdk;
        private readonly IProfileService profileService;
        private SDK.CameraSDKs.SBIGSDK.DeviceInfo? connectedDevice;

        public SBIGFilterWheel(ISbigSdk sdk, DeviceQueryInfo queriedDeviceInfo, IProfileService profileService) {
            this.sdk = sdk;
            this.queriedDeviceInfo = queriedDeviceInfo;
            if (!queriedDeviceInfo.FilterWheelInfo.HasValue) {
                throw new ArgumentException($"SBIG device {queriedDeviceInfo.Name} does not have FilterWheelInfo");
            }
            this.sbigFilterWheelInfo = queriedDeviceInfo.FilterWheelInfo.Value;
            this.profileService = profileService;
            this.Id = queriedDeviceInfo.SerialNumber;
            this.Name = queriedDeviceInfo.Name;
            this.DriverVersion = sdk.GetSdkVersion();
            this.Description = $"{queriedDeviceInfo.Name} on {queriedDeviceInfo.DeviceId}";
        }

        public string Id { get; private set; }

        public string Name { get; private set; }

        public string Category => "SBIG Legacy";

        public string Description { get; private set; }

        public string DriverInfo { get; private set; }

        public string DriverVersion { get; private set; }

        public int[] FocusOffsets => this.Filters.Select((x) => x.FocusOffset).ToArray();

        public string[] Names => this.Filters.Select((x) => x.Name).ToArray();

        public ArrayList SupportedActions => new ArrayList();

        public AsyncObservableCollection<FilterInfo> Filters {
            get {
                var filtersList = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
                int i = filtersList.Count();
                int positions = (int)sbigFilterWheelInfo.FilterCount;

                if (positions < i) {
                    /* Too many filters defined. Truncate the list */
                    for (; i > positions; i--) {
                        filtersList.RemoveAt(i - 1);
                    }
                } else if (positions > i) {
                    /* Too few filters defined. Add missing filter names using Slot <#> format */
                    for (; i <= positions; i++) {
                        var filter = new FilterInfo(string.Format($"Slot {i}"), 0, (short)i);
                        filtersList.Add(filter);
                    }
                }

                return filtersList;
            }
        }

        private bool _connected = false;
        public bool Connected {
            get => _connected;
            set {
                if (_connected != value) {
                    _connected = value;
                    RaiseAllPropertiesChanged();
                }
            }
        }

        public short Position { 
            get {
                if (!Connected) {
                    Logger.Debug($"SBIGFW: Can't get Position. Device is not connected");
                    return -1;
                }

                var filterWheelStatus = sdk.GetFilterWheelStatus(connectedDevice.Value.DeviceId);
                if (filterWheelStatus.Status == SBIG.CfwStatus.CFWS_BUSY || filterWheelStatus.Position == SBIG.CfwPosition.CFWP_UNKNOWN) {
                    return -1;
                }

                return (short)filterWheelStatus.Position;
            }
            set {
                var currentPosition = this.Position;
                Logger.Debug($"SBIGFW: Moving to position {value}. Currently {currentPosition}");
                if (currentPosition > 0 && currentPosition != value) {
                    sdk.SetFilterWheelPosition(connectedDevice.Value.DeviceId, (ushort)value);
                    RaisePropertyChanged();
                }
            }
        }

        public Task<bool> Connect(CancellationToken ct) {
            if (Connected) {
                return Task.FromResult(true);
            }

            return Task.Run(() => {
                Logger.Info($"SBIGFW: Attempting to connect {this.queriedDeviceInfo.DeviceId}");
                try {
                    ConnectedDevice = sdk.OpenDevice(this.queriedDeviceInfo.DeviceId);
                    if (!ConnectedDevice.FilterWheelInfo.HasValue || ConnectedDevice.FilterWheelInfo.Value.Model == SDK.CameraSDKs.SBIGSDK.SbigSharp.SBIG.CfwModelSelect.CFWSEL_UNKNOWN) {
                        throw new InvalidOperationException($"SBIGFW: Cannot connect {this.queriedDeviceInfo.DeviceId} since it is not a filter wheel");
                    }

                    Connected = true;
                    Logger.Info($"SBIGFW: Successfully connected {this.queriedDeviceInfo.DeviceId}");
                    return true;
                } catch (Exception e) {
                    Logger.Error($"SBIGFW: Failed to connect {this.queriedDeviceInfo.DeviceId}", e);
                    Notification.ShowError($"Failed to connect {this.queriedDeviceInfo.DeviceId}, {e}");
                    if (connectedDevice.HasValue) {
                        sdk.CloseDevice(connectedDevice.Value.DeviceId);
                        connectedDevice = null;
                    }
                    Connected = false;
                    return false;
                }
            }, ct);
        }

        private SDK.CameraSDKs.SBIGSDK.DeviceInfo ConnectedDevice {
            get {
                if (connectedDevice.HasValue) {
                    return connectedDevice.Value;
                }
                throw new Exception($"No connected SBIG device");
            }
            set {
                connectedDevice = value;
            }
        }

        public void Disconnect() {
            if (!Connected) {
                return;
            }

            try {
                if (connectedDevice.HasValue) {
                    sdk.CloseDevice(connectedDevice.Value.DeviceId);
                }
            } catch (Exception e) {
                Logger.Error($"SBIGFW: Failed while trying to close device {this.queriedDeviceInfo.DeviceId}. Ignoring", e);
            } finally {
                connectedDevice = null;
                Connected = false;
            }
        }

        #region Unsupported Operations
        public bool HasSetupDialog => false;

        public void SetupDialog() {
            throw new NotImplementedException();
        }
        #endregion
    }
}
