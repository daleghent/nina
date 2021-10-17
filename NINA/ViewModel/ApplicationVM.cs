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
using NINA.Core.Locale;
using NINA.Core.MyMessageBox;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Utility;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Profile.Interfaces;
using NINA.Utility;
using NINA.View.About;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.Management;
using System.Windows;
using System.Windows.Input;
using NINA.Equipment.Equipment;
using NINA.WPF.Base.ViewModel;
using NINA.WPF.Base.Interfaces.ViewModel;
using System.IO;
using System.Linq;
using NINA.Plugin.Interfaces;
using Nito.AsyncEx;

namespace NINA.ViewModel {

    internal class ApplicationVM : BaseVM, IApplicationVM, ICameraConsumer {

        public ApplicationVM(IProfileService profileService, ProjectVersion projectVersion, ICameraMediator cameraMediator, IApplicationMediator applicationMediator, IImageSaveMediator imageSaveMediator, IPluginLoader pluginProvider) : base(profileService) {
            applicationMediator.RegisterHandler(this);
            this.projectVersion = projectVersion;
            this.cameraMediator = cameraMediator;
            this.imageSaveMediator = imageSaveMediator;
            this.pluginProvider = pluginProvider;
            cameraMediator.RegisterConsumer(this);

            ExitCommand = new RelayCommand(ExitApplication);
            ClosingCommand = new RelayCommand(ClosingApplication);
            MinimizeWindowCommand = new RelayCommand(MinimizeWindow);
            MaximizeWindowCommand = new RelayCommand(MaximizeWindow);
            CheckProfileCommand = new RelayCommand(LoadProfile);
            OpenManualCommand = new RelayCommand(OpenManual);
            OpenAboutCommand = new RelayCommand(OpenAbout);
            CheckASCOMPlatformVersionCommand = new RelayCommand(CheckASCOMPlatformVersion);
            CheckWindowsVersionCommand = new RelayCommand(CheckWindowsVersion);

            profileService.ProfileChanged += ProfileService_ProfileChanged;
        }

        private void CheckASCOMPlatformVersion(object obj) {
            try {
                var version = ASCOMInteraction.GetPlatformVersion();
                Logger.Info($"ASCOM Platform {version} installed");
                if ((version.Major < 6) || (version.Major == 6 && version.Minor < 5)) {
                    Notification.ShowWarning(Loc.Instance["LblASCOMPlatformOutdated"]);
                }
            } catch (Exception) {
                Logger.Info($"No ASCOM Platform installed");
            }
        }

        private void CheckWindowsVersion(object obj) {
            // Minimum support Windows version is (curently) Windows 8 (6.2)
            var minimumVersion = new Version(6, 2);
            string friendlyName = "Windows";

            if (Environment.OSVersion.Version < minimumVersion) {
                try {
                    var searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem");

                    foreach (ManagementObject os in searcher.Get()) {
                        friendlyName = os["Caption"].ToString().Trim();
                        break;
                    }
                } catch (Exception ex) {
                    Logger.Info($"Error getting Windows name: {ex.Message}");
                } finally {
                    Notification.ShowError(string.Format(Loc.Instance["LblYourWindowsIsTooOld"], friendlyName));
                }
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
            System.Diagnostics.Process.Start(CoreUtil.DocumentationPage);
        }

        private void OpenAbout(object o) {
            AboutPageView window = new AboutPageView();
            window.Width = 1280;
            window.Height = 720;
            var service = new WindowServiceFactory().Create();
            service.Show(window, Title + " - " + Loc.Instance["LblAbout"], ResizeMode.NoResize, WindowStyle.ToolWindow);
        }

        public void ChangeTab(ApplicationTab tab) {
            TabIndex = (int)tab;
        }

        public string Version => projectVersion.ToString();

        public string Title {
            get {
                return NINA.Core.Utility.CoreUtil.Title;
            }
        }

        private int _tabIndex;
        private CameraInfo cameraInfo = DeviceInfo.CreateDefaultInstance<CameraInfo>();
        private readonly ICameraMediator cameraMediator;
        private readonly IImageSaveMediator imageSaveMediator;
        private readonly IPluginLoader pluginProvider;

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
            Sequencer.ISequenceNavigationVM vm = ((Interfaces.IMainWindowVM)Application.Current.MainWindow.DataContext).SequenceNavigationVM;
            if (vm.Initialized) {
                if (vm.Sequence2VM.Sequencer.MainContainer.AskHasChanged(vm.Sequence2VM.Sequencer.MainContainer.Name)) {
                    return;
                }
                if (((SimpleSequenceVM)vm.SimpleSequenceVM).AskHasChanged()) {
                    return;
                }
                if (cameraInfo.Connected) {
                    var diag = MyMessageBox.Show(Loc.Instance["LblCameraConnectedOnExit"], "", MessageBoxButton.OKCancel, MessageBoxResult.Cancel);
                    if (diag != MessageBoxResult.OK) {
                        return;
                    }
                }
            }

            imageSaveMediator.Shutdown();

            foreach (var plugin in pluginProvider.Plugins) {
                if (plugin.Value) {
                    try {
                        Logger.Debug($"Tearing down plugin {plugin.Key.Name}");
                        AsyncContext.Run(plugin.Key.Teardown);
                    } catch (Exception ex) {
                        Logger.Error($"Failed to teardown plugin {plugin.Key.Name}", ex);
                    }
                }
            }

            /*if (Directory.Exists(Plugin.Constants.StagingFolder) && Directory.EnumerateFileSystemEntries(Plugin.Constants.StagingFolder).Any()
                || Directory.Exists(Plugin.Constants.DeletionFolder) && Directory.EnumerateFileSystemEntries(Plugin.Constants.DeletionFolder).Any()) {
                Approach doesn't seem to work well together with the profiles and it will switch the active profile, as the app is not closed in time
                var diag = MyMessageBox.Show(Loc.Instance["LblPendingPluginsRestart"], "", MessageBoxButton.YesNo, MessageBoxResult.Cancel);
                if (diag == MessageBoxResult.Yes) {
                    System.Windows.Forms.Application.Restart();
                }
            }*/

            Application.Current.Shutdown();
        }

        private void ClosingApplication(object o) {
            try {
                Properties.Settings.Default.Save();
            } catch (Exception) { }
            Logger.CloseAndFlush();
            Notification.Dispose();
        }

        public void UpdateDeviceInfo(CameraInfo deviceInfo) {
            cameraInfo = deviceInfo;
        }

        public void Dispose() {
            cameraMediator.RemoveConsumer(this);
        }

        private readonly ProjectVersion projectVersion;

        public ICommand MinimizeWindowCommand { get; private set; }
        public ICommand MaximizeWindowCommand { get; private set; }
        public ICommand CheckProfileCommand { get; }
        public ICommand CheckUpdateCommand { get; private set; }
        public ICommand OpenManualCommand { get; private set; }
        public ICommand OpenAboutCommand { get; private set; }
        public ICommand ExitCommand { get; private set; }
        public ICommand ClosingCommand { get; private set; }
        public ICommand CheckASCOMPlatformVersionCommand { get; private set; }
        public ICommand CheckWindowsVersionCommand { get; private set; }
    }
}