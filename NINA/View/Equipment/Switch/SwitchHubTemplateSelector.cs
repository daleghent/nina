#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Equipment.MySwitch.PegasusAstro;
using System.Windows;
using System.Windows.Controls;

namespace NINA.View.Equipment.Switch {

    internal class SwitchHubTemplateSelector : DataTemplateSelector {
        public DataTemplate Generic { get; set; }
        public DataTemplate Eagle { get; set; }
        public DataTemplate UltimatePowerBoxV2 { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            switch (item) {
                case Eagle _:
                    return Eagle;

                case UltimatePowerBoxV2 _:
                    return UltimatePowerBoxV2;

                default:
                    return Generic;
            }
        }
    }
}