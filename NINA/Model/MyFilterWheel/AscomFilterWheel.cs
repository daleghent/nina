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
using NINA.Utility;
using NINA.Utility.Notification;
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace NINA.Model.MyFilterWheel {

    internal class AscomFilterWheel : AscomDevice<FilterWheel>, IFilterWheel, IDisposable {

        public AscomFilterWheel(string filterWheelId, string name) : base(filterWheelId, name) {
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

        private AsyncObservableCollection<FilterInfo> _filters;

        public AsyncObservableCollection<FilterInfo> Filters {
            get {
                return _filters;
            }
            private set {
                _filters = value;
                RaisePropertyChanged();
            }
        }

        protected override string ConnectionLostMessage => Locale.Loc.Instance["LblFilterwheelConnectionLost"];

        protected override Task PostConnect() {
            var l = new AsyncObservableCollection<FilterInfo>();
            for (int i = 0; i < Names.Length; i++) {
                l.Add(new FilterInfo(Names[i], FocusOffsets[i], (short)i));
            }
            Filters = l;
            return Task.CompletedTask;
        }

        protected override FilterWheel GetInstance(string id) {
            return new FilterWheel(id);
        }

        protected override void PostDisconnect() {
            Filters?.Clear();
        }
    }
}