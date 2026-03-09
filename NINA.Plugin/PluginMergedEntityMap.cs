#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Collections.Generic;

namespace NINA.Plugin {
    static class PluginMergedEntityMap {
        public static Dictionary<string, List<string>> MergedEntities => new Dictionary<string, List<string>>() {
            {
                "Connector",
                new List<string>() {
                    "NINA.Plugins.Connector.Instructions.ConnectAllEquipment",
                    "NINA.Plugins.Connector.Instructions.ConnectEquipment",
                    "NINA.Plugins.Connector.Instructions.DisconnectAllEquipment",
                    "NINA.Plugins.Connector.Instructions.DisconnectEquipment",
                    "NINA.Plugins.Connector.Instructions.ReconnectOnDownloadFailure",
                    "NINA.Plugins.Connector.Instructions.ReconnectTrigger",
                    "NINA.Plugins.Connector.Instructions.SwitchProfile"
                }
            },
            {
                "Device Actions and Commands",
                new List<string>() {
                    "DaleGhent.NINA.DeviceActionsCommands.DeviceActionInstruction",
                    "DaleGhent.NINA.DeviceActionsCommands.SendCommandInstruction",
                    "DaleGhent.NINA.DeviceActionsCommands.DAaCDock"
                }
            }
        };
    }
}
