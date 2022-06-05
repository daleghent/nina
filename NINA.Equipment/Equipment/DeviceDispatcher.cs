#region "copyright"

/*
    Copyright © 2016 - 2022 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Threading;

namespace NINA.Equipment.Equipment {

    public class DeviceDispatcher : IDeviceDispatcher {
        private bool disposed = false;
        private readonly object dispatcherMapLock = new object();
        private readonly Dictionary<DeviceDispatcherType, Dispatcher> dispatcherMap;
        private readonly IProfileService profileService;

        public DeviceDispatcher(IProfileService profileService) {
            this.dispatcherMap = new Dictionary<DeviceDispatcherType, Dispatcher>();
            this.profileService = profileService;
        }

        public Dispatcher GetDispatcher(DeviceDispatcherType deviceType) {
            lock (dispatcherMapLock) {
                if (disposed) {
                    throw new ObjectDisposedException(GetType().FullName);
                }

                Dispatcher dispatcher;
                if (dispatcherMap.TryGetValue(deviceType, out dispatcher)) {
                    return dispatcher;
                }

                // Create an STA-threaded dispatcher thread
                // COM objects use the apartment model of their creator threads, and STA is used because COM objects that require it would otherwise
                // use the only other STA thread in the application - the UI thread
                // This may be improved further by using MTA if we know the COM object's threading model ahead of time, but the author of this code
                // couldn't figure out how to do it. There probably is minimal effect though - if there are multiple threads calling into the ASCOM device simultaneously,
                // then they will be run serially. Before this was added, this was happening anyways along with all UI operations and across devices
                dispatcher = CreateDispatcher(deviceType);
                dispatcherMap.Add(deviceType, dispatcher);
                return dispatcher;
            }
        }

        private static Dispatcher CreateDispatcher(DeviceDispatcherType deviceType) {
            Dispatcher dispatcher = null;
            var dispatcherReadyEvent = new ManualResetEvent(false);

            var thread = new Thread(new ThreadStart(() => {
                dispatcher = Dispatcher.CurrentDispatcher;
                dispatcherReadyEvent.Set();
                Dispatcher.Run();
            }));
            thread.Name = $"{deviceType} Device Dispatcher";
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            dispatcherReadyEvent.WaitOne();
            dispatcher.ShutdownFinished += Dispatcher_ShutdownFinished;
            return dispatcher;
        }

        private static void Dispatcher_ShutdownFinished(object sender, EventArgs e) {
            var dispatcher = (Dispatcher)sender;
            Logger.Info($"Aborting dispatcher thread {dispatcher.Thread.Name}");
            dispatcher.Thread.Abort();
        }

        public TResult Invoke<TResult>(DeviceDispatcherType deviceType, Func<TResult> callback) {
            return callback();
        }

        public void Invoke(DeviceDispatcherType deviceType, Action callback) {
            // Functionality is neutered for now, until this can be sent to an AppDomain instead
            /*
            if (this.profileService.ActiveProfile.ApplicationSettings.PerDeviceThreadingEnabled) {
                var dispatcher = GetDispatcher(deviceType);
                dispatcher.Invoke(callback);
            } else {
                callback();
            }
            */
            callback();
        }

        public void Dispose() {
            lock (dispatcherMapLock) {
                if (disposed) {
                    return;
                }
                disposed = true;
            }

            foreach (var deviceType in dispatcherMap.Keys) {
                try {
                    var dispatcher = dispatcherMap[deviceType];
                    Logger.Info($"Shutting down dispatcher for {deviceType}");
                    dispatcher.BeginInvokeShutdown(DispatcherPriority.Send);
                } catch (Exception e) {
                    Logger.Error($"Exception while shutting down dispatcher for {deviceType}", e);
                }
            }
        }
    }
}