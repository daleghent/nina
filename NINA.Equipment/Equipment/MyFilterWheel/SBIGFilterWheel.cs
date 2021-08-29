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
                int positions = (int)sbigFilterWheelInfo.FilterCount;

                var seen = new bool[positions];
                for (int i = filtersList.Count - 1; i >= 0; --i) {
                    if (filtersList[i].Position < 0 || filtersList[i].Position >= positions) {
                        // Remove any that are out of bounds
                        filtersList.RemoveAt(i);
                    } else if (seen[filtersList[i].Position]) {
                        // Remove duplicates
                        filtersList.RemoveAt(i);
                    } else {
                        seen[filtersList[i].Position] = true;
                    }
                }

                var sorted = true;
                for (int i = 0; i < positions; ++i) {
                    if (!seen[i]) {
                        var filter = new FilterInfo(string.Format($"Slot {i}"), 0, (short)i);
                        if (i != filtersList.Count) {
                            sorted = false;
                        }
                        filtersList.Add(filter);
                    } else if (filtersList[i].Position != i) {
                        sorted = false;
                    }
                }

                // Lastly, ensure the filters are sorted by position
                if (!sorted) {
                    var filtersListSortedCopy = filtersList.OrderBy(x => x.Position).ToList();
                    filtersList.Clear();
                    foreach (var filter in filtersListSortedCopy) {
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

        private short lastSetPosition = -1;

        public short Position {
            get {
                if (!Connected) {
                    Logger.Debug($"SBIGFW: Can't get Position. Device is not connected");
                    return -1;
                }

                var filterWheelStatus = sdk.GetFilterWheelStatus(connectedDevice.Value.DeviceId);
                if (filterWheelStatus.Status == SBIG.CfwStatus.CFWS_BUSY) {
                    return -1;
                } else if (filterWheelStatus.Position == SBIG.CfwPosition.CFWP_UNKNOWN && filterWheelStatus.Status == SBIG.CfwStatus.CFWS_IDLE) {
                    // Some models can't report their position, so return the last set position while idle
                    return lastSetPosition;
                }

                // Filter wheel positions start from 1, but NINA expects them to be 0-based
                return (short)(filterWheelStatus.Position - 1);
            }
            set {
                var currentPosition = this.Position;
                Logger.Debug($"SBIGFW: Moving to position {value}. Currently {currentPosition}");
                if (currentPosition != value) {
                    // SBIG positions start from 1
                    sdk.SetFilterWheelPosition(connectedDevice.Value.DeviceId, (ushort)(value + 1));
                    RaisePropertyChanged();

                    lastSetPosition = value;
                }
            }
        }

        public Task<bool> Connect(CancellationToken ct) {
            if (Connected) {
                return Task.FromResult(true);
            }

            return Task.Run(async () => {
                Logger.Info($"SBIGFW: Attempting to connect {this.queriedDeviceInfo.DeviceId}");
                try {
                    ConnectedDevice = sdk.OpenDevice(this.queriedDeviceInfo.DeviceId);
                    if (!ConnectedDevice.FilterWheelInfo.HasValue || ConnectedDevice.FilterWheelInfo.Value.Model == SDK.CameraSDKs.SBIGSDK.SbigSharp.SBIG.CfwModelSelect.CFWSEL_UNKNOWN) {
                        throw new InvalidOperationException($"SBIGFW: Cannot connect {this.queriedDeviceInfo.DeviceId} since it is not a filter wheel");
                    }

                    // During connection, the filter wheel might be calibrating and rotating through filters. We wait for it to stop in this case
                    // so we can detect whether it's a model that can report position
                    var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    var cts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
                    var filterWheelStatus = new FilterWheelStatus() {
                        Position = SBIG.CfwPosition.CFWP_UNKNOWN,
                        Status = SBIG.CfwStatus.CFWS_UNKNOWN
                    };
                    while (!cts.Token.IsCancellationRequested) {
                        filterWheelStatus = sdk.GetFilterWheelStatus(connectedDevice.Value.DeviceId);
                        if (filterWheelStatus.Position != SBIG.CfwPosition.CFWP_UNKNOWN || filterWheelStatus.Status != SBIG.CfwStatus.CFWS_BUSY) {
                            break;
                        }
                        Logger.Debug($"SBIGFW: Filter wheel has unknown position on connection. Waiting until it is no longer busy to proceed");
                        await Task.Delay(TimeSpan.FromMilliseconds(500), cts.Token);
                    }
                    cts.Token.ThrowIfCancellationRequested();

                    Connected = true;
                    if (filterWheelStatus.Position == SBIG.CfwPosition.CFWP_UNKNOWN) {
                        Logger.Info($"SBIGFW: Filter wheel has unknown position on connection. Setting to first filter position for initialization");
                        Position = 1;
                    }

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

        #endregion Unsupported Operations
    }
}