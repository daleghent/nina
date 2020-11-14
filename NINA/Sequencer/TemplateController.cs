using Accord.Math;
using NINA.Model;
using NINA.Profile;
using NINA.Properties;
using NINA.Sequencer.Container;
using NINA.Sequencer.DragDrop;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Serialization;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Notification;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace NINA.Sequencer {

    public class TemplateController {
        private readonly SequenceJsonConverter sequenceJsonConverter;
        private readonly IProfileService profileService;
        private readonly string defaultTemplatePath;
        private FileSystemWatcher sequenceTemplateFolderWatcher;
        private string userTemplatePath;
        public const string DefaultTemplatesGroup = nameof(Locale.Locale.LblTemplate_DefaultTemplates);
        private const string UserTemplatesGroup = nameof(Locale.Locale.LblTemplate_UserTemplates);
        public const string TemplateFileExtension = ".template.json";

        public IList<TemplatedSequenceContainer> UserTemplates => Templates.Where(t => t.Group == UserTemplatesGroup).ToList();

        public IList<TemplatedSequenceContainer> Templates { get; }

        public ICollectionView TemplatesView { get; set; }
        public ICollectionView TemplatesMenuView { get; set; }

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
            defaultTemplatePath = Path.Combine(NINA.Utility.Utility.APPLICATIONDIRECTORY, "Sequencer", "Examples");

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

            TemplatesView = new CollectionViewSource { Source = Templates }.View;
            TemplatesView.GroupDescriptions.Add(new PropertyGroupDescription("GroupTranslated"));
            TemplatesView.SortDescriptions.Add(new SortDescription("GroupTranslated", ListSortDirection.Ascending));
            TemplatesView.SortDescriptions.Add(new SortDescription("Container.Name", ListSortDirection.Ascending));
            TemplatesView.Filter += new Predicate<object>(ApplyViewFilter);

            TemplatesMenuView = new CollectionViewSource { Source = Templates }.View;
            TemplatesMenuView.SortDescriptions.Add(new SortDescription("Container.Name", ListSortDirection.Ascending));

            LoadUserTemplates();

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

                if (!Directory.Exists(userTemplatePath)) {
                    Directory.CreateDirectory(userTemplatePath);
                }

                if (sequenceTemplateFolderWatcher == null) {
                    sequenceTemplateFolderWatcher = new FileSystemWatcher(profileService.ActiveProfile.SequenceSettings.SequencerTemplatesFolder, "*" + TemplateFileExtension);
                    sequenceTemplateFolderWatcher.Changed += SequenceTemplateFolderWatcher_Changed;
                    sequenceTemplateFolderWatcher.Deleted += SequenceTemplateFolderWatcher_Changed;
                    sequenceTemplateFolderWatcher.IncludeSubdirectories = true;
                    sequenceTemplateFolderWatcher.EnableRaisingEvents = true;
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
                        template.SubGroups = fileInfo.Directory.FullName.Replace(userTemplatePath, "").Split(new char[] { Path.DirectorySeparatorChar }, System.StringSplitOptions.RemoveEmptyEntries);
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
                Notification.ShowError(Locale.Loc.Instance["Lbl_SequenceTemplateController_LoadUserTemplatesFailed"]);
            }
        }

        public void AddNewUserTemplate(ISequenceContainer sequenceContainer) {
            try {
                if (sequenceContainer is IDeepSkyObjectContainer) {
                    var dso = (sequenceContainer as IDeepSkyObjectContainer);
                    dso.Target.TargetName = string.Empty;
                    dso.Target.InputCoordinates.Coordinates = new NINA.Utility.Astrometry.Coordinates(Angle.Zero, Angle.Zero, Epoch.J2000);
                    dso.Target.Rotation = 0;
                    dso.Target = dso.Target;
                }

                var jsonContainer = sequenceJsonConverter.Serialize(sequenceContainer);
                File.WriteAllText(Path.Combine(userTemplatePath, GetTemplateFileName(sequenceContainer)), jsonContainer);
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(Locale.Loc.Instance["Lbl_SequenceTemplateController_AddNewTemplateFailed"]);
            }
        }

        public void DeleteUserTemplate(TemplatedSequenceContainer sequenceContainer) {
            try {
                if (sequenceContainer == null) return;
                File.Delete(Path.Combine(userTemplatePath, Path.Combine(sequenceContainer.SubGroups), GetTemplateFileName(sequenceContainer.Container)));
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(Locale.Loc.Instance["Lbl_SequenceTemplateController_DeleteTemplateFailed"]);
            }
        }

        private string GetTemplateFileName(ISequenceContainer container) {
            return container.Name + TemplateFileExtension;
        }
    }

    public class TemplatedSequenceContainer : IDroppable {

        public TemplatedSequenceContainer(IProfileService profileService, string group, ISequenceContainer container) {
            Group = group;
            Container = container;
            SubGroups = new string[0];
            this.profileService = profileService;
        }

        public string GroupTranslated => Locale.Loc.Instance[Group] + " › " + (SubGroups.Count() > 0 ? $"{string.Join(" › ", SubGroups)}" : "Base");

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
    }
}