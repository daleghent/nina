#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.Core.Enum;
using NINA.Core.Locale;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows;
using NINA.Core.Utility.Notification;
using NINA.Core.Model;
using NINA.Profile.Interfaces;

namespace NINA.Profile {

    public partial class ProfileService : BaseINPC, IProfileService {
        private static object lockobj = new object();

        public static string PROFILEFOLDER = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "Profiles");

        public ProfileService() {
            saveTimer = new System.Timers.Timer();
            saveTimer.Interval = 1000;
            saveTimer.AutoReset = false;
            saveTimer.Elapsed += SaveTimer_Elapsed;

            Profiles = new AsyncObservableCollection<ProfileMeta>();
        }

        private FileSystemWatcher profileFileWatcher;

        public void CreateWatcher() {
            profileFileWatcher?.Dispose();

            profileFileWatcher = new FileSystemWatcher() {
                Path = PROFILEFOLDER,
                NotifyFilter = NotifyFilters.FileName,
                Filter = "*.profile",
                EnableRaisingEvents = false
            };

            profileFileWatcher.Created += ProfileFileWatcher_Created;
            profileFileWatcher.Deleted += ProfileFileWatcher_Deleted;

            profileFileWatcher.EnableRaisingEvents = true;
        }

        private void ProfileFileWatcher_Deleted(object sender, FileSystemEventArgs e) {
            lock (lockobj) {
                if (Guid.TryParse(Path.GetFileNameWithoutExtension(e.Name), out var id)) {
                    var toDelete = Profiles.Where(x => x.Id == id).FirstOrDefault();
                    if (toDelete != null) {
                        Profiles.Remove(toDelete);
                    }
                }
            }
        }

        private void ProfileFileWatcher_Created(object sender, FileSystemEventArgs e) {
            lock (lockobj) {
                ProfileMeta info = null;
                var retries = 0;
                do {
                    info = Profile.Peek(Path.Combine(PROFILEFOLDER, e.Name));
                    if (info == null) {
                        Thread.Sleep(TimeSpan.FromMilliseconds(500));
                        retries++;
                    }
                } while (retries < 3 && info == null);

                if (info != null) {
                    Profiles.Add(info);
                } else {
                    var id = Guid.Parse(Path.GetFileNameWithoutExtension(e.Name));
                    Profiles.Add(new ProfileMeta() { Id = id, Location = e.FullPath, LastUsed = DateTime.MinValue, IsActive = false, Name = "UNKOWN" });
                }
            }
        }

        /// <summary>
        /// Timer that will trigger a save after 200ms
        /// When another profile change happens during that time, the duration is reset
        /// This way something like a slider will not spam the harddisk with save operations
        /// </summary>
        private System.Timers.Timer saveTimer;

        /// <summary>
        /// Stop the timer and save the profile
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
            Save();
        }

        public bool TryLoad(string startWithProfileId) {
            lock (lockobj) {
                using (MyStopWatch.Measure()) {
                    if (!Directory.Exists(PROFILEFOLDER)) {
                        Directory.CreateDirectory(PROFILEFOLDER);
                    }

                    //This will migrate the namespaces of existing profiles when NINA was not split into multiple smaller projects
                    MigrateModularizedSolutionNamespaceChange();

                    var loadSpecificProfile = !string.IsNullOrWhiteSpace(startWithProfileId);
                    ProfileWasSpecifiedFromCommandLineArgs = loadSpecificProfile;
                    Directory
                        .GetFiles(PROFILEFOLDER, "*.profile")
                        .Select(Profile.Peek)
                        .Where(p => p != null)
                        .OrderByDescending(x => x.LastUsed)
                        .ToList()
                        .ForEach(Profiles.Add);

                    if (!Profiles.Any() && !loadSpecificProfile) {
                        if (File.Exists(OLDPROFILEFILEPATH)) {
                            MigrateOldProfile();
                        } else {
                            AddDefaultProfile();
                        }
                    }

                    var selectedProfile =
                        Profiles
                        .Where(p => !loadSpecificProfile || p.Id.ToString() == startWithProfileId)
                        .SkipWhile(p => !SelectProfile(p))
                        .FirstOrDefault();
                    if (selectedProfile != null) {
                        return true;
                    }
                    if (loadSpecificProfile) {
                        return false;
                    }

                    Logger.Debug("All Profiles are in use. Creating a new default profile");
                    var defaultProfile = AddDefaultProfile();
                    SelectProfile(defaultProfile);
                    return true;
                }
            }
        }

        private void Save() {
            lock (lockobj) {
                using (MyStopWatch.Measure()) {
                    try {
                        ActiveProfile.Save();
                    } catch (Exception ex) {
                        Logger.Error(ex);
                    }
                }
            }
        }

        /// <summary>
        /// Stop the timer and restart it again
        /// </summary>
        private void TryScheduleSave() {
            if (Monitor.TryEnter(lockobj, 1000)) {
                try {
                    saveTimer.Stop();
                    saveTimer.Start();
                } finally {
                    Monitor.Exit(lockobj);
                }
            }
        }

        private bool saveProfiles = true;

        public void PauseSave() {
            saveProfiles = false;
        }

        public void ResumeSave() {
            saveProfiles = true;
        }

        private void SettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (saveProfiles && e.PropertyName == "Settings") {
                System.Threading.Tasks.Task.Run(() => TryScheduleSave());
            }
            if (e.PropertyName == nameof(IProfile.Name)) {
                Profiles.Where(x => ActiveProfile.Id == x.Id).First().Name = ActiveProfile.Name;
            }
        }

        private void RegisterChangedEventHandlers() {
            this.ActiveProfile.PropertyChanged += SettingsChanged;
        }

        private void UnregisterChangedEventHandlers() {
            if (this.ActiveProfile != null) {
                this.ActiveProfile.PropertyChanged -= SettingsChanged;
            }
        }

        public bool ProfileWasSpecifiedFromCommandLineArgs { get; private set; }

        public event EventHandler LocaleChanged;

        public void ChangeLocale(CultureInfo language) {
            ActiveProfile.ApplicationSettings.Language = language;

            System.Threading.Thread.CurrentThread.CurrentUICulture = language;
            System.Threading.Thread.CurrentThread.CurrentCulture = language;

            Loc.Instance.ReloadLocale(ActiveProfile.ApplicationSettings.Culture);
            LocaleChanged?.Invoke(this, null);
        }

        public void ChangeHemisphere(Hemisphere hemisphere) {
            ActiveProfile.AstrometrySettings.HemisphereType = hemisphere;
            LocationChanged?.Invoke(this, null);
        }

        public void ChangeLatitude(double latitude) {
            var hemisphereType = ActiveProfile.AstrometrySettings.HemisphereType;
            if ((hemisphereType == Hemisphere.SOUTHERN && latitude > 0) || (hemisphereType == Hemisphere.NORTHERN && latitude < 0)) {
                latitude = -latitude;
            }
            ActiveProfile.AstrometrySettings.Latitude = latitude;
            LocationChanged?.Invoke(this, null);
        }

        public void ChangeLongitude(double longitude) {
            ActiveProfile.AstrometrySettings.Longitude = longitude;
            LocationChanged?.Invoke(this, null);
        }

        public void ChangeHorizon(string horizonFilePath) {
            ActiveProfile.AstrometrySettings.HorizonFilePath = horizonFilePath;

            try {
                ActiveProfile.AstrometrySettings.Horizon = CustomHorizon.FromFile(horizonFilePath);
            } catch (Exception ex) {
                ActiveProfile.AstrometrySettings.HorizonFilePath = string.Empty;
                Logger.Error(ex);
                Notification.ShowError(Loc.Instance["LblFailedToLoadCustomHorizon"] + ex.Message);
            }

            HorizonChanged?.Invoke(this, null);
        }

        public event EventHandler LocationChanged;

        public event EventHandler ProfileChanged;

        public event EventHandler HorizonChanged;

        public AsyncObservableCollection<ProfileMeta> Profiles { get; set; }

        public void Add() {
            AddDefaultProfile();
        }

        public bool Clone(ProfileMeta profileInfo) {
            lock (lockobj) {
                using (MyStopWatch.Measure()) {
                    if (profileFileWatcher != null) {
                        profileFileWatcher.EnableRaisingEvents = false;
                    }

                    IProfile clone = null;
                    if (profileInfo.Id == ActiveProfile.Id) {
                        clone = Profile.Clone(ActiveProfile);
                    } else {
                        try {
                            var p = Profile.Load(profileInfo.Location);
                            clone = Profile.Clone(p);
                            p.Dispose();
                        } catch (Exception) {
                            //Profile is in use
                            return false;
                        }
                    }

                    if (clone != null) {
                        clone.Save();

                        var info = new ProfileMeta() { Id = clone.Id, Name = clone.Name, Location = clone.Location };
                        Profiles.Add(info);
                        clone.Dispose();

                        if (profileFileWatcher != null) {
                            profileFileWatcher.EnableRaisingEvents = true;
                        }
                    }
                    return true;
                }
            }
        }

        private IProfile activeProfile;

        public IProfile ActiveProfile {
            get => activeProfile;
            private set {
                activeProfile = value;
                Application.Current.Resources["ActiveProfile"] = activeProfile;
                RaisePropertyChanged();
            }
        }

        public bool SelectProfile(ProfileMeta info) {
            lock (lockobj) {
                using (MyStopWatch.Measure()) {
                    try {
                        var p = Profile.Load(info.Location);

                        UnregisterChangedEventHandlers();
                        if (ActiveProfile != null) {
                            ActiveProfile.Dispose();
                            Profiles.Where(x => x.Id == ActiveProfile.Id).First().IsActive = false;
                        }

                        ActiveProfile = p;
                        info.IsActive = true;

                        System.Threading.Thread.CurrentThread.CurrentUICulture = ActiveProfile.ApplicationSettings.Language;
                        System.Threading.Thread.CurrentThread.CurrentCulture = ActiveProfile.ApplicationSettings.Language;
                        Loc.Instance.ReloadLocale(ActiveProfile.ApplicationSettings.Culture);

                        LocaleChanged?.Invoke(this, null);
                        ProfileChanged?.Invoke(this, null);
                        LocationChanged?.Invoke(this, null);
                        RegisterChangedEventHandlers();
                    } catch (Exception ex) {
                        Logger.Debug(ex.Message + Environment.NewLine + ex.StackTrace);
                        return false;
                    }
                    return true;
                }
            }
        }

        private ProfileMeta AddDefaultProfile() {
            lock (lockobj) {
                if (profileFileWatcher != null) {
                    profileFileWatcher.EnableRaisingEvents = false;
                }

                using (var p = new Profile("Default")) {
                    p.Save();

                    var info = new ProfileMeta() { Id = p.Id, Name = p.Name, Location = p.Location };
                    Profiles.Add(info);

                    if (profileFileWatcher != null) {
                        profileFileWatcher.EnableRaisingEvents = true;
                    }

                    return info;
                }
            }
        }

        public bool RemoveProfile(ProfileMeta info) {
            lock (lockobj) {
                if (!Profile.Remove(info)) {
                    return false;
                } else {
                    Profiles.Remove(info);
                    return true;
                }
            }
        }

        #region Migration

        public static string OLDPROFILEFILEPATH = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "profiles.settings");

        /// <summary>
        /// Migrate old profile.settings into new separted profile files
        /// Last active profile will get its LastUsed date to DateTime.Now to be selected first.
        /// </summary>
        private void MigrateOldProfile() {
            var s = File.ReadAllText(OLDPROFILEFILEPATH);
            s = s.Replace("NINA.Utility.Profile", "NINA.Profile");
            var tmp = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "migration.profiles");
            File.WriteAllText(tmp, s);

            using (var fs = new FileStream(tmp, FileMode.Open, FileAccess.Read)) {
                var serializer = new DataContractSerializer(typeof(Profiles));
                var obj = serializer.ReadObject(fs);
                var files = (Profiles)obj;

                foreach (Profile p in files.ProfileList) {
                    if (p.Id == files.ActiveProfileId) { p.LastUsed = DateTime.Now; }
                    p.Save();
                    var info = new ProfileMeta() { Id = p.Id, Name = p.Name, Location = p.Location, LastUsed = p.LastUsed };
                    p.Dispose();
                    Profiles.Add(info);
                }
            }
        }

        private void MigrateModularizedSolutionNamespaceChange() {
            foreach (var profileFile in Directory.GetFiles(PROFILEFOLDER)) {
                try {
                    var profile = File.ReadAllText(profileFile);
                    if (profile.Contains("http://schemas.datacontract.org/2004/07/NINA.Utility")) {
                        Logger.Info($"Migrating profile {profileFile}");
                        profile = profile.Replace("http://schemas.datacontract.org/2004/07/NINA.Model.MyFilterWheel", "http://schemas.datacontract.org/2004/07/NINA.Core.Model.Equipment")
                             .Replace("http://schemas.datacontract.org/2004/07/NINA.Model.MyCamera", "http://schemas.datacontract.org/2004/07/NINA.Core.Model.Equipment")
                             .Replace("http://schemas.datacontract.org/2004/07/NINA.Utility", "http://schemas.datacontract.org/2004/07/NINA.Core.Utility.ColorSchema");

                        var backupfolder = PROFILEFOLDER + "_old";
                        if (!Directory.Exists(backupfolder)) {
                            Directory.CreateDirectory(backupfolder);
                        }

                        // Backup old profile
                        File.Move(profileFile, Path.Combine(backupfolder, Path.GetFileName(profileFile)));
                        // Save adjusted profile
                        File.WriteAllText(profileFile, profile);
                    }
                } catch (Exception ex) {
                    Logger.Error($"Failed to migrate profile {profileFile} due to ", ex);
                }
            }
        }

        #endregion Migration

        public static void ActivateInstanceOfNinaReferencingProfile(string startWithProfileId) {
            using (var waitHandle = new EventWaitHandle(false,

                EventResetMode.ManualReset,

                "NINA_ActivateInstance:" + startWithProfileId)) {
                waitHandle.Set();
            }
        }

        public static System.Threading.Tasks.Task ActivateInstanceWatcher(
            IProfileService profileService,
            Window mainWindow
            ) {
            var currentProfile = profileService.ActiveProfile.Id.ToString();
            return System.Threading.Tasks.Task.Factory.StartNew(
                () => {
                    var profileChanged = new ManualResetEventSlim();
                    profileService.ProfileChanged += (x, y) => profileChanged.Set();

                    var activated = new EventWaitHandle(false,
                        EventResetMode.AutoReset,
                        "NINA_ActivateInstance:" + currentProfile);

                    while (true) {
                        var handles = new WaitHandle[] {
                            profileChanged.WaitHandle,
                            activated };
                        var response = WaitHandle.WaitAny(handles, TimeSpan.FromSeconds(1));
                        if (258 == response) // timeout
                        {
                            continue;
                        }

                        if (handles[response] == profileChanged.WaitHandle) {
                            profileChanged.Reset();
                            activated.Dispose();
                            handles[1] = activated = new EventWaitHandle(false,
                                EventResetMode.AutoReset,
                                "NINA_ActivateInstance:" +
                                    profileService.ActiveProfile.Id.ToString());
                        }
                        if (handles[response] == activated) {
                            mainWindow.Dispatcher.Invoke(
                                    () => mainWindow.Activate());
                        }
                    }
                }, System.Threading.Tasks.TaskCreationOptions.LongRunning);
        }
    }
}