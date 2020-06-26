#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Profile;
using NINA.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZWOptical.EFWSDK;

namespace NINA.Model.MyFilterWheel {

    internal class ASIFilterWheel : BaseINPC, IFilterWheel {
        private int id;
        private IProfileService profileService;

        public ASIFilterWheel(int idx, IProfileService profileService) {
            _ = EFWdll.GetID(idx, out var id);
            this.id = id;
            this.profileService = profileService;
        }

        public short InterfaceVersion => 1;

        public int[] FocusOffsets => this.Filters.Select((x) => x.FocusOffset).ToArray();

        public string[] Names => this.Filters.Select((x) => x.Name).ToArray();

        public ArrayList SupportedActions => new ArrayList();

        public AsyncObservableCollection<FilterInfo> Filters {
            get {
                var filtersList = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
                int i = filtersList.Count();

                var positions = info.slotNum;

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

        public bool HasSetupDialog { get; } = false;

        public string Id {
            get {
                return $"{Name} {id}";
            }
        }

        public string Name => "ZWOptical FilterWheel";

        public string Category => "ZWOptical";

        private bool _connected = false;
        private EFWdll.EFW_INFO info;

        public bool Connected {
            get => _connected;
            private set {
                _connected = value;
                RaisePropertyChanged();
            }
        }

        public string Description => "Native driver for ZWOptical filter wheels";

        public string DriverInfo => "Native driver for ZWOptical filter wheels";

        public string DriverVersion => "1.0";

        public short Position {
            get {
                var err = EFWdll.GetPosition(this.info.ID, out var position);
                if (err == EFWdll.EFW_ERROR_CODE.EFW_SUCCESS) {
                    return (short)position;
                } else {
                    Logger.Error($"EFW Communication error to get position {err}");
                    return -1;
                }
            }

            set {
                var err = EFWdll.SetPosition(this.info.ID, value);
                if (err != EFWdll.EFW_ERROR_CODE.EFW_SUCCESS) {
                    Logger.Error($"EFW Communication error during position change {err}");
                }
            }
        }

        public Task<bool> Connect(CancellationToken token) {
            return Task.Run(() => {
                if (EFWdll.Open(this.id) == EFWdll.EFW_ERROR_CODE.EFW_SUCCESS) {
                    Connected = true;

                    EFWdll.GetProperty(this.id, out var info);
                    this.info = info;

                    EFWdll.SetDirection(this.info.ID, false);

                    Connected = true;
                    return true;
                } else {
                    Logger.Error("Failed to connect to EFW");
                    return false;
                };
            });
        }

        public void Disconnect() {
            _ = EFWdll.Close(this.id);
            this.Connected = false;
        }

        public void SetupDialog() {
        }
    }
}