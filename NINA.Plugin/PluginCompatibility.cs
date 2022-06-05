#region "copyright"
/*
    Copyright © 2016 - 2022 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using Newtonsoft.Json;
using NINA.Plugin.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Plugin {
    /// <summary>
    /// This map can be used for known plugin compatibility issues.
    /// A minimum version for the plugin specific version can be specified.
    /// When a plugin is installed with a lower version than specified here, it won't be loaded and shown as an error.
    /// This is necessary due to offline installations not having any information about the latest online plugin manifests
    /// </summary>
    internal class PluginCompatibilityMap {
        private string raw = @"
[
  {
    'name': 'Orbuculum',
    'identifier': 'a6c614f3-c3ab-423a-8be1-d480a253f07c',
    'minimumVersion': '1.0.4.0'
  },
  {
    'name': 'ExoPlanets',
    'identifier': '6d0e07f2-8773-4229-dc2b-f451e53c677f',
    'minimumVersion': '1.1.4.0'
  },
  {
    'name': 'DIY Meridian Flip',
    'identifier': '38367705-3489-4528-9b4a-c7765bc0eced',
    'minimumVersion': '1.1.1.0'
  },
  {
    'name': 'Smart Meridian Flip',
    'identifier': '6d0e07f2-8773-4229-bf2c-f451e53f677a',
    'minimumVersion': '1.0.1.3'
  },
  {
    'name': 'Astro-Physics Tools',
    'identifier': '99688A5D-BD28-4D8D-80D5-3D4192BB987D',
    'minimumVersion': '0.5.0.0'
  },
  {
    'name': 'Ground Station',
    'identifier': '2737AFDF-A1AA-48C3-BE17-0F5F03282AEB',
    'minimumVersion': '1.11.0.0'
  },
  {
    'name': 'Moon Angle',
    'identifier': '036af399-91b0-4a29-a7d3-44af0bfde13e',
    'minimumVersion': '1.3.0.0'
  },
  {
    'name': 'Scope Control',
    'identifier': '0bcbb707-6611-4266-9686-231be457f069',
    'minimumVersion': '1.2.2.0'
  },
  {
    'name': 'Framing Cache Generator',
    'identifier': 'b71ce4a9-17bd-4152-a372-e9e6e127ddfb',
    'minimumVersion': '65535.0.0.0'
  }
]";


        public PluginCompatibilityMap() {
            CompatibilityMap = new Dictionary<string, PluginCompatibility>();


            var obj = JsonConvert.DeserializeObject<List<PluginCompatibility>>(raw);
            foreach (var compatibility in obj) {
                CompatibilityMap[compatibility.Identifier] = compatibility;
            }
        }
        private Dictionary<string, PluginCompatibility> CompatibilityMap { get; }

        public bool IsCompatible(IPluginManifest plugin) {
            if (CompatibilityMap.TryGetValue(plugin.Identifier, out var compatibility)) {
                var minimumCompatibleVersion = new Version(compatibility.MinimumVersion);
                var version = new Version(plugin.Version.Major, plugin.Version.Minor, plugin.Version.Patch, plugin.Version.Build);
                if (version < minimumCompatibleVersion) {
                    return false;
                }
            }
            return true;
        }

        public Version GetMinimumVersion(IPluginManifest plugin) {
            if (CompatibilityMap.TryGetValue(plugin.Identifier, out var compatibility)) {
                return new Version(compatibility.MinimumVersion);
            }
            return new Version("0.0.0.0");
        }

        internal class PluginCompatibility {
            [JsonProperty(propertyName: "identifier")]
            public string Identifier { get; set; }
            [JsonProperty(propertyName: "name")]
            public string Name { get; set; }
            [JsonProperty(propertyName: "minimumVersion")]
            public string MinimumVersion { get; set; }
        }
    }
}
