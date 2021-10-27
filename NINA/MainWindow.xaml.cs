#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Microsoft.Win32;
using NINA.Core.Utility;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace NINA {

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        public MainWindow() {
            InitializeComponent();
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e) {
            var tmp = this.WindowState;
            this.WindowState = WindowState.Minimized;
            this.WindowState = tmp;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed) {
                DragMove();
            }
        }

        protected override void OnSourceInitialized(EventArgs e) {
            base.OnSourceInitialized(e);
            ((HwndSource)PresentationSource.FromVisual(this)).AddHook(HookProc);

            this.WindowState = Properties.Settings.Default.WindowState;
            this.Top = Properties.Settings.Default.WindowTop;
            this.Left = Properties.Settings.Default.WindowLeft;
            this.Width = Properties.Settings.Default.WindowWidth;
            this.Height = Properties.Settings.Default.WindowHeight;

            sizeInitialized = true;
        }

        private IntPtr HookProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
            if (msg == 0x0084 /*WM_NCHITTEST*/ ) {
                // This prevents a crash in WindowChromeWorker._HandleNCHitTest
                try {
                    lParam.ToInt32();
                } catch (OverflowException) {
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        private bool sizeInitialized = false;

        private void ThisWindow_LocationChanged(object sender, EventArgs e) {
            if (sizeInitialized && this.WindowState != WindowState.Maximized) {
                Properties.Settings.Default.WindowTop = this.Top;
                Properties.Settings.Default.WindowLeft = this.Left;
                Properties.Settings.Default.WindowWidth = this.Width;
                Properties.Settings.Default.WindowHeight = this.Height;
            }
        }

        private void ThisWindow_StateChanged(object sender, EventArgs e) {
            if (sizeInitialized) {
                Properties.Settings.Default.WindowState = this.WindowState;
                CoreUtil.SaveSettings(NINA.Properties.Settings.Default);
            }
        }
    }
}