#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Model;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.DragDrop;
using NINA.Sequencer.Serialization;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using NINA.Core.Utility.Notification;
using NINA.Core.Locale;
using OxyPlot.Axes;

namespace NINA.Sequencer {

    public class TargetController : BaseINPC {
        private readonly SequenceJsonConverter sequenceJsonConverter;
        private readonly IProfileService profileService;
        private string targetPath;
        private FileSystemWatcher sequenceTargetsFolderWatcher;
        public const string TargetsFileExtension = ".json";

        public IList<TargetSequenceContainer> Targets { get; }

        private CollectionViewSource targetsView;
        private CollectionViewSource targetsMenuView;
        public ICollectionView TargetsView { get => targetsView.View; }
        public ICollectionView TargetsMenuView { get => targetsMenuView.View; }

        private string viewFilter = string.Empty;
        private ISequenceSettings activeSequenceSettings;

        public string ViewFilter {
            get => viewFilter;
            set {
                viewFilter = value;
                TargetsView.Refresh();
            }
        }

        public TargetController(SequenceJsonConverter sequenceJsonConverter, IProfileService profileService) {
            this.sequenceJsonConverter = sequenceJsonConverter;
            this.profileService = profileService;

            Targets = new List<TargetSequenceContainer>();

            targetsView = new CollectionViewSource { Source = Targets };
            targetsView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(TargetSequenceContainer.Grouping)));
            TargetsView.SortDescriptions.Add(new SortDescription(nameof(TargetSequenceContainer.Weight), ListSortDirection.Ascending));
            TargetsView.Filter += new Predicate<object>(ApplyViewFilter);
            SortByRelevance = true;

            targetsMenuView = new CollectionViewSource { Source = Targets };
            TargetsMenuView.SortDescriptions.Add(new SortDescription(nameof(TargetSequenceContainer.Name), ListSortDirection.Ascending));

            LoadTargets();

            ToggleSortCommand = new RelayCommand(x => {
                if (SortByRelevance) {
                    TargetsView.SortDescriptions.RemoveAt(0);
                    TargetsView.SortDescriptions.Add(new SortDescription(nameof(TargetSequenceContainer.Weight), ListSortDirection.Ascending));
                } else if (SortByName) {
                    TargetsView.SortDescriptions.RemoveAt(0);
                    TargetsView.SortDescriptions.Add(new SortDescription(nameof(TargetSequenceContainer.Name), ListSortDirection.Ascending));
                }
            });

            sequenceTargetsFolderWatcher = new FileSystemWatcher(profileService.ActiveProfile.SequenceSettings.SequencerTargetsFolder, "*" + TargetsFileExtension);

            sequenceTargetsFolderWatcher.Created += SequenceTargetsFolderWatcher_Changed;
            sequenceTargetsFolderWatcher.Changed += SequenceTargetsFolderWatcher_Changed;
            sequenceTargetsFolderWatcher.Deleted += SequenceTargetsFolderWatcher_Changed;
            sequenceTargetsFolderWatcher.IncludeSubdirectories = true;
            sequenceTargetsFolderWatcher.EnableRaisingEvents = true;

            profileService.ProfileChanged += ProfileService_ProfileChanged;
            activeSequenceSettings = profileService.ActiveProfile.SequenceSettings;
            activeSequenceSettings.PropertyChanged += SequenceSettings_SequencerTargetsFolderChanged;
        }

        private bool sortByRelevance;

        public bool SortByRelevance {
            get => sortByRelevance;
            set {
                sortByRelevance = value;
                if (value) {
                    SortByName = false;
                }
                RaisePropertyChanged();
            }
        }

        private bool sortByName;

        public bool SortByName {
            get => sortByName;
            set {
                sortByName = value;
                if (value) {
                    SortByRelevance = false;
                }
                RaisePropertyChanged();
            }
        }

        public ICommand ToggleSortCommand { get; }

        private bool ApplyViewFilter(object obj) {
            return (obj as TargetSequenceContainer).Name.IndexOf(ViewFilter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void SequenceTargetsFolderWatcher_Changed(object sender, FileSystemEventArgs e) {
            LoadTargets();
        }

        private void SequenceSettings_SequencerTargetsFolderChanged(object sender, System.EventArgs e) {
            if ((e as PropertyChangedEventArgs)?.PropertyName == nameof(profileService.ActiveProfile.SequenceSettings.SequencerTargetsFolder)) {
                sequenceTargetsFolderWatcher.Path = profileService.ActiveProfile.SequenceSettings.SequencerTargetsFolder;
                LoadTargets();
            }
        }

        private void ProfileService_ProfileChanged(object sender, System.EventArgs e) {
            activeSequenceSettings.PropertyChanged -= SequenceSettings_SequencerTargetsFolderChanged;
            activeSequenceSettings = profileService.ActiveProfile.SequenceSettings;
            activeSequenceSettings.PropertyChanged += SequenceSettings_SequencerTargetsFolderChanged;
            LoadTargets();
        }

        public void AddTarget(IDeepSkyObjectContainer deepSkyObjectContainer) {
            try {
                var jsonContainer = sequenceJsonConverter.Serialize(deepSkyObjectContainer);
                File.WriteAllText(Path.Combine(targetPath, NINA.Core.Utility.CoreUtil.ReplaceAllInvalidFilenameChars(deepSkyObjectContainer.Name) + ".json"), jsonContainer);

                var existingTarget = Targets.FirstOrDefault(x => x.Name == deepSkyObjectContainer.Name);
                if (existingTarget != null) {
                    Targets.Remove(existingTarget);
                    Targets.Add(new TargetSequenceContainer(profileService, deepSkyObjectContainer));
                } else {
                    Targets.Add(new TargetSequenceContainer(profileService, deepSkyObjectContainer));
                }

                RefreshFilters();
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(Loc.Instance["Lbl_SequenceTargetController_AddNewTargetFailed"]);
            }
        }

        private void LoadTargets() {
            try {
                targetPath = profileService.ActiveProfile.SequenceSettings.SequencerTargetsFolder;
                var rootParts = targetPath.Split(new char[] { Path.DirectorySeparatorChar }, System.StringSplitOptions.RemoveEmptyEntries);

                if (!Directory.Exists(targetPath)) {
                    Directory.CreateDirectory(targetPath);
                }

                foreach (var target in Targets.ToList()) {
                    Application.Current.Dispatcher.Invoke(() => Targets.Remove(target));
                }

                foreach (var file in Directory.GetFiles(targetPath, "*" + TargetsFileExtension, SearchOption.AllDirectories)) {
                    try {
                        var container = sequenceJsonConverter.Deserialize(File.ReadAllText(file));

                        var dsoContainer = container as IDeepSkyObjectContainer;
                        if (dsoContainer != null) {
                            var target = new TargetSequenceContainer(profileService, dsoContainer);
                            var fileInfo = new FileInfo(file);
                            container.Name = fileInfo.Name.Replace(TargetsFileExtension, "");
                            var parts = fileInfo.Directory.FullName.Split(new char[] { Path.DirectorySeparatorChar }, System.StringSplitOptions.RemoveEmptyEntries);
                            target.SubGroups = parts.Except(rootParts).ToArray();

                            Targets.Add(target);
                        }
                    } catch (Exception ex) {
                        Logger.Error($"Invalid target JSON {file}", ex);
                    }
                }
                RefreshFilters();
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(Loc.Instance["Lbl_SequenceTargetController_LoadUserTargetFailed"]);
            }
        }

        private void RefreshFilters() {
            Application.Current.Dispatcher.Invoke(() => {
                try {
                    TargetsView.Refresh(); TargetsMenuView.Refresh();
                } catch (Exception ex) {
                    Logger.Error(ex);
                }
            });
        }

        public void DeleteTarget(TargetSequenceContainer targetSequenceContainer) {
            try {
                if (targetSequenceContainer == null) return;

                var file = Path.Combine(targetPath, Path.Combine(targetSequenceContainer.SubGroups), CoreUtil.ReplaceAllInvalidFilenameChars(targetSequenceContainer.Name) + ".json");
                if (File.Exists(file)) {
                    File.Delete(file);
                }

                Targets.Remove(targetSequenceContainer);

                RefreshFilters();
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(Loc.Instance["Lbl_SequenceTargetController_DeleteTargetFailed"]);
            }
        }
    }

    public class TargetSequenceContainer : IDroppable {

        public TargetSequenceContainer(IProfileService profileService, IDeepSkyObjectContainer container) {
            Container = container;
            SubGroups = new string[0];
            this.profileService = profileService;
        }

        public string Grouping => (SubGroups.Count() > 0 ? $"{string.Join(" › ", SubGroups)}" : "Base");
        public string[] SubGroups { get; set; }

        public string Name { get => Container.Name; }

        /// <summary>
        /// The weight is calculated based on the following parameters
        /// - Distance of the max altitude from the middle of the night
        /// - Maximum altitude normalized
        /// -> Weight = Sum of (distance * 2) and max altitude
        /// </summary>
        public double Weight {
            get {
                var sunSet = Container.NighttimeData.SunRiseAndSet.Set;
                var sunRise = Container.NighttimeData.SunRiseAndSet.Rise;

                if (sunSet.HasValue && sunRise.HasValue) {
                    var substract = sunRise.Value - sunSet.Value;
                    var middle = sunSet.Value.AddSeconds(substract.TotalSeconds / 2d);

                    var middleNight = DateTimeAxis.ToDouble(middle);
                    var maxAlt = Container.Target.DeepSkyObject.MaxAltitude;

                    var timeAtMax = maxAlt.X;

                    var distanceWeight = Math.Abs(middleNight - timeAtMax);

                    var altitudeWeight = 1 - ((maxAlt.Y - (-90)) / (90 - (-90)));

                    return (2 * distanceWeight + altitudeWeight) / 3;
                } else {
                    return 1;
                }
            }
        }

        private static readonly DateTime TimeOrigin = new DateTime(1899, 12, 31, 0, 0, 0, DateTimeKind.Utc);

        public IDeepSkyObjectContainer Container { get; }

        private IProfileService profileService;

        public ISequenceContainer Parent => null;

        public ICommand DetachCommand => null;

        public ICommand MoveUpCommand => null;

        public ICommand MoveDownCommand => null;

        public void AfterParentChanged() {
        }

        public void AttachNewParent(ISequenceContainer newParent) {
        }

        public void Detach() {
        }

        public void MoveDown() {
        }

        public void MoveUp() {
        }

        public IDeepSkyObjectContainer Clone() {
            var clone = (IDeepSkyObjectContainer)Container.Clone();
            if (profileService.ActiveProfile.SequenceSettings.CollapseSequencerTemplatesByDefault) {
                clone.IsExpanded = false;
            }
            return clone;
        }
    }
}