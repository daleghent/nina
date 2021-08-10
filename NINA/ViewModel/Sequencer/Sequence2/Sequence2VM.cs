#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Accord.IO;
using NINA.Core.Enum;
using NINA.Equipment.Equipment.MyPlanetarium;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.DragDrop;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Sequencer.Serialization;
using NINA.Sequencer.Trigger;
using NINA.Utility;
using NINA.Astrometry;
using NINA.ViewModel.FramingAssistant;
using NINA.ViewModel.ImageHistory;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shell;
using System.Windows.Threading;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Core.Utility;
using NINA.Core.MyMessageBox;
using NINA.Core.Locale;
using NINA.Core.Utility.Notification;
using NINA.Core.Model;
using NINA.Astrometry.Interfaces;
using NINA.Equipment.Interfaces;
using NINA.WPF.Base.ViewModel;
using NINA.WPF.Base.Interfaces.ViewModel;

namespace NINA.ViewModel.Sequencer {

    internal class Sequence2VM : SequencerBaseVM, ISequence2VM {
        private IApplicationStatusMediator applicationStatusMediator;
        private ISequenceMediator sequenceMediator;
        private ICameraMediator cameraMediator;
        private DispatcherTimer validationTimer;

        public Sequence2VM(
            IProfileService profileService,
            ISequenceMediator sequenceMediator,
            IApplicationStatusMediator applicationStatusMediator,
            ICameraMediator cameraMediator,
            ISequencerFactory factory

            ) : base(profileService) {
            this.applicationStatusMediator = applicationStatusMediator;

            this.sequenceMediator = sequenceMediator;
            this.cameraMediator = cameraMediator;

            SequencerFactory = factory;

            StartSequenceCommand = new AsyncCommand<bool>(StartSequence, (object o) => cameraMediator.IsFreeToCapture(this));
            CancelSequenceCommand = new RelayCommand(CancelSequence);
            SaveAsSequenceCommand = new RelayCommand(SaveAsSequence);
            SaveSequenceCommand = new RelayCommand(SaveSequence);
            AddTemplateCommand = new RelayCommand(AddTemplate);
            AddTargetToControllerCommand = new RelayCommand(AddTargetToController);
            LoadSequenceCommand = new RelayCommand(LoadSequence);
            SwitchToOverviewCommand = new RelayCommand((object o) => sequenceMediator.SwitchToOverview(), (object o) => !profileService.ActiveProfile.SequenceSettings.DisableSimpleSequencer);

            DetachCommand = new RelayCommand((o) => {
                var source = (o as DropIntoParameters)?.Source;
                source?.Detach();
                if (source != null) {
                    if (source is TemplatedSequenceContainer) {
                        var result = MyMessageBox.Show(string.Format(Loc.Instance["LblTemplate_DeleteTemplateMessageBox_Text"], (source as TemplatedSequenceContainer).Container.Name),
                            Loc.Instance["LblTemplate_DeleteTemplateMessageBox_Caption"], System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);
                        if (result == System.Windows.MessageBoxResult.OK) {
                            TemplateController.DeleteUserTemplate(source as TemplatedSequenceContainer);
                        }
                    }
                    if (source is TargetSequenceContainer) {
                        var result = MyMessageBox.Show(string.Format(Loc.Instance["Lbl_Sequencer_TargetSidebar_DeleteTargetMessageBox_Text"], (source as TargetSequenceContainer).Name),
                            Loc.Instance["Lbl_Sequencer_TargetSidebar_DeleteTargetMessageBox_Caption"], System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxResult.No);
                        if (result == System.Windows.MessageBoxResult.Yes) {
                            TargetController.DeleteTarget(source as TargetSequenceContainer);
                        }
                    }
                }
            });
            SkipCurrentItemCommand = new AsyncCommand<bool>(SkipCurrentItem);
            SkipToEndOfSequenceCommand = new AsyncCommand<bool>(SkipToEndOfSequence);
        }

        public Task Initialize() {
            return Task.Run(async () => {
                await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() => {
                    SequenceJsonConverter = new SequenceJsonConverter(SequencerFactory);
                    TemplateController = new TemplateController(SequenceJsonConverter, profileService);
                    TargetController = new TargetController(SequenceJsonConverter, profileService);

                    var rootContainer = SequencerFactory.GetContainer<SequenceRootContainer>();
                    rootContainer.Name = Loc.Instance["LblAdvancedSequenceTitle"];
                    rootContainer.Add(SequencerFactory.GetContainer<StartAreaContainer>());
                    rootContainer.Add(SequencerFactory.GetContainer<TargetAreaContainer>());
                    rootContainer.Add(SequencerFactory.GetContainer<EndAreaContainer>());
                    rootContainer.ClearHasChanged();
                    Sequencer = new NINA.Sequencer.Sequencer(
                        rootContainer
                    );

                    validationTimer = new DispatcherTimer(DispatcherPriority.Background);
                    validationTimer.Interval = TimeSpan.FromSeconds(5);
                    validationTimer.IsEnabled = true;
                    validationTimer.Tick += (sender, args) => Sequencer.MainContainer.Validate();
                    validationTimer.Start();

                    if (File.Exists(profileService.ActiveProfile.SequenceSettings.StartupSequenceTemplate)) {
                        try {
                            LoadSequenceFromFile(profileService.ActiveProfile.SequenceSettings.StartupSequenceTemplate);
                            SavePath = string.Empty;
                        } catch (Exception ex) {
                            Logger.Error("Startup Sequence failed to load", ex);
                        }
                    }
                }));
            });
        }

        private Task<bool> SkipCurrentItem(object arg) {
            Sequencer.MainContainer.SkipCurrentRunningItems();
            return Task.FromResult(true);
        }

        private async Task<bool> SkipToEndOfSequence(object arg) {
            var startContainer = Sequencer.MainContainer.Items[0] as ISequenceContainer;
            var targetContainer = Sequencer.MainContainer.Items[1] as ISequenceContainer;
            if (startContainer.Status == SequenceEntityStatus.RUNNING) {
                await startContainer.Interrupt();
                await Task.Delay(100);
            }
            if (targetContainer.Status == SequenceEntityStatus.RUNNING) {
                await targetContainer.Interrupt();
            }
            return true;
        }

        private void AddTargetToController(object obj) {
            var original = ((obj as DropIntoParameters).Source as IDeepSkyObjectContainer);
            IDeepSkyObjectContainer clonedContainer = original.Clone() as IDeepSkyObjectContainer;

            if (clonedContainer == null) { return; }

            if (TargetController.Targets.Any(t => t.Name == clonedContainer.Name)) {
                if (MyMessageBox.Show(
                    string.Format(Loc.Instance["Lbl_Sequencer_TargetSidebar_OverwriteMessageBox_Text"], clonedContainer.Name),
                    Loc.Instance["Lbl_Sequencer_TargetSidebar_OverwriteMessageBox_Caption"], System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxResult.No
                  ) == System.Windows.MessageBoxResult.Yes) {
                    TargetController.AddTarget(clonedContainer);
                    Notification.ShowSuccess(string.Format(Loc.Instance["Lbl_Sequencer_TargetSidebar_Updated"], clonedContainer.Name));
                }
            } else {
                TargetController.AddTarget(clonedContainer);
                Notification.ShowSuccess(string.Format(Loc.Instance["Lbl_Sequencer_TargetSidebar_Created"], clonedContainer.Name));
            }
        }

        private void AddTemplate(object o) {
            ISequenceContainer clonedContainer = ((o as DropIntoParameters).Source as ISequenceContainer).Clone() as ISequenceContainer;
            if (clonedContainer == null || clonedContainer is ISequenceRootContainer || clonedContainer is IImmutableContainer) return;
            clonedContainer.AttachNewParent(null);
            clonedContainer.ResetAll();

            bool addTemplate = true;
            var templateExists = TemplateController.UserTemplates.Any(t => t.Container.Name == clonedContainer.Name && t.SubGroups.Count() == 0);
            if (templateExists) {
                var result = MyMessageBox.Show(string.Format(Loc.Instance["LblTemplate_OverwriteTemplateMessageBox_Text"], clonedContainer.Name),
                    Loc.Instance["LblTemplate_OverwriteTemplateMessageBox_Caption"], System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);
                addTemplate = result == System.Windows.MessageBoxResult.OK;
            }

            if (addTemplate) {
                TemplateController.AddNewUserTemplate(clonedContainer);
                if (templateExists) {
                    Notification.ShowSuccess(string.Format(Loc.Instance["LblTemplate_Updated"], clonedContainer.Name));
                } else {
                    Notification.ShowSuccess(string.Format(Loc.Instance["LblTemplate_Created"], clonedContainer.Name));
                }
            }
        }

        private void LoadSequence(object obj) {
            if (Sequencer.MainContainer.AskHasChanged(SavePath ?? "")) {
                return;
            }
            var initialDirectory = string.Empty;
            if (Directory.Exists(profileService.ActiveProfile.SequenceSettings.DefaultSequenceFolder)) {
                initialDirectory = Path.GetFullPath(profileService.ActiveProfile.SequenceSettings.DefaultSequenceFolder);
            }
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Multiselect = false;
            dialog.Title = Loc.Instance["LblLoad"];
            if (string.IsNullOrEmpty(SavePath) || Path.GetExtension(SavePath) != ".json") {
                dialog.InitialDirectory = initialDirectory;
                dialog.FileName = "";
            } else {
                dialog.InitialDirectory = Path.GetDirectoryName(SavePath);
                dialog.FileName = Path.GetFileName(SavePath);
            }
            dialog.DefaultExt = "json";
            dialog.Filter = "N.I.N.A. sequence JSON|*." + dialog.DefaultExt;

            if (dialog.ShowDialog() == true) {
                LoadSequenceFromFile(dialog.FileName);
            }
        }

        private void LoadSequenceFromFile(string file) {
            try {
                var json = File.ReadAllText(file);
                var container = SequenceJsonConverter.Deserialize(json) as ISequenceRootContainer;
                if (container != null) {
                    SavePath = file;
                    Sequencer.MainContainer = container;
                    Sequencer.MainContainer.Validate();
                    Sequencer.MainContainer.ClearHasChanged();
                    SavePath = file;
                } else {
                    Logger.Error("Unable to load sequence - Sequencer root element must be sequence root container!");
                    Notification.ShowError(Loc.Instance["Lbl_Sequencer_RootElementMustBeRootContainer"]);
                }
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(Loc.Instance["Lbl_Sequencer_UnableToDeserializeJSON"]);
            }
        }

        private void SaveAsSequence(object arg) {
            try {
                var initialDirectory = string.Empty;
                if (Directory.Exists(profileService.ActiveProfile.SequenceSettings.DefaultSequenceFolder)) {
                    initialDirectory = Path.GetFullPath(profileService.ActiveProfile.SequenceSettings.DefaultSequenceFolder);
                }
                Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
                dialog.Title = Loc.Instance["LblSave"];
                if (string.IsNullOrEmpty(SavePath) || Path.GetExtension(SavePath) != ".json") {
                    dialog.InitialDirectory = initialDirectory;
                    dialog.FileName = Sequencer.MainContainer.Name;
                } else {
                    dialog.InitialDirectory = Path.GetDirectoryName(SavePath);
                    dialog.FileName = Path.GetFileName(SavePath);
                }
                dialog.DefaultExt = "json";
                dialog.Filter = "N.I.N.A. sequence JSON|*." + dialog.DefaultExt;
                dialog.OverwritePrompt = true;

                if (dialog.ShowDialog().Value) {
                    var json = SequenceJsonConverter.Serialize(Sequencer.MainContainer);
                    File.WriteAllText(dialog.FileName, json);
                    SavePath = dialog.FileName;
                    Sequencer.MainContainer.ClearHasChanged();
                }
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(string.Format(Loc.Instance["Lbl_Sequencer_SaveSequence_FailureNotification"], Sequencer.MainContainer.Name, ex.Message));
            }
        }

        private void SaveSequence(object arg) {
            if (string.IsNullOrEmpty(SavePath)) {
                SaveAsSequence(arg);
            } else {
                try {
                    var json = SequenceJsonConverter.Serialize(Sequencer.MainContainer);
                    File.WriteAllText(SavePath, json);
                    Sequencer.MainContainer.ClearHasChanged();
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(string.Format(Loc.Instance["Lbl_Sequencer_SaveSequence_FailureNotification"], Sequencer.MainContainer.Name, ex.Message));
                    return;
                }
            }
            if (!string.IsNullOrEmpty(SavePath)) {
                Notification.ShowSuccess(string.Format(Loc.Instance["Lbl_Sequencer_SaveSequence_Notification"], Sequencer.MainContainer.Name, SavePath));
            }
        }

        public ISequencerFactory SequencerFactory { get; }

        public TemplateController TemplateController { get; private set; }
        public TargetController TargetController { get; private set; }

        public SequenceJsonConverter SequenceJsonConverter { get; private set; }

        private bool isRunning;

        public bool IsRunning {
            get => isRunning;
            set {
                isRunning = value;
                RaisePropertyChanged();
            }
        }

        private TaskbarItemProgressState taskBarProgressState = TaskbarItemProgressState.None;

        public TaskbarItemProgressState TaskBarProgressState {
            get => taskBarProgressState;
            set {
                taskBarProgressState = value;
                RaisePropertyChanged();
            }
        }

        private ApplicationStatus _status;

        public ApplicationStatus Status {
            get {
                return _status;
            }
            set {
                _status = value;
                if (string.IsNullOrWhiteSpace(_status.Source)) {
                    _status.Source = Loc.Instance["LblSequence"];
                }

                RaisePropertyChanged();

                applicationStatusMediator.StatusUpdate(_status);
            }
        }

        private CancellationTokenSource cts;

        public async Task<bool> StartSequence(object arg) {
            cts?.Dispose();
            cts = new CancellationTokenSource();
            IsRunning = true;
            TaskBarProgressState = TaskbarItemProgressState.Normal;
            try {
                cameraMediator.RegisterCaptureBlock(this);
                await Sequencer.Start(new Progress<ApplicationStatus>(p => Status = p), cts.Token);
                return true;
            } finally {
                cameraMediator.ReleaseCaptureBlock(this);
                TaskBarProgressState = TaskbarItemProgressState.None;
                IsRunning = false;
            }
        }

        private void CancelSequence(object obj) {
            cts?.Cancel();
        }

        public IList<IDeepSkyObjectContainer> GetDeepSkyObjectContainerTemplates() {
            return TemplateController.Templates.Where(x => x.Container is IDeepSkyObjectContainer).Select(y => y.Container as IDeepSkyObjectContainer).ToList();
        }

        public void AddTarget(IDeepSkyObjectContainer container) {
            (this.Sequencer.MainContainer.Items[1] as ISequenceContainer).Add(container);
        }

        public void AddTargetToTargetList(IDeepSkyObjectContainer container) {
            var d = new DropIntoParameters(container);
            d.Position = DropTargetEnum.Center;
            AddTargetToController(d);
        }

        public ICommand AddTemplateCommand { get; private set; }
        public ICommand AddTargetToControllerCommand { get; private set; }

        public ICommand DetachCommand { get; set; }
        public IAsyncCommand StartSequenceCommand { get; private set; }
        public IAsyncCommand SkipCurrentItemCommand { get; private set; }
        public IAsyncCommand SkipToEndOfSequenceCommand { get; private set; }
        public ICommand CancelSequenceCommand { get; private set; }

        public ICommand LoadSequenceCommand { get; }
        public ICommand SwitchToOverviewCommand { get; }
        public ICommand SaveSequenceCommand { get; }
        public ICommand SaveAsSequenceCommand { get; }
    }
}