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

namespace NINA.Model {

    /// <summary>
    /// The unified class that handles the shared properties of all ASCOM devices like Connection, Generic Info and Setup
    /// </summary>
    public abstract class AscomDevice<T> : BaseINPC, IDevice where T : AscomDriver {

        public AscomDevice(string id, string name) {
            Id = id;
            Name = name;
        }

        protected T device;
        public string Category { get; } = "ASCOM";
        protected abstract string ConnectionLostMessage { get; }

        protected object lockObj = new object();

        public bool HasSetupDialog {
            get {
                return true;
            }
        }

        public string Id { get; }

        public string Name { get; }

        public string Description {
            get {
                try {
                    return device?.Description ?? string.Empty;
                } catch (Exception) { }
                return string.Empty;
            }
        }

        public string DriverInfo {
            get {
                try {
                    return device?.DriverInfo ?? string.Empty;
                } catch (Exception) { }
                return string.Empty;
            }
        }

        public string DriverVersion {
            get {
                try {
                    return device?.DriverVersion ?? string.Empty;
                } catch (Exception) { }
                return string.Empty;
            }
        }

        private bool connected;

        public bool Connected {
            get {
                if (connected) {
                    bool val = false;

                    try {
                        bool expected;
                        lock (lockObj) {
                            val = device.Connected;
                            expected = connected;
                        }
                        if (expected != val) {
                            Logger.Error($"{Name} should be connected={expected} but reports to be connected={val}");
                            Notification.ShowWarning(ConnectionLostMessage);
                            Disconnect();
                        }
                    } catch (Exception ex) {
                        Logger.Error(ex);
                        Notification.ShowWarning(ConnectionLostMessage);
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
                    Logger.Debug($"{Name} - Try SET Connected to {value}");
                    lock (lockObj) {
                        if (device != null) {
                            device.Connected = value;
                            connected = value;
                        }
                    }
                } catch (Exception ex) {
                    Logger.Error($"{Name} - Connected SET failed", ex);
                    Notification.ShowError(Locale.Loc.Instance["LblFailedChangingConnectionState"] + Environment.NewLine + ex.Message);
                    connected = false;
                }
            }
        }

        /// <summary>
        /// Customizing hook called before connection
        /// </summary>
        protected virtual Task PreConnect() {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Customizing hook called after successful connection
        /// </summary>
        protected virtual Task PostConnect() {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Customizing hook called before disconnection
        /// </summary>
        protected virtual void PreDisconnect() {
        }

        /// <summary>
        /// Customizing hook called after disconnection
        /// </summary>
        protected virtual void PostDisconnect() {
        }

        public Task<bool> Connect(CancellationToken token) {
            return Task.Run(async () => {
                try {
                    Logger.Trace($"{Name} - Calling PreConnect");
                    await PreConnect();

                    Logger.Trace($"{Name} - Creating instance for {Id}");
                    device = GetInstance(Id);
                    Connected = true;
                    if (Connected) {
                        Logger.Trace($"{Name} - Calling PostConnect");
                        await PostConnect();
                        RaiseAllPropertiesChanged();
                    }
                } catch (ASCOM.DriverAccessCOMException ex) {
                    Utility.Utility.HandleAscomCOMException(ex);
                } catch (System.Runtime.InteropServices.COMException ex) {
                    Utility.Utility.HandleAscomCOMException(ex);
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError($"Unable to connect to {Id} - {Name} " + ex.Message);
                }
                return Connected;
            });
        }

        protected abstract T GetInstance(string id);

        public void SetupDialog() {
            if (HasSetupDialog) {
                try {
                    bool dispose = false;
                    if (device == null) {
                        Logger.Trace($"{Name} - Creating instance for {Id}");
                        device = GetInstance(Id);
                        dispose = true;
                    }
                    Logger.Trace($"{Name} - Creating Setup Dialog for {Id}");
                    device.SetupDialog();
                    if (dispose) {
                        device.Dispose();
                        device = null;
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
                }
            }
        }

        public void Disconnect() {
            Logger.Trace($"{Name} - Calling PreDisconnect");
            PreDisconnect();
            Connected = false;
            Logger.Trace($"{Name} - Calling PostDisconnect");
            PostDisconnect();
            Dispose();
        }

        public void Dispose() {
            Logger.Trace($"{Name} - Disposing device");
            device?.Dispose();
            device = null;
        }
    }
}