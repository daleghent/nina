using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NINA.Equipment.SDK.CameraSDKs.PlayerOneSDK;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MyFilterWheel {
    public class PlayerOneFilterWheel : BaseINPC, IFilterWheel {
        private int id;
        private IProfileService profileService;

        public PlayerOneFilterWheel(int idx, IProfileService profileService) {
            _ = PlayerOneFilterWheelSDK.POAGetPWProperties(idx, out var info);
            this.info = info;
            this.id = info.Handle;
            this.profileService = profileService;
        }

        public short InterfaceVersion => 1;

        public int[] FocusOffsets => this.Filters.Select((x) => x.FocusOffset).ToArray();

        public string[] Names => this.Filters.Select((x) => x.Name).ToArray();

        public IList<string> SupportedActions => new List<string>();

        public AsyncObservableCollection<FilterInfo> Filters {
            get {
                var filtersList = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
                var positions = info.PositionCount;

                // Find duplicate positions due to data corruption and remove duplicates
                var duplicates = filtersList.GroupBy(x => x.Position).Where(x => x.Count() > 1).ToList();
                foreach (var group in duplicates) {
                    foreach (var filterToRemove in group) {
                        Logger.Warning($"Duplicate filter position defined in filter list. Removing the duplicates and importing from filter wheel again. Removing filter: {filterToRemove.Name}, focus offset: {filterToRemove.FocusOffset}");
                        filtersList.Remove(filterToRemove);
                    }
                }

                if (filtersList.Count > 0) {
                    // Scan for missing position indexes between 0 .. maxPosition and reimport them
                    var existingPositions = filtersList.Select(x => (int)x.Position).ToList();
                    var missingPositions = Enumerable.Range(0, existingPositions.Max()).Except(existingPositions);
                    foreach (var position in missingPositions) {
                        if (positions > position) {
                            var filterToAdd = new FilterInfo(string.Format($"Slot {position}"), 0, (short)position);
                            Logger.Warning($"Missing filter position. Importing filter: {filterToAdd.Name}, focus offset: {filterToAdd.FocusOffset}");
                            filtersList.Insert(position, filterToAdd);
                        }
                    }
                }

                int i = filtersList.Count;


                if (positions < i) {
                    /* Too many filters defined. Truncate the list */
                    for (; i > positions; i--) {
                        var filterToRemove = filtersList[i - 1];
                        Logger.Warning($"Too many filters defined in the equipment filter list. Removing filter: {filterToRemove.Name}, focus offset: {filterToRemove.FocusOffset}");
                        filtersList.Remove(filterToRemove);
                    }
                } else if (positions > i) {
                    /* Too few filters defined. Add missing filter names using Slot <#> format */
                    for (; i <= positions; i++) {
                        var filter = new FilterInfo(string.Format($"Slot {i}"), 0, (short)i);
                        filtersList.Add(filter);
                        Logger.Info($"Not enough filters defined in the equipment filter list. Importing filter: {filter.Name}, focus offset: {filter.FocusOffset}");
                    }
                }

                return filtersList;
            }
        }

        public bool HasSetupDialog { get; } = false;

        public string Id {
            get {
                return $"{Category}_{Name}_{id}";
            }
        }

        public string Name => $"{info.Name}";

        public string Category => "Player One";

        private bool _connected = false;
        private PlayerOneFilterWheelSDK.PWProperties info;

        public bool Connected {
            get => _connected;
            private set {
                _connected = value;
                RaisePropertyChanged();
            }
        }

        public string Description => "Native driver for PlayerOne filter wheels";

        public string DriverInfo => "Native driver for PlayerOne filter wheels";

        public string DriverVersion => "1.0";

        public short Position {
            get {
                var err = PlayerOneFilterWheelSDK.POAGetCurrentPosition(this.id, out var position);
                if (err == PlayerOneFilterWheelSDK.PWErrors.PW_OK) {
                    return (short)position;
                } else {
                    Logger.Error($"PlayerOne FilterWheel Communication error to get position {err}");
                    return -1;
                }
            }

            set {
                var err = PlayerOneFilterWheelSDK.POAGotoPosition(this.id, value);
                if (err != PlayerOneFilterWheelSDK.PWErrors.PW_OK) {
                    Logger.Error($"PlayerOne FilterWheel Communication error during position change {err}");
                }
            }
        }

        public Task<bool> Connect(CancellationToken token) {
            return Task.Run(() => {
                if (PlayerOneFilterWheelSDK.POAOpenPW(this.id) == PlayerOneFilterWheelSDK.PWErrors.PW_OK) {
                    Connected = true;

                    PlayerOneFilterWheelSDK.POAGetPWPropertiesByHandle(this.id, out var info);
                    this.info = info;

                    PlayerOneFilterWheelSDK.POASetOneWay(this.id, true);

                    Connected = true;
                    return true;
                } else {
                    Logger.Error("Failed to connect to PlayerOne FilterWheel");
                    return false;
                };
            });
        }

        public void Disconnect() {
            _ = PlayerOneFilterWheelSDK.POAClosePW(this.id);
            this.Connected = false;
        }

        public void SetupDialog() {
        }

        public string Action(string actionName, string actionParameters) {
            throw new NotImplementedException();
        }

        public string SendCommandString(string command, bool raw) {
            throw new NotImplementedException();
        }

        public bool SendCommandBool(string command, bool raw) {
            throw new NotImplementedException();
        }

        public void SendCommandBlind(string command, bool raw) {
            throw new NotImplementedException();
        }
    }
}
