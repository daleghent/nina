#region "copyright"
/*
    Copyright © 2016 - 2022 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using NINA.Core.Utility.WindowService;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Equipment.Interfaces;
using NINA.Core.Utility.Http;
using Newtonsoft.Json.Linq;

namespace NINA.Equipment.Equipment.MySwitch.Eagle4 {

    public class Eagle4 : BaseINPC, ISwitchHub {

        public Eagle4(IProfileService profileService) {
            this.profileService = profileService;
        }

        public string Category { get; } = "PrimaLuceLab";

        public bool HasSetupDialog {
            get => true;
        }

        public string Id {
            get => "EAGLE4";
        }

        public string Name {
            get => "EAGLE4";
        }

        private bool connected;

        public bool Connected {
            get => connected;
            set {
                connected = value;
                RaisePropertyChanged();
            }
        }

        public string Description {
            get => "EAGLE4";
        }

        public string DriverInfo {
            get => string.Empty;
        }

        public string DriverVersion {
            get => string.Empty;
        }

        public ICollection<ISwitch> Switches { get; private set; } = new List<ISwitch>();

        private async Task<bool> AddSwitches() {
            var url = BaseUrl + "getall";

            Logger.Trace($"Try getting all values via {url}");

            var request = new HttpGetRequest(url, Id);
            var response = await request.Request(new CancellationToken());

            var jobj = JObject.Parse(response);
            if (jobj.ContainsKey("result") && jobj.GetValue("result").ToString().ToLower().Trim() == "ok") {
                if (jobj.ContainsKey("supply")) {
                    Switches.Add(new EagleInputPower(0, BaseUrl));
                }
                for (short i = 0; i < 10; i++) {
                    if (jobj.ContainsKey($"pwrout{i}")) {
                        Switches.Add(new Eagle12VPower(i, BaseUrl));
                    }
                    if (jobj.ContainsKey($"regout{i}")) {
                        Switches.Add(new EagleVariablePower(i, BaseUrl));
                    }
                    if (jobj.ContainsKey($"pwrhub{i}")) {
                        Switches.Add(new EagleUSBSwitch(i, BaseUrl));
                    }
                }
                return true;
            } else {
                return false;
            }
        }

        public async Task<bool> Connect(CancellationToken token) {
            Logger.Trace("Connecting to EAGLE");
            Connected = await AddSwitches();
            if (!Connected) {
                Switches.Clear();
                Logger.Error("Unable to connect to EAGLE");
            }
            Logger.Trace("Successfully connected to EAGLE");
            return Connected;
        }

        public void Disconnect() {
            Switches.Clear();
            Connected = false;
        }

        private IWindowService windowService;

        public IWindowService WindowService {
            get {
                if (windowService == null) {
                    windowService = new WindowService();
                }
                return windowService;
            }
            set {
                windowService = value;
            }
        }

        private IProfileService profileService;

        public string BaseUrl {
            get => profileService.ActiveProfile.SwitchSettings.EagleUrl;
            set {
                profileService.ActiveProfile.SwitchSettings.EagleUrl = value;
                RaisePropertyChanged();
            }
        }

        public void SetupDialog() {
            WindowService.Close();
            WindowService.Show(this, "EAGLE Setup", System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.ToolWindow);
        }
    }
}