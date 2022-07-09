#region "copyright"

/*
    Copyright © 2016 - 2022 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility.Converters;
using NINACustomControlLibrary;
using System;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Navigation;

namespace NINA.View {

    /// <summary>
    /// Interaction logic for FramingAssistantView.xaml
    /// </summary>
    public partial class FramingAssistantView : UserControl {

        public FramingAssistantView() {
            InitializeComponent();
        }

        public void OverlapUnitCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            Binding binding = new Binding("OverlapValue") { Mode = BindingMode.TwoWay };
            if ((string)e.AddedItems[0] == "%") {
                binding.Converter = new PercentageConverter();
            }
            OverlapValueStepperControl.SetBinding(IntStepperControl.ValueProperty, binding);
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e) {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}