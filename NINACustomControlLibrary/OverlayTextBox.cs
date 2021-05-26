#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NINACustomControlLibrary {

    public class OverlayTextBox : TextBox {

        static OverlayTextBox() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(OverlayTextBox), new FrameworkPropertyMetadata(typeof(OverlayTextBox)));
        }

        public static readonly DependencyProperty OverlayTextProperty =
           DependencyProperty.Register(nameof(OverlayText), typeof(string), typeof(OverlayTextBox), new UIPropertyMetadata(string.Empty));

        public string OverlayText {
            get {
                return (string)GetValue(OverlayTextProperty);
            }
            set {
                SetValue(OverlayTextProperty, value);
            }
        }
    }
}