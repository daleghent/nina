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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MySafetyMonitor {

    internal class AscomSafetyMonitor : BaseINPC, ISafetyMonitor {
        private SafetyMonitor safetyMonitor;

        public AscomSafetyMonitor(string id, string name) {
            this.Id = id;
            this.Name = name;
        }

        public bool IsSafe {
            get {
                if (Connected) {
                    return safetyMonitor.IsSafe;
                } else {
                    return false;
                }
            }
        }

        public bool HasSetupDialog {
            get {
                return true;
            }
        }

        public string Id { get; }

        public string Name { get; }

        public string Category { get; } = "ASCOM";

        private bool _connected;

        public bool Connected {
            get {
                if (_connected) {
                    bool val = false;
                    try {
                        val = safetyMonitor.Connected;
                        if (_connected != val) {
                            Notification.ShowWarning(Locale.Loc.Instance["LblSafetyMonitorConnectionLost"]);
                            Disconnect();
                        }
                    } catch (Exception ex) {
                        Logger.Error(ex);
                        Notification.ShowWarning(Locale.Loc.Instance["LblSafetyMonitorConnectionLost"]);
                        try {
                            Disconnect();
                        } catch (Exception disconnectEx) {
                            Logger.Error(disconnectEx);
                        }
                    }
                    return val;
                } else {
                    return false;
                }
            }
            private set {
                try {
                    safetyMonitor.Connected = value;
                    _connected = value;
                } catch (Exception ex) {
                    Logger.Error(ex);
                    _connected = false;
                }
            }
        }

        public string Description {
            get {
                return safetyMonitor.Description;
            }
        }

        public string DriverInfo {
            get {
                return safetyMonitor.DriverInfo;
            }
        }

        public string DriverVersion {
            get {
                return safetyMonitor.DriverVersion;
            }
        }

        public async Task<bool> Connect(CancellationToken token) {
            return await Task<bool>.Run(() => {
                try {
                    safetyMonitor = new SafetyMonitor(Id);
                    Connected = true;
                    if (Connected) {
                        RaiseAllPropertiesChanged();
                    }
                } catch (ASCOM.DriverAccessCOMException ex) {
                    Utility.Utility.HandleAscomCOMException(ex);
                } catch (System.Runtime.InteropServices.COMException ex) {
                    Utility.Utility.HandleAscomCOMException(ex);
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError("Unable to connect to safety monitor " + ex.Message);
                }
                return Connected;
            });
        }

        public void Disconnect() {
            Connected = false;
            Dispose();
        }

        public void Dispose() {
            safetyMonitor?.Dispose();
            safetyMonitor = null;
        }

        public void SetupDialog() {
            if (HasSetupDialog) {
                try {
                    bool dispose = false;
                    if (safetyMonitor == null) {
                        safetyMonitor = new SafetyMonitor(Id);
                        dispose = true;
                    }
                    safetyMonitor.SetupDialog();
                    if (dispose) {
                        safetyMonitor.Dispose();
                        safetyMonitor = null;
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
                }
            }
        }
    }
}