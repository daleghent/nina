#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NINA.View.Sequencer {

    /// <summary>
    /// Interaction logic for SequenceContainerView.xaml
    /// </summary>
    public partial class SequenceContainerView : UserControl {

        public SequenceContainerView() {
            InitializeComponent();
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters) {
            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }

        public static readonly DependencyProperty SequenceContainerContentProperty =
            DependencyProperty.Register(nameof(SequenceContainerContent), typeof(object), typeof(SequenceBlockView));

        public object SequenceContainerContent {
            get { return (object)GetValue(SequenceContainerContentProperty); }
            set { SetValue(SequenceContainerContentProperty, value); }
        }

        public static readonly DependencyProperty ShowDetailsProperty =
            DependencyProperty.Register(nameof(ShowDetails), typeof(bool), typeof(SequenceBlockView), new PropertyMetadata(true));

        public bool ShowDetails {
            get { return (bool)GetValue(ShowDetailsProperty); }
            set { SetValue(ShowDetailsProperty, value); }
        }
    }
}