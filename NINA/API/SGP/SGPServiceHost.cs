#region "copyright"
/*
    Copyright © 2016 - 2022 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using NINA.Core.Interfaces.API.SGP;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Utility;
using Nito.AsyncEx;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace NINA.API.SGP {

    public class SGPServiceHost : ISGPServiceHost {
        private WebApplication app;
        private readonly ISGPServiceBackend backend;
        private volatile Task serviceTask;

        public SGPServiceHost(ISGPServiceBackend backend) {
            serviceTask = null;
            this.backend = backend;

        }

        /*
         * SGP is hardcoded to listen on localhost:59590. Depending on the system, this may be either an IPv4 or IPv6 loopback, so both should be configured to allow http connections.
         * The following commands should be run from an elevated command prompt:
         *
         * 1) netsh http add iplisten ipaddress=::
         * 2) netsh http add iplisten ipaddress=0.0.0.0
         * 3) netsh http add urlacl url=http://+:59590/ user=Everyone
         */

        public void RunService() {
            if (this.serviceTask != null) {
                Logger.Trace("SGP Service already running during start attempt");
                return;
            }

            this.serviceTask = Task.Run(async () => {
                try {
                    var builder = WebApplication.CreateBuilder();
                    builder.WebHost.ConfigureKestrel(options =>
                    {
                        options.AllowSynchronousIO = true;
                        options.ListenLocalhost(59590);
                    });                    
                    builder.Services.AddSingleton<ISGPServiceBackend>(backend);
                    builder.Services.AddControllers().AddNewtonsoftJson(); 
                    
                    builder.Services.AddEndpointsApiExplorer();
                    builder.Services.AddSwaggerGen();


                    app = builder.Build();

                    app.UseSwagger();
                    app.UseSwaggerUI();
                    app.MapControllers();

                    Logger.Info("SGP Service starting");
                    await app.RunAsync();

                } catch (Exception ex) {
                    Logger.Error("Failed to start SGP Server", ex);
                    Notification.ShowError(string.Format(Loc.Instance["LblServerFailed"], ex.Message));
                    throw;
                } finally {
                    await app.StopAsync();
                }
            });
        }

        public void Stop() {
            if (serviceTask != null) {
                Logger.Info("Stopping SGP Service");
                AsyncContext.Run(() => app.StopAsync());
                try {
                    serviceTask.Wait(new CancellationTokenSource(2000).Token);
                    Logger.Info("SGP Service stopped");
                } catch (Exception ex) {
                    Logger.Error("Failed to stop SGP Server", ex);
                } finally {
                    serviceTask = null;
                }
            }
        }
    }
}