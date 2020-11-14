#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Accord.Statistics.Models.Regression.Linear;
using NINA.Model;
using NINA.Model.ImageData;
using NINA.Model.MyCamera;
using NINA.Model.MyDome;
using NINA.Model.MyFilterWheel;
using NINA.Model.MyFlatDevice;
using NINA.Model.MyFocuser;
using NINA.Model.MyGuider;
using NINA.Model.MyPlanetarium;
using NINA.Model.MyRotator;
using NINA.Model.MyTelescope;
using NINA.Model.MyWeatherData;
using NINA.PlateSolving;
using NINA.Profile;
using NINA.Sequencer;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.Container.ExecutionStrategy;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Autofocus;
using NINA.Sequencer.SequenceItem.Camera;
using NINA.Sequencer.SequenceItem.Dome;
using NINA.Sequencer.SequenceItem.FilterWheel;
using NINA.Sequencer.SequenceItem.FlatDevice;
using NINA.Sequencer.SequenceItem.Guider;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.SequenceItem.Platesolving;
using NINA.Sequencer.SequenceItem.Telescope;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Sequencer.Trigger.Autofocus;
using NINA.Sequencer.Trigger.Guider;
using NINA.Sequencer.Trigger.MeridianFlip;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Exceptions;
using NINA.Utility.ExternalCommand;
using NINA.Utility.Mediator;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using NINA.Utility.WindowService;
using NINA.ViewModel.Equipment.Camera;
using NINA.ViewModel.FramingAssistant;
using NINA.ViewModel.ImageHistory;
using NINA.ViewModel.Interfaces;
using NINA.ViewModel.Sequencer;
using NINA.ViewModel.Sequencer.SimpleSequence;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shell;
using System.Windows.Threading;

namespace NINA.ViewModel {

    internal class SimpleSequenceVM : BaseVM, ISimpleSequenceVM, ICameraConsumer {

        public SimpleSequenceVM(
                IProfileService profileService,
                ISequenceMediator sequenceMediator,
                ICameraMediator cameraMediator,
                IApplicationStatusMediator applicationStatusMediator,
                INighttimeCalculator nighttimeCalculator,
                IPlanetariumFactory planetariumFactory,
                IFramingAssistantVM framingAssistantVM,
                IApplicationMediator applicationMediator,
                ISequencerFactory factory
        ) : base(profileService) {
            this.applicationMediator = applicationMediator;

            this.sequenceMediator = sequenceMediator;
            this.applicationStatusMediator = applicationStatusMediator;

            this.cameraMediator = cameraMediator;
            this.cameraMediator.RegisterConsumer(this);
            this.nighttimeCalculator = nighttimeCalculator;
            this.NighttimeData = this.nighttimeCalculator.Calculate();
            this.planetariumFactory = planetariumFactory;
            this.framingAssistantVM = framingAssistantVM;
            this.applicationMediator = applicationMediator;
            this.profileService = profileService;

            this.factory = factory;

            AddTargetCommand = new RelayCommand(AddDefaultTarget);
            BuildSequenceCommand = new RelayCommand((object o) => {
                if (MyMessageBox.MyMessageBox.Show(Locale.Loc.Instance["Lbl_OldSequencer_BuildAdvanced_Text"], Locale.Loc.Instance["Lbl_OldSequencer_BuildAdvanced_Caption"], MessageBoxButton.YesNo, MessageBoxResult.Yes) == MessageBoxResult.Yes) {
                    BuildSequence();
                }
            });
            SaveSequenceCommand = new RelayCommand(SaveSequence, (object o) => !string.IsNullOrEmpty(SelectedTarget?.FileName));
            SaveAsSequenceCommand = new RelayCommand(SaveAsSequence);
            LoadSequenceCommand = new RelayCommand((object o) => LoadTarget());
            SaveTargetSetCommand = new RelayCommand(SaveTargetSet);
            LoadTargetSetCommand = new RelayCommand((object o) => LoadTargetSet());
            ImportTargetsCommand = new RelayCommand((object o) => ImportTargets());
            StartSequenceCommand = new AsyncCommand<bool>(StartSequence);
            CancelSequenceCommand = new RelayCommand(CancelSequence);
            SwitchToOverviewCommand = new RelayCommand((object o) => sequenceMediator.SwitchToOverview(), (object o) => !profileService.ActiveProfile.SequenceSettings.DisableSimpleSequencer);

            autoUpdateTimer = new DispatcherTimer(DispatcherPriority.Background);
            autoUpdateTimer.Interval = TimeSpan.FromSeconds(1);
            autoUpdateTimer.IsEnabled = true;
            autoUpdateTimer.Tick += (sender, args) => CalculateETA();

            profileService.LocationChanged += (object sender, EventArgs e) => {
                foreach (var item in this.Targets.Items) {
                    var target = item as SimpleDSOContainer;
                    var dso = new DeepSkyObject(target.Target.DeepSkyObject.Name, target.Target.DeepSkyObject.Coordinates, profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository);
                    dso.SetDateAndPosition(Utility.Astrometry.NighttimeCalculator.GetReferenceDate(DateTime.Now), profileService.ActiveProfile.AstrometrySettings.Latitude, profileService.ActiveProfile.AstrometrySettings.Longitude);
                    target.Target.DeepSkyObject = dso;
                }
            };

            autoUpdateTimer.Start();
            var targetArea = factory.GetContainer<TargetAreaContainer>();
            var rootContainer = factory.GetContainer<SequenceRootContainer>();
            rootContainer.Add(new SimpleStartContainer(factory, profileService));
            rootContainer.Add(targetArea);
            rootContainer.Add(new SimpleEndContainer(factory, profileService));
            (targetArea.Items as ObservableCollection<ISequenceItem>).CollectionChanged += SimpleSequenceVM_CollectionChanged;

            Sequencer = new NINA.Sequencer.Sequencer(
                rootContainer
            );

            DoMeridianFlip = profileService.ActiveProfile.SequenceSettings.DoMeridianFlip;

            EstimatedDownloadTime = profileService.ActiveProfile.SequenceSettings.EstimatedDownloadTime;
        }

        private void SimpleSequenceVM_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            if (e.NewItems != null) {
                foreach (var item in e.NewItems) {
                    var target = item as SimpleDSOContainer;
                    target.PropertyChanged += Target_PropertyChanged;
                }
            }
            if (e.OldItems != null) {
                foreach (var item in e.OldItems) {
                    var target = item as SimpleDSOContainer;
                    target.PropertyChanged -= Target_PropertyChanged;
                }
            }
        }

        private void Target_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(SimpleDSOContainer.Status)) {
                var target = sender as SimpleDSOContainer;
                if (target.Status == SequenceEntityStatus.RUNNING) {
                    ActiveTarget = target;
                }
            }
        }

        private CancellationTokenSource cts;

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
                    _status.Source = Locale.Loc.Instance["LblSequence"];
                }

                RaisePropertyChanged();

                applicationStatusMediator.StatusUpdate(_status);
            }
        }

        public async Task<bool> StartSequence(object arg) {
            cts?.Dispose();
            cts = new CancellationTokenSource();
            IsRunning = true;
            TaskBarProgressState = TaskbarItemProgressState.Normal;
            try {
                //Reset start and end of sequence options in case they were both already done
                if (Sequencer.MainContainer.Items[2].Status == SequenceEntityStatus.FINISHED) {
                    Sequencer.MainContainer.Items[0].ResetProgress();
                    Sequencer.MainContainer.Items[2].ResetProgress();
                }

                await Sequencer.Start(new Progress<ApplicationStatus>(p => Status = p), cts.Token);
                return true;
            } finally {
                TaskBarProgressState = TaskbarItemProgressState.None;
                IsRunning = false;
            }
        }

        private void CancelSequence(object obj) {
            cts?.Cancel();
        }

        public NINA.Sequencer.Sequencer Sequencer { get; }

        public bool DoMeridianFlip {
            get => profileService.ActiveProfile.SequenceSettings.DoMeridianFlip;
            set {
                profileService.ActiveProfile.SequenceSettings.DoMeridianFlip = value;
                if (value) {
                    foreach (var item in Targets.Items) {
                        var target = item as SimpleDSOContainer;
                        target.Add(factory.GetTrigger<MeridianFlipTrigger>());
                    }
                } else {
                    foreach (var item in Targets.Items) {
                        var target = item as SimpleDSOContainer;
                        var t = target.Triggers.FirstOrDefault(x => x is MeridianFlipTrigger);
                        target.Remove(t);
                    }
                }
                RaisePropertyChanged();
            }
        }

        public SimpleStartContainer StartOptions {
            get {
                return Sequencer.MainContainer.Items[0] as SimpleStartContainer;
            }
        }

        public SimpleEndContainer EndOptions {
            get {
                return Sequencer.MainContainer.Items[2] as SimpleEndContainer;
            }
        }

        public TargetAreaContainer Targets {
            get {
                var targets = Sequencer.MainContainer.Items[1] as TargetAreaContainer;
                return targets;
            }
        }

        private SimpleDSOContainer selectedTarget;

        public SimpleDSOContainer SelectedTarget {
            get => selectedTarget;
            set {
                selectedTarget = value;
                if (selectedTarget == null) {
                    selectedTarget = (SimpleDSOContainer)Targets.Items.LastOrDefault();
                }

                if (selectedTarget == null && this.Targets.Items.Count == 0) {
                    sequenceMediator.SwitchToOverview();
                } else {
                    if (!IsRunning) {
                        ActiveTarget = Targets.Items.FirstOrDefault() as SimpleDSOContainer;
                    }
                }

                RaisePropertyChanged();
            }
        }

        private SimpleDSOContainer activeTarget;

        public SimpleDSOContainer ActiveTarget {
            get => activeTarget;
            set {
                activeTarget = value;
                ActiveTargetIndex = Targets.Items.IndexOf(activeTarget) + 1;
                RaisePropertyChanged();
            }
        }

        private int activeTargetIndex;

        public int ActiveTargetIndex {
            get => activeTargetIndex;
            set {
                activeTargetIndex = value;
                RaisePropertyChanged();
            }
        }

        public bool ImportTargets() {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Multiselect = false;
            dialog.Title = Locale.Loc.Instance["LblImportTargets"];
            dialog.InitialDirectory = profileService.ActiveProfile.SequenceSettings.DefaultSequenceFolder;
            dialog.FileName = "";
            dialog.DefaultExt = "csv";
            dialog.Filter = "Telescopius|*." + dialog.DefaultExt;

            if (dialog.ShowDialog() == true) {
                try {
                    var s = File.ReadAllText(dialog.FileName);
                    using (var reader = new System.IO.StringReader(s)) {
                        string headerLine = reader.ReadLine();
                        string[] headerColumns = headerLine.Split(',').Select(p => p.Trim()).ToArray();

                        if (Array.FindIndex(headerColumns, x => x.ToLower() == "pane") > -1) {
                            this.Targets.Items.Clear();
                            var idxPane = Array.FindIndex(headerColumns, x => x.ToLower() == "pane");
                            var idxRA = Array.FindIndex(headerColumns, x => x.ToLower() == "ra");
                            var idxDec = Array.FindIndex(headerColumns, x => x.ToLower() == "dec");
                            var idxAngle = Array.FindIndex(headerColumns, x => x.ToLower() == "position angle (east)");

                            string line;
                            while ((line = reader.ReadLine()) != null) {
                                //Telescopius Mosaic Plan
                                var columns = line.Split(',').Select(p => p.Trim()).ToArray();

                                var name = columns[idxPane];
                                var ra = Astrometry.HMSToDegrees(columns[idxRA]);
                                var dec = Astrometry.DMSToDegrees(columns[idxDec]);
                                //Nina orientation is not east of north, but flipped
                                var angle = 360 - Astrometry.EuclidianModulus(double.Parse(columns[idxAngle], CultureInfo.InvariantCulture), 360);

                                var template = GetTemplate();
                                template.Name = name;
                                template.Target.TargetName = name;
                                template.Target.InputCoordinates.Coordinates = new Coordinates(Angle.ByDegree(ra), Angle.ByDegree(dec), Epoch.J2000);
                                template.Target.Rotation = angle;
                                this.Targets.Add(template);
                            }
                        } else if (Array.FindIndex(headerColumns, x => x.ToLower() == "familiar name") > -1) {
                            this.Targets.Items.Clear();
                            //Telescopius Observing List
                            var idxCatalogue = Array.FindIndex(headerColumns, x => x.ToLower() == "catalogue entry");
                            var idxName = Array.FindIndex(headerColumns, x => x.ToLower() == "familiar name");
                            var idxRA = Array.FindIndex(headerColumns, x => x.ToLower() == "right ascension");
                            var idxDec = Array.FindIndex(headerColumns, x => x.ToLower() == "declination");
                            var idxAngle = Array.FindIndex(headerColumns, x => x.ToLower() == "position angle (east)");

                            string line;
                            while ((line = reader.ReadLine()) != null) {
                                var columns = line.Split(',').Select(p => p.Trim()).ToArray();

                                var name = columns[idxName];
                                var ra = Astrometry.HMSToDegrees(columns[idxRA]);
                                var dec = Astrometry.DMSToDegrees(columns[idxDec]);

                                var template = GetTemplate();
                                template.Name = string.IsNullOrWhiteSpace(name) ? columns[idxCatalogue] : name; ;
                                template.Target.TargetName = string.IsNullOrWhiteSpace(name) ? columns[idxCatalogue] : name; ;
                                template.Target.InputCoordinates.Coordinates = new Coordinates(Angle.ByDegree(ra), Angle.ByDegree(dec), Epoch.J2000);
                                if (idxAngle >= 0 && !string.IsNullOrWhiteSpace(columns[idxAngle])) {
                                    //Nina orientation is not east of north, but flipped
                                    var angle = 360 - Astrometry.EuclidianModulus(double.Parse(columns[idxAngle], CultureInfo.InvariantCulture), 360);
                                    template.Target.Rotation = angle;
                                } else {
                                    template.Target.Rotation = 0;
                                }
                                template.Target.Rotation = 0;
                                this.Targets.Add(template);
                            }
                        } else {
                            Notification.ShowError(Locale.Loc.Instance["LblUnknownImportFormat"]);
                        }
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(Locale.Loc.Instance["LblUnknownImportFormat"]);
                } finally {
                    SelectedTarget = (SimpleDSOContainer)Targets.Items.FirstOrDefault();
                }
            }
            return Targets?.Items.Count > 0;
        }

        private readonly IFramingAssistantVM framingAssistantVM;
        private readonly IApplicationMediator applicationMediator;
        private DispatcherTimer autoUpdateTimer;
        private ISequencerFactory factory;

        private void AddDefaultTarget(object obj) {
            this.Targets.Add(GetTemplate());
            SelectedTarget = Targets.Items.Last() as SimpleDSOContainer;
        }

        public void AddTarget(DeepSkyObject deepSkyObject) {
            var target = GetTemplate();
            target.Target.InputCoordinates.Coordinates = deepSkyObject.Coordinates.Clone();
            target.Target.TargetName = deepSkyObject.Name;
            target.Target.Rotation = deepSkyObject.Rotation;
            this.Targets.Add(target);
            SelectedTarget = Targets.Items.Last() as SimpleDSOContainer;
        }

        private void SaveTargetSet(object obj) {
            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.InitialDirectory = profileService.ActiveProfile.SequenceSettings.DefaultSequenceFolder;
            dialog.Title = Locale.Loc.Instance["LblSaveTargetSet"];
            dialog.FileName = "";
            dialog.DefaultExt = "ninaTargetSet";
            dialog.Filter = "N.I.N.A target set files|*." + dialog.DefaultExt;
            dialog.OverwritePrompt = true;

            if (dialog.ShowDialog().Value) {
                var cslCollection = new Collection<CaptureSequenceList>();
                foreach (var item in Targets.Items) {
                    var target = item as SimpleDSOContainer;
                    if (target != null) {
                        cslCollection.Add(MigrateToCaptureSequenceList(target));
                    }
                }
                CaptureSequenceList.SaveSequenceSet(cslCollection, dialog.FileName);
            }
        }

        public bool LoadTargetSet() {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Multiselect = false;
            dialog.Title = Locale.Loc.Instance["LblLoadTargetSet"];
            dialog.InitialDirectory = profileService.ActiveProfile.SequenceSettings.DefaultSequenceFolder;
            dialog.FileName = "";
            dialog.DefaultExt = "ninaTargetSet";
            dialog.Filter = "N.I.N.A target set files|*." + dialog.DefaultExt;

            if (dialog.ShowDialog() == true) {
                using (var s = new FileStream(dialog.FileName, FileMode.Open)) {
                    this.Targets.Items.Clear();
                    var set = new AsyncObservableCollection<CaptureSequenceList>(CaptureSequenceList.LoadSequenceSet(
                        s,
                        profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters,
                        profileService.ActiveProfile.AstrometrySettings.Latitude,
                        profileService.ActiveProfile.AstrometrySettings.Longitude
                    ));
                    foreach (var csl in set) {
                        this.Targets.Add(MigrateFromCaptureSequenceList(csl));
                    }
                    SelectedTarget = Targets.Items.FirstOrDefault() as SimpleDSOContainer;
                }
            }
            return Targets?.Items.Count > 0;
        }

        public bool LoadTarget() {
            // LoadSequence loads .xml files indivually - user may select any number of files from same folder
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Multiselect = true;
            dialog.Title = Locale.Loc.Instance["LblLoadSequence"];
            dialog.InitialDirectory = profileService.ActiveProfile.SequenceSettings.DefaultSequenceFolder;
            dialog.FileName = "Target";
            dialog.DefaultExt = ".xml";
            dialog.Filter = "XML documents|*.xml";

            if (dialog.ShowDialog() == true) {
                foreach (var fileName in dialog.FileNames) {
                    var l = CaptureSequenceList.Load(fileName,
                        profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters,
                        profileService.ActiveProfile.AstrometrySettings.Latitude,
                        profileService.ActiveProfile.AstrometrySettings.Longitude
                    );
                    if (l != null) {
                        var transform = MigrateFromCaptureSequenceList(l);
                        transform.FileName = dialog.FileName;
                        this.Targets.Add(transform);

                        // set the last one loaded as the current sequence
                        SelectedTarget = transform;
                    }
                }
            }
            return Targets?.Items.Count > 0;
        }

        private void SaveSequence(object obj) {
            if (string.IsNullOrEmpty(SelectedTarget.FileName)) {
                SaveAsSequence(obj);
            } else {
                var csl = MigrateToCaptureSequenceList(SelectedTarget);
                csl.Save(SelectedTarget.FileName);
            }
        }

        private void SaveAsSequence(object obj) {
            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.InitialDirectory = profileService.ActiveProfile.SequenceSettings.DefaultSequenceFolder;
            dialog.Title = Locale.Loc.Instance["LblSaveAsSequence"];
            dialog.DefaultExt = ".xml";
            dialog.Filter = "XML documents|*.xml";
            dialog.OverwritePrompt = true;

            Regex r = new Regex($"[{new string(Path.GetInvalidFileNameChars())}]");
            dialog.FileName = r.Replace(SelectedTarget.Target.TargetName, "-");

            if (dialog.ShowDialog().Value) {
                SelectedTarget.FileName = dialog.FileName;
                var csl = MigrateToCaptureSequenceList(SelectedTarget);
                csl.Save(dialog.FileName);
            }
        }

        public void AddDownloadTime(TimeSpan t) {
            _actualDownloadTimes.Add(t);
            double doubleAverageTicks = _actualDownloadTimes.Average(timeSpan => timeSpan.Ticks);
            long longAverageTicks = Convert.ToInt64(doubleAverageTicks);
            EstimatedDownloadTime = new TimeSpan(longAverageTicks);
        }

        private void CalculateETA() {
            if (Targets.Items.Count > 0) {
                TimeSpan time = new TimeSpan();
                foreach (var item in Targets.Items) {
                    var target = item as SimpleDSOContainer;
                    if (target != null) {
                        var targetETA = target.CalculateEstimatedRuntime();
                        target.EstimatedStartTime = DateTime.Now.AddSeconds(time.TotalSeconds);
                        time += targetETA;
                        target.EstimatedEndTime = DateTime.Now.AddSeconds(time.TotalSeconds);
                        target.EstimatedDuration = target.EstimatedEndTime - target.EstimatedStartTime;
                    }
                }

                OverallStartTime = DateTime.Now;
                OverallEndTime = DateTime.Now.AddSeconds(time.TotalSeconds);
                OverallDuration = OverallEndTime - OverallStartTime;
            }
        }

        private List<TimeSpan> _actualDownloadTimes = new List<TimeSpan>();

        private TimeSpan estimatedDownloadTime = TimeSpan.Zero;

        public TimeSpan EstimatedDownloadTime {
            get {
                return estimatedDownloadTime;
            }
            set {
                if (value < TimeSpan.Zero) {
                    value = TimeSpan.Zero;
                }
                estimatedDownloadTime = value;
                RaisePropertyChanged();
                CalculateETA();
            }
        }

        private DateTime overallStartTime;

        public DateTime OverallStartTime {
            get {
                return overallStartTime;
            }
            private set {
                overallStartTime = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(OverallEndTime));
            }
        }

        private DateTime _eta;

        public DateTime OverallEndTime {
            get {
                return _eta;
            }
            private set {
                _eta = value;
                RaisePropertyChanged();
            }
        }

        private TimeSpan overallDuration;

        public TimeSpan OverallDuration {
            get {
                return overallDuration;
            }
            private set {
                overallDuration = value;
                RaisePropertyChanged();
            }
        }

        private IWindowServiceFactory windowServiceFactory;

        public IWindowServiceFactory WindowServiceFactory {
            get {
                if (windowServiceFactory == null) {
                    windowServiceFactory = new WindowServiceFactory();
                }
                return windowServiceFactory;
            }
            set {
                windowServiceFactory = value;
            }
        }

        private void BuildSequence() {
            var container = factory.GetContainer<SequenceRootContainer>();
            var startArea = factory.GetContainer<StartAreaContainer>();
            foreach (var item in StartOptions.Items) {
                startArea.Add((ISequenceItem)item.Clone());
            }

            var targetArea = factory.GetContainer<TargetAreaContainer>();
            foreach (var item in Targets.Items) {
                var target = item as SimpleDSOContainer;
                if (target.Status == SequenceEntityStatus.CREATED) {
                    targetArea.Add(target.TransformToDSOContainer());
                }
            }

            var endArea = factory.GetContainer<EndAreaContainer>();
            foreach (var item in EndOptions.Items) {
                endArea.Add((ISequenceItem)item.Clone());
            }

            container.Add(startArea);
            container.Add(targetArea);
            container.Add(endArea);
            sequenceMediator.SetAdvancedSequence(container);
            sequenceMediator.SwitchToAdvancedView();
        }

        private SimpleDSOContainer GetTemplate() {
            CaptureSequenceList csl = null;
            if (File.Exists(profileService.ActiveProfile.SequenceSettings.TemplatePath)) {
                csl = CaptureSequenceList.Load(profileService.ActiveProfile.SequenceSettings.TemplatePath,
                    profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters,
                    profileService.ActiveProfile.AstrometrySettings.Latitude,
                    profileService.ActiveProfile.AstrometrySettings.Longitude
                );
            } else {
                var seq = new CaptureSequence();
                csl = new CaptureSequenceList(seq) { TargetName = "Target" };
                csl.DSO?.SetDateAndPosition(
                    Utility.Astrometry.NighttimeCalculator.GetReferenceDate(DateTime.Now),
                    profileService.ActiveProfile.AstrometrySettings.Latitude,
                    profileService.ActiveProfile.AstrometrySettings.Longitude
                );
            }

            return MigrateFromCaptureSequenceList(csl);
        }

        public CaptureSequenceList MigrateToCaptureSequenceList(SimpleDSOContainer container) {
            var csl = new CaptureSequenceList();

            csl.Mode = container.Mode;
            csl.Delay = container.Delay;
            csl.StartGuiding = container.StartGuiding;
            csl.SlewToTarget = container.SlewToTarget;
            csl.CenterTarget = container.CenterTarget;
            csl.RotateTarget = container.RotateTarget;
            csl.AutoFocusOnStart = container.AutoFocusOnStart;
            csl.AutoFocusOnFilterChange = container.AutoFocusOnFilterChange;
            csl.AutoFocusAfterSetTime = container.AutoFocusAfterSetTime;
            csl.AutoFocusSetTime = container.AutoFocusSetTime;
            csl.AutoFocusAfterSetExposures = container.AutoFocusAfterSetExposures;
            csl.AutoFocusSetExposures = container.AutoFocusSetExposures;
            csl.AutoFocusAfterTemperatureChange = container.AutoFocusAfterTemperatureChange;
            csl.AutoFocusAfterTemperatureChangeAmount = container.AutoFocusAfterTemperatureChangeAmount;
            csl.AutoFocusAfterHFRChange = container.AutoFocusAfterHFRChange;
            csl.AutoFocusAfterHFRChangeAmount = container.AutoFocusAfterHFRChangeAmount;

            csl.TargetName = container.Target.TargetName;
            csl.Coordinates = container.Target.InputCoordinates.Coordinates.Clone();
            csl.Rotation = container.Target.Rotation;

            foreach (var item in container.Items) {
                var simpleExposure = item as SimpleExposure;
                var capture = MigrateSimpleExposureToCaptureSequence(simpleExposure);
                if (container.Mode == SequenceMode.ROTATE) {
                    capture.ProgressExposureCount = (container.Conditions[0] as LoopCondition).CompletedIterations;
                    capture.TotalExposureCount = (container.Conditions[0] as LoopCondition).Iterations;
                }
                csl.Add(capture);
            }

            return csl;
        }

        public CaptureSequence MigrateSimpleExposureToCaptureSequence(SimpleExposure simpleExposure) {
            var cs = new CaptureSequence();
            cs.Enabled = simpleExposure.Enabled;

            cs.Dither = simpleExposure.Dither;
            cs.DitherAmount = simpleExposure.GetDitherAfterExposures().AfterExposures;

            var loop = simpleExposure.GetLoopCondition();
            cs.ProgressExposureCount = loop.CompletedIterations;
            cs.TotalExposureCount = loop.Iterations;

            cs.FilterType = simpleExposure.GetSwitchFilter().Filter;

            var exposure = simpleExposure.GetTakeExposure();
            cs.ExposureTime = exposure.ExposureTime;
            cs.ImageType = exposure.ImageType;
            cs.Binning = exposure.Binning;
            cs.Gain = exposure.Gain;
            cs.Offset = exposure.Offset;
            return cs;
        }

        public SimpleDSOContainer MigrateFromCaptureSequenceList(CaptureSequenceList csl) {
            var container = new SimpleDSOContainer(factory, profileService, cameraMediator, nighttimeCalculator, framingAssistantVM, applicationMediator, planetariumFactory);

            container.Delay = csl.Delay;
            container.StartGuiding = csl.StartGuiding;
            container.SlewToTarget = csl.SlewToTarget;
            container.CenterTarget = csl.CenterTarget;
            container.RotateTarget = csl.RotateTarget;
            container.AutoFocusOnStart = csl.AutoFocusOnStart;
            container.AutoFocusOnFilterChange = csl.AutoFocusOnFilterChange;
            container.AutoFocusAfterSetTime = csl.AutoFocusAfterSetTime;
            container.AutoFocusSetTime = csl.AutoFocusSetTime;
            container.AutoFocusAfterSetExposures = csl.AutoFocusAfterSetExposures;
            container.AutoFocusSetExposures = (int)csl.AutoFocusSetExposures;
            container.AutoFocusAfterTemperatureChange = csl.AutoFocusAfterTemperatureChange;
            container.AutoFocusAfterTemperatureChangeAmount = csl.AutoFocusAfterTemperatureChangeAmount;
            container.AutoFocusAfterHFRChange = csl.AutoFocusAfterHFRChange;
            container.AutoFocusAfterHFRChangeAmount = csl.AutoFocusAfterHFRChangeAmount;

            container.Target.TargetName = csl.TargetName;
            container.Target.InputCoordinates.Coordinates = csl.DSO.Coordinates.Clone();
            container.Target.Rotation = csl.Rotation;

            var completed = true;
            foreach (var item in csl.Items) {
                var simpleExposure = container.AddSimpleExposure();
                simpleExposure.Enabled = item.Enabled;

                simpleExposure.Dither = item.Dither;
                var dither = simpleExposure.GetDitherAfterExposures();
                dither.AfterExposures = item.DitherAmount;

                var iterations = simpleExposure.GetLoopCondition();
                iterations.CompletedIterations = item.ProgressExposureCount;
                iterations.Iterations = item.TotalExposureCount;

                if (item.ProgressExposureCount < item.TotalExposureCount) {
                    completed = false;
                }

                var filter = simpleExposure.GetSwitchFilter();
                filter.Filter = item.FilterType;

                var exposure = simpleExposure.GetTakeExposure();
                exposure.ExposureTime = item.ExposureTime;
                exposure.ImageType = item.ImageType;
                exposure.Binning = item.Binning;
                exposure.Gain = item.Gain;
                exposure.Offset = item.Offset;
            }

            //Setting mode as last item to auto migrate simple exposure to rotate mode
            container.Mode = csl.Mode;

            if (completed) {
                container.Status = SequenceEntityStatus.FINISHED;
            }

            if (DoMeridianFlip) {
                container.MeridianFlipEnabled = true;
            }

            return container;
        }

        private ObservableCollection<string> _imageTypes;
        private ISequenceMediator sequenceMediator;
        private IApplicationStatusMediator applicationStatusMediator;
        private ICameraMediator cameraMediator;
        private INighttimeCalculator nighttimeCalculator;
        private readonly IPlanetariumFactory planetariumFactory;
        private CameraInfo cameraInfo = DeviceInfo.CreateDefaultInstance<CameraInfo>();

        private NighttimeData nighttimeData;

        public NighttimeData NighttimeData {
            get {
                return nighttimeData;
            }
            set {
                if (nighttimeData != value) {
                    nighttimeData = value;
                    RaisePropertyChanged();
                }
            }
        }

        public ObservableCollection<string> ImageTypes {
            get {
                if (_imageTypes == null) {
                    _imageTypes = new ObservableCollection<string>();

                    Type type = typeof(Model.CaptureSequence.ImageTypes);
                    foreach (var p in type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)) {
                        var v = p.GetValue(null);
                        _imageTypes.Add(v.ToString());
                    }
                }
                return _imageTypes;
            }
            set {
                _imageTypes = value;
                RaisePropertyChanged();
            }
        }

        public CameraInfo CameraInfo {
            get => cameraInfo;
            set {
                cameraInfo = value;
                RaisePropertyChanged();
            }
        }

        public void UpdateDeviceInfo(CameraInfo cameraInfo) {
            CameraInfo = cameraInfo;
        }

        public IAsyncCommand StartSequenceCommand { get; private set; }
        public ICommand CancelSequenceCommand { get; private set; }

        public ICommand AddTargetCommand { get; private set; }
        public ICommand BuildSequenceCommand { get; private set; }
        public ICommand LoadSequenceCommand { get; private set; }
        public ICommand SaveSequenceCommand { get; private set; }
        public ICommand SaveAsSequenceCommand { get; private set; }

        public ICommand SaveTargetSetCommand { get; private set; }
        public ICommand LoadTargetSetCommand { get; private set; }
        public ICommand ImportTargetsCommand { get; private set; }
        public ICommand SwitchToOverviewCommand { get; private set; }

        public void Dispose() {
            this.cameraMediator.RemoveConsumer(this);
        }
    }
}