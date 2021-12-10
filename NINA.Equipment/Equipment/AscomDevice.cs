#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using ASCOM;
using ASCOM.DriverAccess;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment {

    /// <summary>
    /// The unified class that handles the shared properties of all ASCOM devices like Connection, Generic Info and Setup
    /// </summary>
    public abstract class AscomDevice<I, T> : BaseINPC, IDevice 
        where T : AscomDriver, I 
        where I : class {

        public AscomDevice(string id, string name, IDeviceDispatcher deviceDispatcher, DeviceDispatcherType deviceDispatcherType) {
            Id = id;
            Name = name;
            DeviceDispatcher = deviceDispatcher;
            DeviceDispatcherType = deviceDispatcherType;
        }

        protected IDeviceDispatcher DeviceDispatcher { get; private set; }
        protected DeviceDispatcherType DeviceDispatcherType { get; private set; }
        private AscomDriver baseDevice;
        protected I device;
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
                    return DeviceDispatcher.Invoke(DeviceDispatcherType, () => baseDevice?.Description) ?? string.Empty;
                } catch (Exception) { }
                return string.Empty;
            }
        }

        public string DriverInfo {
            get {
                try {
                    return DeviceDispatcher.Invoke(DeviceDispatcherType, () => baseDevice?.DriverInfo) ?? string.Empty;
                } catch (Exception) { }
                return string.Empty;
            }
        }

        public string DriverVersion {
            get {
                try {
                    return DeviceDispatcher.Invoke(DeviceDispatcherType, () => baseDevice?.DriverVersion) ?? string.Empty;
                } catch (Exception) { }
                return string.Empty;
            }
        }

        private bool connected;

        private void DisconnectOnConnectionError() {
            try {
                Notification.ShowWarning(ConnectionLostMessage);
                Disconnect();
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        public bool Connected {
            get {
                lock (lockObj) {
                    if (connected && device != null) {
                        bool val = false;

                        try {
                            bool expected;
                            val = DeviceDispatcher.Invoke(DeviceDispatcherType, () => baseDevice?.Connected) ?? false;
                            expected = connected;
                            if (expected != val) {
                                Logger.Error($"{Name} should be connected but reports to be disconnected. Trying to reconnect...");
                                try {
                                    Connected = true;
                                    if (!DeviceDispatcher.Invoke(DeviceDispatcherType, () => baseDevice.Connected)) {
                                        throw new NotConnectedException();
                                    }
                                    val = true;
                                    Logger.Info($"{Name} reconnection successful");
                                } catch (Exception ex) {
                                    Logger.Error("Reconnection failed. The device might be disconnected! - ", ex);
                                    DisconnectOnConnectionError();
                                }
                            }
                        } catch (Exception ex) {
                            Logger.Error(ex);
                            DisconnectOnConnectionError();
                        }
                        return val;
                    } else {
                        return false;
                    }
                }
            }
            private set {
                lock (lockObj) {
                    if (device != null) {
                        Logger.Debug($"SET {Name} Connected to {value}");
                        DeviceDispatcher.Invoke(DeviceDispatcherType, () => baseDevice.Connected = value);
                        connected = value;
                    }
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
                    propertyGETMemory = new Dictionary<string, PropertyMemory>();
                    propertySETMemory = new Dictionary<string, PropertyMemory>();
                    Logger.Trace($"{Name} - Calling PreConnect");
                    await PreConnect();

                    Logger.Trace($"{Name} - Creating instance for {Id}");
                    var concreteDevice = GetInstance(Id);
                    baseDevice = concreteDevice;
                    device = DeviceDispatchInterceptor<I>.Wrap(concreteDevice, DeviceDispatcher, DeviceDispatcherType);

                    Connected = true;
                    if (Connected) {
                        Logger.Trace($"{Name} - Calling PostConnect");
                        await PostConnect();
                        RaiseAllPropertiesChanged();
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(string.Format(Loc.Instance["LblUnableToConnect"], Name, ex.Message));
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
                    DeviceDispatcher.Invoke(DeviceDispatcherType, () => baseDevice.SetupDialog());
                    if (dispose) {
                        DeviceDispatcher.Invoke(DeviceDispatcherType, () => baseDevice.Dispose());
                        baseDevice = null;
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
                }
            }
        }

        public void Disconnect() {
            lock (lockObj) {
                Logger.Info($"Disconnecting from {Id} {Name}");
                Logger.Trace($"{Name} - Calling PreDisconnect");
                PreDisconnect();
                try {
                    Connected = false;
                } catch (Exception ex) {
                    Logger.Error(ex);
                }
                connected = false;
                Logger.Trace($"{Name} - Calling PostDisconnect");
                PostDisconnect();
                Dispose();
            }
            RaiseAllPropertiesChanged();
        }

        public void Dispose() {
            Logger.Trace($"{Name} - Disposing device");
            DeviceDispatcher.Invoke(DeviceDispatcherType, () => baseDevice?.Dispose());
            baseDevice = null;
        }

        protected Dictionary<string, PropertyMemory> propertyGETMemory = new Dictionary<string, PropertyMemory>();
        protected Dictionary<string, PropertyMemory> propertySETMemory = new Dictionary<string, PropertyMemory>();

        /// <summary>
        /// Tries to get a property by its name. If an exception occurs the last known value will be used instead.
        /// If a PropertyNotImplementedException occurs, the "isImplemetned" value will be set to false
        /// </summary>
        /// <typeparam name="PropT"></typeparam>
        /// <param name="propertyName">Property Name of the AscomDevice property</param>
        /// <param name="defaultValue">The default value to be returned when not connected or not implemented</param>
        /// <returns></returns>
        protected PropT GetProperty<PropT>(string propertyName, PropT defaultValue) {
            if (device != null) {
                var retries = 3;

                var interval = TimeSpan.FromMilliseconds(100);
                var type = device.GetType();

                if (!propertyGETMemory.TryGetValue(propertyName, out var memory)) {
                    memory = new PropertyMemory(type.GetProperty(propertyName));
                    lock (propertyGETMemory) {
                        propertyGETMemory[propertyName] = memory;
                    }
                }

                for (int i = 0; i < retries; i++) {
                    try {
                        if (i > 0) {
                            Thread.Sleep(interval);
                            Logger.Info($"Retrying to GET {type.Name}.{propertyName} - Attempt {i + 1} / {retries}");
                        }

                        if (memory.IsImplemented && Connected) {
                            PropT value = (PropT)memory.GetValue(device);

                            Logger.Trace($"GET {type.Name}.{propertyName}: {value}");

                            return (PropT)memory.LastValue;
                        } else {
                            return defaultValue;
                        }
                    } catch (Exception ex) {
                        if (ex.InnerException is PropertyNotImplementedException || ex.InnerException is System.NotImplementedException) {
                            Logger.Info($"Property {type.Name}.{propertyName} GET is not implemented in this driver ({Name})");

                            memory.IsImplemented = false;
                            return defaultValue;
                        }

                        if (ex.InnerException is NotConnectedException) {
                            Logger.Error($"{Name} is not connected ", ex.InnerException);
                        }

                        var logEx = ex.InnerException ?? ex;
                        Logger.Error($"An unexpected exception occurred during GET of {type.Name}.{propertyName}: ", logEx);
                    }
                }

                var val = (PropT)memory.LastValue;
                Logger.Info($"GET {type.Name}.{propertyName} failed - Returning last known value {val}");
                return val;
            }
            return defaultValue;
        }

        /// <summary>
        /// Tries to set a property by its name. If an exception occurs it will be logged.
        /// If a PropertyNotImplementedException occurs, the "isImplemetned" value will be set to false
        /// </summary>
        /// <typeparam name="PropT"></typeparam>
        /// <param name="propertyName">Property Name of the AscomDevice property</param>
        /// <param name="value">The value to be set for the given property</param>
        /// <returns></returns>
        protected bool SetProperty<PropT>(string propertyName, PropT value, [CallerMemberName] string originalPropertyName = null) {
            if (device != null) {
                var type = device.GetType();

                if (!propertySETMemory.TryGetValue(propertyName, out var memory)) {
                    memory = new PropertyMemory(type.GetProperty(propertyName));
                    lock (propertySETMemory) {
                        propertySETMemory[propertyName] = memory;
                    }
                }

                try {
                    if (memory.IsImplemented && Connected) {
                        memory.SetValue(device, value);

                        Logger.Trace($"SET {type.Name}.{propertyName}: {value}");
                        RaisePropertyChanged(originalPropertyName);
                        return true;
                    } else {
                        return false;
                    }
                } catch (Exception ex) {
                    if (ex.InnerException is PropertyNotImplementedException || ex.InnerException is System.NotImplementedException) {
                        Logger.Info($"Property {type.Name}.{propertyName} SET is not implemented in this driver ({Name})");
                        memory.IsImplemented = false;
                        return false;
                    }

                    if (ex is InvalidValueException) {
                        Logger.Warning(ex.Message);
                        return false;
                    }

                    if (ex is ASCOM.InvalidOperationException) {
                        Logger.Warning(ex.Message);
                        return false;
                    }

                    if (ex.InnerException is NotConnectedException) {
                        Logger.Error($"{Name} is not connected ", ex.InnerException);
                        return false;
                    }

                    var message = ex.InnerException?.Message ?? ex.Message;
                    Logger.Error($"An unexpected exception occurred during SET of {type.Name}.{propertyName}: {message}");
                }
            }
            return false;
        }

        protected class PropertyMemory {

            public PropertyMemory(PropertyInfo p) {
                info = p;
                IsImplemented = true;

                LastValue = null;
                if (p.PropertyType.IsValueType) {
                    LastValue = Activator.CreateInstance(p.PropertyType);
                }
            }

            private PropertyInfo info;
            public bool IsImplemented { get; set; }
            public object LastValue { get; set; }

            public object GetValue(I device) {
                var value = info.GetValue(device);

                LastValue = value;

                return value;
            }

            public void SetValue(I device, object value) {
                info.SetValue(device, value);
            }
        }
    }
}