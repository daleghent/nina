#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using ASCOM.DriverAccess;
using NINA.Core.Locale;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace NINA.Equipment.Equipment.MyFilterWheel {

    internal class AscomFilterWheel : AscomDevice<FilterWheel>, IFilterWheel, IDisposable {

        public AscomFilterWheel(string filterWheelId, string name, IProfileService profileService) : base(filterWheelId, name) {
            this.profileService = profileService;
        }

        public short InterfaceVersion {
            get {
                return device.InterfaceVersion;
            }
        }

        public int[] FocusOffsets {
            get {
                return device.FocusOffsets;
            }
        }

        public string[] Names {
            get {
                return device.Names;
            }
        }

        public short Position {
            get {
                if (Connected) {
                    return device.Position;
                } else {
                    return -1;
                }
            }
            set {
                if (Connected) {
                    try {
                        Logger.Debug($"ASCOM FW: Moving to position {value}");
                        device.Position = value;
                    } catch (ASCOM.DriverAccessCOMException ex) {
                        Notification.ShowWarning(ex.Message);
                    }
                }

                RaisePropertyChanged();
            }
        }

        public ArrayList SupportedActions {
            get {
                return device.SupportedActions;
            }
        }

        private IProfileService profileService;

        public AsyncObservableCollection<FilterInfo> Filters {
            get {
                return profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
            }
        }

        protected override string ConnectionLostMessage => Loc.Instance["LblFilterwheelConnectionLost"];

        protected override Task PostConnect() {
            var filtersList = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
            int profileFilters = filtersList.Count();

            var deviceFilters = device.Names.Length;

            if (profileFilters < deviceFilters) {
                /* Not enough filters defined. Add missing to the list */
                for (int i = profileFilters; i < deviceFilters; i++) {
                    var filter = new FilterInfo(device.Names[i], device.FocusOffsets[i], (short)i);
                    Logger.Info($"Not enough filters defined in the equipment filter list. Importing filter: {filter.Name}, focus offset: {filter.FocusOffset}");
                    filtersList.Add(filter);
                }
            } else if (profileFilters > deviceFilters) {
                /* Too many filters defined. Truncate the list */
                for (int i = profileFilters - 1; i >= deviceFilters; i--) {
                    var filterToRemove = filtersList[i];
                    Logger.Warning($"Too many filters defined in the equipment filter list. Removing filter: {filterToRemove.Name}, focus offset: {filterToRemove.FocusOffset}");
                    filtersList.Remove(filterToRemove);
                }
            }
            profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters = filtersList;
            return Task.CompletedTask;
        }

        protected override FilterWheel GetInstance(string id) {
            return new FilterWheel(id);
        }
    }
}