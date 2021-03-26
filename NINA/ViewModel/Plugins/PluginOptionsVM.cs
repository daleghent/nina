#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Plugin;
using NINA.Profile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.ViewModel.Plugins {

    public class PluginOptionsVM : BaseVM {
        private IPluginProvider pluginProvider;

        public PluginOptionsVM(IPluginProvider pluginProvider, IProfileService profileService) : base(profileService) {
            this.pluginProvider = pluginProvider;

            Task.Run(async () => {
                await pluginProvider.Load();
                Plugins = pluginProvider.Plugins.ToList();
                SelectedPlugin = Plugins.FirstOrDefault();
                RaisePropertyChanged(nameof(Plugins));
                RaisePropertyChanged(nameof(SelectedPlugin));
            });
        }

        private IPlugin selectedPlugin;

        public IPlugin SelectedPlugin {
            get => selectedPlugin;
            set {
                selectedPlugin = value;
                RaisePropertyChanged();
            }
        }

        public List<IPlugin> Plugins { get; set; }
    }
}