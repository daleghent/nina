#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Accord.Statistics.Models.Regression.Linear;
using CsvHelper;
using NINA.Core.Enum;
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
using NINA.Astrometry;
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
            Task.Run(() => this.NighttimeData = this.nighttimeCalculator.Calculate());
            this.planetariumFactory = planetariumFactory;
            this.framingAssistantVM = framingAssistantVM;
            this.applicationMediator = applicationMediator;
            this.profileService = profileService;

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

            profileService.LocationChanged += (object sender, EventArgs e) => {
                foreach (var item in this.Targets.Items) {
                    var target = item as SimpleDSOContainer;
                    var dso = new DeepSkyObject(target.Target.DeepSkyObject.Name, target.Target.DeepSkyObject.Coordinates, profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository, profileService.ActiveProfile.AstrometrySettings.Horizon);
                    dso.SetDateAndPosition(NighttimeCalculator.GetReferenceDate(DateTime.Now), profileService.ActiveProfile.AstrometrySettings.Latitude, profileService.ActiveProfile.AstrometrySettings.Longitude);
                    target.Target.DeepSkyObject = dso;
                }
            };

            Task.Run(async () => {
                this.factory = factory;
                while (!factory.Initialized) {
                    await Task.Delay(300);
                }
                await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() => {
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

                    autoUpdateTimer = new DispatcherTimer(DispatcherPriority.Background);
                    autoUpdateTimer.Interval = TimeSpan.FromSeconds(1);
                    autoUpdateTimer.IsEnabled = true;
                    autoUpdateTimer.Tick += (sender, args) => CalculateETA();
                    autoUpdateTimer.Start();
                }));
            });
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

        public NINA.Sequencer.Sequencer Sequencer { get; private set; }

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

        private ISimpleDSOContainer selectedTarget;

        public ISimpleDSOContainer SelectedTarget {
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
                    using (var reader = new StreamReader(dialog.FileName)) {
                        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture)) {
                            this.Targets.Items.Clear();
                            csv.Configuration.PrepareHeaderForMatch = (string header, int index) => header.ToLower().Trim();
                            csv.Configuration.CultureInfo = CultureInfo.InvariantCulture;
                            csv.Configuration.BadDataFound = null;

                            csv.Read();
                            csv.ReadHeader();

                            while (csv.Read()) {
                                string name = string.Empty;
                                if (csv.TryGetField("pane", out name)) {
                                    var ra = AstroUtil.HMSToDegrees(csv.GetField("ra"));
                                    var dec = AstroUtil.DMSToDegrees(csv.GetField("dec"));

                                    //Nina orientation is not east of north, but flipped
                                    var angle = 360 - AstroUtil.EuclidianModulus(csv.GetField<double>("position angle (east)"), 360);

                                    var template = GetTemplate();
                                    template.Name = name;
                                    template.Target.TargetName = name;
                                    template.Target.InputCoordinates.Coordinates = new Coordinates(Angle.ByDegree(ra), Angle.ByDegree(dec), Epoch.J2000);
                                    template.Target.Rotation = angle;
                                    this.Targets.Add(template);
                                } else if (csv.TryGetField<string>("familiar name", out name)) {
                                    var catalogue = csv.GetField("catalogue entry");
                                    var ra = AstroUtil.HMSToDegrees(csv.GetField("right ascension"));
                                    var dec = AstroUtil.DMSToDegrees(csv.GetField("declination"));

                                    var template = GetTemplate();
                                    template.Name = string.IsNullOrWhiteSpace(name) ? catalogue : name; ;
                                    template.Target.TargetName = string.IsNullOrWhiteSpace(name) ? catalogue : name; ;
                                    template.Target.InputCoordinates.Coordinates = new Coordinates(Angle.ByDegree(ra), Angle.ByDegree(dec), Epoch.J2000);
                                    if (csv.TryGetField<string>("position angle (east)", out var stringAngle) && !string.IsNullOrWhiteSpace(stringAngle)) {
                                        //Nina orientation is not east of north, but flipped
                                        var angle = 360 - AstroUtil.EuclidianModulus(csv.GetField<double>("position angle (east)"), 360);
                                        template.Target.Rotation = angle;
                                    } else {
                                        template.Target.Rotation = 0;
                                    }
                                    template.Target.Rotation = 0;
                                    this.Targets.Add(template);
                                }
                            }
                            if (!(this.Targets?.Items?.Count > 0)) {
                                Notification.ShowError(Locale.Loc.Instance["LblUnknownImportFormat"]);
                            }
                        }
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(Locale.Loc.Instance["LblUnknownImportFormat"]);
                } finally {
                    SelectedTarget = (SimpleDSOContainer)Targets.Items.FirstOrDefault();
                }
            }
            (SelectedTarget as SimpleDSOContainer)?.ResetProgressCascaded();
            return Targets?.Items.Count > 0;
        }

        private readonly IFramingAssistantVM framingAssistantVM;
        private readonly IApplicationMediator applicationMediator;
        private DispatcherTimer autoUpdateTimer;
        private ISequencerFactory factory;

        private void AddDefaultTarget(object obj) {
            this.Targets.Add(GetTemplate());
            SelectedTarget = Targets.Items.Last() as SimpleDSOContainer;
            (SelectedTarget as SimpleDSOContainer)?.ResetProgressCascaded();
        }

        public void AddTarget(DeepSkyObject deepSkyObject) {
            var target = GetTemplate();
            target.Target.InputCoordinates.Coordinates = deepSkyObject.Coordinates.Clone();
            target.Target.TargetName = deepSkyObject.Name;
            target.Target.Rotation = deepSkyObject.Rotation;
            this.Targets.Add(target);
            SelectedTarget = Targets.Items.Last() as SimpleDSOContainer;
            (SelectedTarget as SimpleDSOContainer)?.ResetProgressCascaded();
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
            (SelectedTarget as SimpleDSOContainer)?.ResetProgressCascaded();
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
            (SelectedTarget as SimpleDSOContainer)?.ResetProgressCascaded();
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
            try {
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
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(ex.Message);
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
                    NighttimeCalculator.GetReferenceDate(DateTime.Now),
                    profileService.ActiveProfile.AstrometrySettings.Latitude,
                    profileService.ActiveProfile.AstrometrySettings.Longitude
                );
            }

            return MigrateFromCaptureSequenceList(csl);
        }

        public CaptureSequenceList MigrateToCaptureSequenceList(ISimpleDSOContainer container) {
            var csl = new CaptureSequenceList();

            var definedContainer = (SimpleDSOContainer)container;

            csl.Mode = definedContainer.Mode;
            csl.Delay = definedContainer.Delay;
            csl.StartGuiding = definedContainer.StartGuiding;
            csl.SlewToTarget = definedContainer.SlewToTarget;
            csl.CenterTarget = definedContainer.CenterTarget;
            csl.RotateTarget = definedContainer.RotateTarget;
            csl.AutoFocusOnStart = definedContainer.AutoFocusOnStart;
            csl.AutoFocusOnFilterChange = definedContainer.AutoFocusOnFilterChange;
            csl.AutoFocusAfterSetTime = definedContainer.AutoFocusAfterSetTime;
            csl.AutoFocusSetTime = definedContainer.AutoFocusSetTime;
            csl.AutoFocusAfterSetExposures = definedContainer.AutoFocusAfterSetExposures;
            csl.AutoFocusSetExposures = definedContainer.AutoFocusSetExposures;
            csl.AutoFocusAfterTemperatureChange = definedContainer.AutoFocusAfterTemperatureChange;
            csl.AutoFocusAfterTemperatureChangeAmount = definedContainer.AutoFocusAfterTemperatureChangeAmount;
            csl.AutoFocusAfterHFRChange = definedContainer.AutoFocusAfterHFRChange;
            csl.AutoFocusAfterHFRChangeAmount = definedContainer.AutoFocusAfterHFRChangeAmount;

            csl.TargetName = definedContainer.Target.TargetName;
            csl.Coordinates = definedContainer.Target.InputCoordinates.Coordinates.Clone();
            csl.Rotation = definedContainer.Target.Rotation;

            foreach (var item in definedContainer.Items) {
                var simpleExposure = item as SimpleExposure;
                var capture = MigrateSimpleExposureToCaptureSequence(simpleExposure);
                if (definedContainer.Mode == SequenceMode.ROTATE) {
                    capture.ProgressExposureCount = (definedContainer.Conditions[0] as LoopCondition).CompletedIterations;
                    capture.TotalExposureCount = (definedContainer.Conditions[0] as LoopCondition).Iterations;
                }
                csl.Add(capture);
            }

            return csl;
        }

        public CaptureSequence MigrateSimpleExposureToCaptureSequence(SimpleExposure simpleExposure) {
            var cs = new CaptureSequence();
            cs.Enabled = simpleExposure.Enabled;

            cs.Dither = simpleExposure.Dither;
            cs.DitherAmount = (simpleExposure.GetDitherAfterExposures() as DitherAfterExposures).AfterExposures;

            var loop = simpleExposure.GetLoopCondition() as LoopCondition;
            cs.ProgressExposureCount = loop.CompletedIterations;
            cs.TotalExposureCount = loop.Iterations;

            cs.FilterType = (simpleExposure.GetSwitchFilter() as SwitchFilter).Filter;

            var exposure = simpleExposure.GetTakeExposure() as TakeExposure;
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
                var dither = simpleExposure.GetDitherAfterExposures() as DitherAfterExposures;
                dither.AfterExposures = item.DitherAmount;

                var iterations = simpleExposure.GetLoopCondition() as LoopCondition;
                iterations.CompletedIterations = item.ProgressExposureCount;
                iterations.Iterations = item.TotalExposureCount;

                if (item.ProgressExposureCount < item.TotalExposureCount) {
                    completed = false;
                }

                var filter = simpleExposure.GetSwitchFilter() as SwitchFilter;
                filter.Filter = item.FilterType;

                var exposure = simpleExposure.GetTakeExposure() as TakeExposure;
                exposure.ExposureTime = item.ExposureTime;
                exposure.ImageType = item.ImageType;
                exposure.Binning = item.Binning;
                exposure.Gain = item.Gain;
                exposure.Offset = item.Offset;
                exposure.ExposureCount = item.ProgressExposureCount;
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