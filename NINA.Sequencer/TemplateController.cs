#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Accord.Math;
using NINA.Core.Model;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.DragDrop;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Serialization;
using NINA.Core.Utility;
using NINA.Astrometry;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using NINA.Core.Utility.Notification;
using NINA.Core.Locale;

namespace NINA.Sequencer {

    public class TemplateController {
        private readonly SequenceJsonConverter sequenceJsonConverter;
        private readonly IProfileService profileService;
        private readonly string defaultTemplatePath;
        private FileSystemWatcher sequenceTemplateFolderWatcher;
        private string userTemplatePath;
        public const string DefaultTemplatesGroup = "LblTemplate_DefaultTemplates";
        private const string UserTemplatesGroup = "LblTemplate_UserTemplates";
        public const string TemplateFileExtension = ".template.json";

        public IList<TemplatedSequenceContainer> UserTemplates => Templates.Where(t => t.Group == UserTemplatesGroup).ToList();

        public IList<TemplatedSequenceContainer> Templates { get; }

        private CollectionViewSource templatesView;
        private CollectionViewSource templatesMenuView;
        public ICollectionView TemplatesView { get => templatesView.View; }
        public ICollectionView TemplatesMenuView { get => templatesMenuView.View; }

        private string viewFilter = string.Empty;

        public string ViewFilter {
            get => viewFilter;
            set {
                viewFilter = value;
                TemplatesView.Refresh();
            }
        }

        public TemplateController(SequenceJsonConverter sequenceJsonConverter, IProfileService profileService) {
            this.sequenceJsonConverter = sequenceJsonConverter;
            this.profileService = profileService;
            defaultTemplatePath = Path.Combine(NINA.Core.Utility.CoreUtil.APPLICATIONDIRECTORY, "Sequencer", "Examples");

            Templates = new List<TemplatedSequenceContainer>();
            try {
                if (!Directory.Exists(defaultTemplatePath)) {
                    Directory.CreateDirectory(defaultTemplatePath);
                }
                foreach (var file in Directory.GetFiles(defaultTemplatePath, "*" + TemplateFileExtension)) {
                    try {
                        var container = sequenceJsonConverter.Deserialize(File.ReadAllText(file)) as ISequenceContainer;
                        if (container is ISequenceRootContainer) continue;
                        Templates.Add(new TemplatedSequenceContainer(profileService, DefaultTemplatesGroup, container));
                    } catch (Exception ex) {
                        Logger.Error("Invalid template JSON", ex);
                    }
                }
            } catch (Exception ex) {
                Logger.Error("Error occurred while loading default templates", ex);
            }

            templatesView = new CollectionViewSource { Source = Templates };
            TemplatesView.GroupDescriptions.Add(new PropertyGroupDescription("GroupTranslated"));
            TemplatesView.SortDescriptions.Add(new SortDescription("GroupTranslated", ListSortDirection.Ascending));
            TemplatesView.SortDescriptions.Add(new SortDescription("Container.Name", ListSortDirection.Ascending));
            TemplatesView.Filter += new Predicate<object>(ApplyViewFilter);

            templatesMenuView = new CollectionViewSource { Source = Templates };
            TemplatesMenuView.SortDescriptions.Add(new SortDescription("Container.Name", ListSortDirection.Ascending));

            LoadUserTemplates();

            sequenceTemplateFolderWatcher = new FileSystemWatcher(profileService.ActiveProfile.SequenceSettings.SequencerTemplatesFolder, "*" + TemplateFileExtension);
            sequenceTemplateFolderWatcher.Changed += SequenceTemplateFolderWatcher_Changed;
            sequenceTemplateFolderWatcher.Deleted += SequenceTemplateFolderWatcher_Changed;
            sequenceTemplateFolderWatcher.IncludeSubdirectories = true;
            sequenceTemplateFolderWatcher.EnableRaisingEvents = true;

            profileService.ProfileChanged += ProfileService_ProfileChanged;
            profileService.ActiveProfile.SequenceSettings.PropertyChanged += SequenceSettings_SequencerTemplatesFolderChanged;
        }

        private bool ApplyViewFilter(object obj) {
            return (obj as TemplatedSequenceContainer).Container.Name.IndexOf(ViewFilter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void SequenceTemplateFolderWatcher_Changed(object sender, FileSystemEventArgs e) {
            LoadUserTemplates();
        }

        private void SequenceSettings_SequencerTemplatesFolderChanged(object sender, System.EventArgs e) {
            if ((e as PropertyChangedEventArgs)?.PropertyName == nameof(profileService.ActiveProfile.SequenceSettings.SequencerTemplatesFolder)) {
                sequenceTemplateFolderWatcher.Path = profileService.ActiveProfile.SequenceSettings.SequencerTemplatesFolder;
                LoadUserTemplates();
            }
        }

        private void ProfileService_ProfileChanged(object sender, System.EventArgs e) {
            profileService.ActiveProfile.SequenceSettings.PropertyChanged += SequenceSettings_SequencerTemplatesFolderChanged;
            LoadUserTemplates();
        }

        private void LoadUserTemplates() {
            try {
                userTemplatePath = profileService.ActiveProfile.SequenceSettings.SequencerTemplatesFolder;
                var rootParts = userTemplatePath.Split(new char[] { Path.DirectorySeparatorChar }, System.StringSplitOptions.RemoveEmptyEntries);

                if (!Directory.Exists(userTemplatePath)) {
                    Directory.CreateDirectory(userTemplatePath);
                }

                foreach (var template in Templates.Where(t => t.Group != DefaultTemplatesGroup).ToList()) {
                    Application.Current.Dispatcher.Invoke(() => Templates.Remove(template));
                }

                foreach (var file in Directory.GetFiles(userTemplatePath, "*" + TemplateFileExtension, SearchOption.AllDirectories)) {
                    try {
                        var container = sequenceJsonConverter.Deserialize(File.ReadAllText(file));
                        if (container is ISequenceRootContainer) continue;
                        var template = new TemplatedSequenceContainer(profileService, UserTemplatesGroup, container);
                        var fileInfo = new FileInfo(file);
                        container.Name = fileInfo.Name.Replace(TemplateFileExtension, "");
                        var parts = fileInfo.Directory.FullName.Split(new char[] { Path.DirectorySeparatorChar }, System.StringSplitOptions.RemoveEmptyEntries);
                        template.SubGroups = parts.Except(rootParts).ToArray();
                        Templates.Add(template);
                    } catch (Exception ex) {
                        Logger.Error("Invalid template JSON", ex);
                    }
                }

                Application.Current.Dispatcher.Invoke(() => {
                    try {
                        TemplatesView.Refresh();
                        TemplatesMenuView.Refresh();
                    } catch (Exception ex) {
                        Logger.Error(ex);
                    }
                });
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(Loc.Instance["Lbl_SequenceTemplateController_LoadUserTemplatesFailed"]);
            }
        }

        public void AddNewUserTemplate(ISequenceContainer sequenceContainer) {
            try {
                if (sequenceContainer is IDeepSkyObjectContainer) {
                    var dso = (sequenceContainer as IDeepSkyObjectContainer);
                    dso.Target.TargetName = string.Empty;
                    dso.Target.InputCoordinates.Coordinates = new Coordinates(Angle.Zero, Angle.Zero, Epoch.J2000);
                    dso.Target.Rotation = 0;
                    dso.Target = dso.Target;
                }

                var jsonContainer = sequenceJsonConverter.Serialize(sequenceContainer);
                File.WriteAllText(Path.Combine(userTemplatePath, GetTemplateFileName(sequenceContainer)), jsonContainer);
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(Loc.Instance["Lbl_SequenceTemplateController_AddNewTemplateFailed"]);
            }
        }

        public void DeleteUserTemplate(TemplatedSequenceContainer sequenceContainer) {
            try {
                if (sequenceContainer == null) return;
                File.Delete(Path.Combine(userTemplatePath, Path.Combine(sequenceContainer.SubGroups), GetTemplateFileName(sequenceContainer.Container)));
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(Loc.Instance["Lbl_SequenceTemplateController_DeleteTemplateFailed"]);
            }
        }

        private string GetTemplateFileName(ISequenceContainer container) {
            return NINA.Core.Utility.CoreUtil.ReplaceAllInvalidFilenameChars(container.Name) + TemplateFileExtension;
        }
    }

    public class TemplatedSequenceContainer : IDroppable {

        public TemplatedSequenceContainer(IProfileService profileService, string group, ISequenceContainer container) {
            Group = group;
            Container = container;
            SubGroups = new string[0];
            this.profileService = profileService;
        }

        public string GroupTranslated => Loc.Instance[Group] + " › " + (SubGroups.Count() > 0 ? $"{string.Join(" › ", SubGroups)}" : "Base");

        public string Group { get; }
        public string[] SubGroups { get; set; }

        private IProfileService profileService;

        public ISequenceContainer Container { get; }

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

        public ISequenceItem Clone() {
            var clone = (ISequenceContainer)Container.Clone();
            if (profileService.ActiveProfile.SequenceSettings.CollapseSequencerTemplatesByDefault) {
                clone.IsExpanded = false;
            }
            return clone;
        }

        public override string ToString() {
            return this.Container.Name;
        }
    }
}