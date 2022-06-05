#region "copyright"

/*
    Copyright © 2016 - 2022 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using Castle.DynamicProxy;
using NINA.Equipment.Interfaces;

namespace NINA.Equipment.Equipment {

    public class DeviceDispatchInterceptor<T> : IAsyncInterceptor where T : class {
        private static readonly ProxyGenerator proxyGenerator = new ProxyGenerator();
        private readonly IDeviceDispatcher deviceDispatcher;
        private readonly DeviceDispatcherType deviceDispatcherType;

        public DeviceDispatchInterceptor(IDeviceDispatcher deviceDispatcher, DeviceDispatcherType deviceDispatcherType) {
            this.deviceDispatcher = deviceDispatcher;
            this.deviceDispatcherType = deviceDispatcherType;
        }

        public static T Wrap(T wrapped, IDeviceDispatcher deviceDispatcher, DeviceDispatcherType deviceDispatcherType, object[] constructorArguments = null) {
            if (typeof(T).IsInterface) {
                return proxyGenerator.CreateInterfaceProxyWithTarget(wrapped, new DeviceDispatchInterceptor<T>(deviceDispatcher, deviceDispatcherType));
            } else {
                if (constructorArguments == null) {
                    return proxyGenerator.CreateClassProxyWithTarget(wrapped, new DeviceDispatchInterceptor<T>(deviceDispatcher, deviceDispatcherType));
                } else {
                    return (T)proxyGenerator.CreateClassProxyWithTarget(typeof(T), wrapped, constructorArguments, new DeviceDispatchInterceptor<T>(deviceDispatcher, deviceDispatcherType));
                }
            }
        }

        public void InterceptAsynchronous(IInvocation invocation) {
            throw new NotImplementedException("InterceptAsynchronous not implemented");
        }

        public void InterceptAsynchronous<TResult>(IInvocation invocation) {
            throw new NotImplementedException("InterceptAsynchronous not implemented");
        }

        public void InterceptSynchronous(IInvocation invocation) {
            deviceDispatcher.Invoke(deviceDispatcherType, () => invocation.Proceed());
        }
    }
}