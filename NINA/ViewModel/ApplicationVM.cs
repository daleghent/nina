#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Profile;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using NINA.ViewModel.Equipment.Camera;
using NINA.ViewModel.Interfaces;
using System;
using System.Windows;
using System.Windows.Input;

namespace NINA.ViewModel {
    internal class ApplicationVM : BaseVM, IApplicationVM, ICameraConsumer {
        public ApplicationVM(IProfileService profileService, ISequenceMediator sequenceMediator, ProjectVersion projectVersion, ICameraMediator cameraMediator, IApplicationMediator applicationMediator) : base(profileService) {
            if (Properties.Settings.Default.UpdateSettings) {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpdateSettings = false;
                Properties.Settings.Default.Save();
            }

            applicationMediator.RegisterHandler(this);
            this.sequenceMediator = sequenceMediator;
            this.projectVersion = projectVersion;
            this.cameraMediator = cameraMediator;
            cameraMediator.RegisterConsumer(this);

            Logger.SetLogLevel(profileService.ActiveProfile.ApplicationSettings.LogLevel);

            ExitCommand = new RelayCommand(ExitApplication);
            ClosingCommand = new RelayCommand(ClosingApplication);
            MinimizeWindowCommand = new RelayCommand(MinimizeWindow);
            MaximizeWindowCommand = new RelayCommand(MaximizeWindow);
            CheckProfileCommand = new RelayCommand(LoadProfile);
            OpenManualCommand = new RelayCommand(OpenManual);
            CheckASCOMPlatformVersionCommand = new RelayCommand(CheckASCOMPlatformVersion);

            profileService.ProfileChanged += ProfileService_ProfileChanged;
        }

        private void CheckASCOMPlatformVersion(object obj) {
            try {
                var version = ASCOMInteraction.GetPlatformVersion();
                if ((version.Major < 6) || (version.Major == 6 && version.Minor < 4)) {
                    Notification.ShowWarning(Locale.Loc.Instance["LblASCOMPlatformOutdated"]);
                }
            } catch (Exception) {
            }
        }

        private void ProfileService_ProfileChanged(object sender, EventArgs e) {
            RaisePropertyChanged(nameof(ActiveProfile));
        }

        private void LoadProfile(object obj) {
            if (profileService.Profiles.Count > 1) {
                new ProfileSelectVM(profileService).SelectProfile();
            }
        }

        private void OpenManual(object o) {
            System.Diagnostics.Process.Start("https://nighttime-imaging.eu/docs/master/site/");
        }

        public void ChangeTab(ApplicationTab tab) {
            TabIndex = (int)tab;
        }

        public string Version => projectVersion.ToString();

        public string Title {
            get {
                return Utility.Utility.Title;
            }
        }

        private int _tabIndex;
        private CameraInfo cameraInfo = DeviceInfo.CreateDefaultInstance<CameraInfo>();
        private readonly ICameraMediator cameraMediator;

        public int TabIndex {
            get {
                return _tabIndex;
            }
            set {
                _tabIndex = value;
                RaisePropertyChanged();
            }
        }

        private static void MaximizeWindow(object obj) {
            if (Application.Current.MainWindow.WindowState == WindowState.Maximized) {
                Application.Current.MainWindow.WindowState = WindowState.Normal;
            } else {
                Application.Current.MainWindow.WindowState = WindowState.Maximized;
            }
        }

        private void MinimizeWindow(object obj) {
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
        }

        private void ExitApplication(object obj) {
            if (sequenceMediator.OkToExit() == false)
                return;
            if (cameraInfo.Connected) {
                var diag = MyMessageBox.MyMessageBox.Show(Locale.Loc.Instance["LblCameraConnectedOnExit"], "", MessageBoxButton.OKCancel, MessageBoxResult.Cancel);
                if (diag != MessageBoxResult.OK) {
                    return;
                }
            }

            Application.Current.Shutdown();
        }

        private void ClosingApplication(object o) {
            Notification.Dispose();
        }

        public void UpdateDeviceInfo(CameraInfo deviceInfo) {
            cameraInfo = deviceInfo;
        }

        public void Dispose() {
            cameraMediator.RemoveConsumer(this);
        }

        private readonly ISequenceMediator sequenceMediator;
        private readonly ProjectVersion projectVersion;

        public ICommand MinimizeWindowCommand { get; private set; }
        public ICommand MaximizeWindowCommand { get; private set; }
        public ICommand CheckProfileCommand { get; }
        public ICommand CheckUpdateCommand { get; private set; }
        public ICommand OpenManualCommand { get; private set; }
        public ICommand ExitCommand { get; private set; }
        public ICommand ClosingCommand { get; private set; }
        public ICommand CheckASCOMPlatformVersionCommand { get; private set; }
    }

    public enum ApplicationTab {
        EQUIPMENT,
        SKYATLAS,
        FRAMINGASSISTANT,
        FLATWIZARD,
        SEQUENCE,
        IMAGING,
        OPTIONS
    }
}