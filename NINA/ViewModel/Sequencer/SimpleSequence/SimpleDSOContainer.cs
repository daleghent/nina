#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Model.MyPlanetarium;
using NINA.Profile;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.Container.ExecutionStrategy;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Autofocus;
using NINA.Sequencer.SequenceItem.Guider;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.SequenceItem.Platesolving;
using NINA.Sequencer.SequenceItem.Telescope;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Trigger.Autofocus;
using NINA.Sequencer.Trigger.Guider;
using NINA.Sequencer.Trigger.MeridianFlip;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Exceptions;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using NINA.ViewModel;
using NINA.ViewModel.FramingAssistant;
using NINA.ViewModel.ImageHistory;
using NINA.ViewModel.Sequencer.SimpleSequence;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.Sequencer.Container {

    [JsonObject(MemberSerialization.OptIn)]
    public class SimpleDSOContainer : SequenceContainer, IDeepSkyObjectContainer, IImmutableContainer {
        private readonly IProfileService profileService;
        private readonly ISequencerFactory factory;
        private readonly IFramingAssistantVM framingAssistantVM;
        private readonly IPlanetariumFactory planetariumFactory;
        private readonly IApplicationMediator applicationMediator;
        private INighttimeCalculator nighttimeCalculator;
        private InputTarget target;
        private CameraInfo cameraInfo;
        private int delay;
        private bool stargGuiding;
        private bool slewToTarget;
        private bool centerTarget;
        private bool rotateTarget;
        private bool autoFocusOnStart;
        private bool autoFocusOnFilterChange;
        private AutofocusAfterFilterChange afOnFilterChangeTrigger;
        private AutofocusAfterTimeTrigger autoFocusAfterSetTimeTrigger;
        private AutofocusAfterExposures autoFocusAfterSetExposuresTrigger;
        private AutofocusAfterTemperatureChangeTrigger autoFocusAfterTemperatureChangeTrigger;
        private AutofocusAfterHFRIncreaseTrigger autoFocusAfterHFRChangeTrigger;
        private MeridianFlipTrigger flipTrigger;
        private LoopCondition rotateLoopCondition;
        private bool autoFocusAfterSetTime;
        private bool autoFocusAfterSetExposures;
        private bool autoFocusAfterTemperatureChange;
        private bool autoFocusAfterHFRChange;
        private bool meridianFlipEnabled;
        private ICameraMediator cameraMediator;
        private SimpleExposure selectedSimpleExposure;
        private DateTime estimatedStartTime;
        private DateTime estimatedEndTime;
        private TimeSpan estimatedDuration;
        private SimpleExposure activeExposure;
        private SequenceMode mode;

        [ImportingConstructor]
        public SimpleDSOContainer(
                ISequencerFactory factory, /* Having the factory here is an antipattern, however the old sequencer won't be further enhanced anyways */
                IProfileService profileService,
                ICameraMediator cameraMediator,
                INighttimeCalculator nighttimeCalculator,
                IFramingAssistantVM framingAssistantVM,
                IApplicationMediator applicationMediator,
                IPlanetariumFactory planetariumFactory) : base(new SequentialStrategy()) {
            this.profileService = profileService;
            this.factory = factory;
            this.cameraMediator = cameraMediator;
            this.nighttimeCalculator = nighttimeCalculator;
            this.applicationMediator = applicationMediator;
            this.framingAssistantVM = framingAssistantVM;
            this.planetariumFactory = planetariumFactory;

            CameraInfo = this.cameraMediator.GetInfo();
            NighttimeData = nighttimeCalculator.Calculate();
            Target = new InputTarget(Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Latitude), Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Longitude));
            CoordsToFramingCommand = new AsyncCommand<bool>(() => Task.Run(CoordsToFraming));
            CoordsFromPlanetariumCommand = new AsyncCommand<bool>(() => Task.Run(CoordsFromPlanetarium));

            AddSimpleExposureCommand = new RelayCommand((object o) => AddSimpleExposure());
            RemoveSimpleExposureCommand = new RelayCommand(RemoveSimpleExposure);
            ResetSimpleExposureCommand = new RelayCommand(ResetSimpleExposure);
            PromoteSimpleExposureCommand = new RelayCommand(PromoteSimpleExposure);
            DemoteSimpleExposureCommand = new RelayCommand(DemoteSimpleExposure);

            this.afOnFilterChangeTrigger = factory.GetTrigger<AutofocusAfterFilterChange>();
            this.autoFocusAfterSetTimeTrigger = factory.GetTrigger<AutofocusAfterTimeTrigger>();
            this.autoFocusAfterSetExposuresTrigger = factory.GetTrigger<AutofocusAfterExposures>();
            this.autoFocusAfterTemperatureChangeTrigger = factory.GetTrigger<AutofocusAfterTemperatureChangeTrigger>();
            this.autoFocusAfterHFRChangeTrigger = factory.GetTrigger<AutofocusAfterHFRIncreaseTrigger>();
            this.flipTrigger = factory.GetTrigger<MeridianFlipTrigger>();

            this.rotateLoopCondition = factory.GetCondition<LoopCondition>();
            this.rotateLoopCondition.Iterations = 1;
        }

        public SimpleExposure ActiveExposure {
            get => activeExposure;
            set {
                activeExposure = value;
                RaisePropertyChanged();
            }
        }

        public override void MoveUp() {
            if (this.Parent.Items.IndexOf(this) > 0) {
                base.MoveUp();
            }
        }

        public override void MoveDown() {
            if (this.Parent.Items.IndexOf(this) < this.Parent.Items.Count - 1) {
                base.MoveDown();
            }
        }

        public TimeSpan CalculateEstimatedRuntime() {
            var duration = TimeSpan.Zero;
            //todo - add estimations for autofocus if possible
            foreach (var item in Items) {
                var se = item as SimpleExposure;
                if (se.Enabled) {
                    duration += TimeSpan.FromSeconds((se.GetTakeExposure().GetEstimatedDuration().TotalSeconds + profileService.ActiveProfile.SequenceSettings.EstimatedDownloadTime.TotalSeconds) * (se.GetLoopCondition().Iterations - se.GetLoopCondition().CompletedIterations));
                }
            }
            return duration;
        }

        public DateTime EstimatedStartTime {
            get => estimatedStartTime;
            set {
                estimatedStartTime = value;
                RaisePropertyChanged();
            }
        }

        public DateTime EstimatedEndTime {
            get => estimatedEndTime;
            set {
                estimatedEndTime = value;
                RaisePropertyChanged();
            }
        }

        public TimeSpan EstimatedDuration {
            get => estimatedDuration;
            set {
                estimatedDuration = value;
                RaisePropertyChanged();
            }
        }

        public SimpleExposure AddSimpleExposure() {
            SimpleExposure item;
            if (SelectedSimpleExposure != null) {
                item = (SimpleExposure)SelectedSimpleExposure.Clone();
                this.Add(item);
            } else {
                item = new SimpleExposure(factory);
                this.Add(item);
            }

            item.PropertyChanged += Item_PropertyChanged1;

            SelectedSimpleExposure = item;
            ActiveExposure = Items.FirstOrDefault() as SimpleExposure;
            return item;
        }

        private void Item_PropertyChanged1(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(SimpleExposure.Status)) {
                var se = sender as SimpleExposure;
                if (se.Status == SequenceEntityStatus.RUNNING) {
                    ActiveExposure = se;
                }
            }
        }

        private void RemoveSimpleExposure(object obj) {
            if (SelectedSimpleExposure != null) {
                var idx = this.Items.IndexOf(SelectedSimpleExposure);
                SelectedSimpleExposure.PropertyChanged -= Item_PropertyChanged;
                this.Remove(SelectedSimpleExposure);

                if (idx >= this.Items.Count) {
                    SelectedSimpleExposure = (SimpleExposure)this.Items.LastOrDefault();
                } else {
                    SelectedSimpleExposure = (SimpleExposure)this.Items[idx];
                }
            }
        }

        private void ResetSimpleExposure(object obj) {
            SelectedSimpleExposure?.ResetAll();
        }

        private void PromoteSimpleExposure(object obj) {
            var idx = this.Items.IndexOf(SelectedSimpleExposure);
            if (idx > 0 && idx <= this.Items.Count - 1) {
                SelectedSimpleExposure?.MoveUp();
                SelectedSimpleExposure = (SimpleExposure)this.Items[--idx];
            }
        }

        private void DemoteSimpleExposure(object obj) {
            var idx = this.Items.IndexOf(SelectedSimpleExposure);
            if (idx >= 0 && idx < this.Items.Count - 1) {
                SelectedSimpleExposure?.MoveDown();
                SelectedSimpleExposure = (SimpleExposure)this.Items[++idx];
            }
        }

        public int RotateIterations {
            get => this.rotateLoopCondition.Iterations;
            set {
                this.rotateLoopCondition.Iterations = value;
                RaisePropertyChanged();
            }
        }

        public SequenceMode Mode {
            get => mode;
            set {
                if (mode != value) {
                    this.mode = value;

                    if (mode == SequenceMode.ROTATE) {
                        var maxIteration = 1;
                        this.Conditions.Add(rotateLoopCondition);
                        foreach (var item in Items) {
                            var se = item as SimpleExposure;
                            var loop = se.GetLoopCondition();
                            maxIteration = Math.Max(maxIteration, loop.Iterations);
                            loop.Iterations = 1;
                            loop.ResetProgress();
                        }
                        RotateIterations = maxIteration;
                    } else {
                        this.Conditions.Remove(rotateLoopCondition);
                        foreach (var item in Items) {
                            var se = item as SimpleExposure;
                            var loop = se.GetLoopCondition();
                            loop.Iterations = RotateIterations;
                            loop.ResetProgress();
                        }
                    }
                    RaisePropertyChanged();
                }
            }
        }

        public SimpleExposure SelectedSimpleExposure {
            get => selectedSimpleExposure;
            set {
                selectedSimpleExposure = value;
                RaisePropertyChanged();
            }
        }

        public CameraInfo CameraInfo {
            get => cameraInfo;
            private set {
                cameraInfo = value;
                RaisePropertyChanged();
            }
        }

        public override bool Validate() {
            CameraInfo = this.cameraMediator.GetInfo();
            return base.Validate();
        }

        [JsonProperty]
        public int Delay {
            get => delay;
            set {
                delay = value;
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public bool StartGuiding {
            get => stargGuiding;
            set {
                stargGuiding = value;
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public bool SlewToTarget {
            get => slewToTarget;
            set {
                slewToTarget = value;
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public bool CenterTarget {
            get => centerTarget;
            set {
                centerTarget = value;
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public bool RotateTarget {
            get => rotateTarget;
            set {
                rotateTarget = value;
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public bool AutoFocusOnStart {
            get => autoFocusOnStart;
            set {
                autoFocusOnStart = value;
                RaisePropertyChanged();
            }
        }

        public bool MeridianFlipEnabled {
            get => meridianFlipEnabled;
            set {
                meridianFlipEnabled = value;
                if (value) {
                    this.Triggers.Add(flipTrigger);
                } else {
                    this.Triggers.Remove(flipTrigger);
                }
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public bool AutoFocusOnFilterChange {
            get => autoFocusOnFilterChange;
            set {
                autoFocusOnFilterChange = value;
                if (value) {
                    this.Triggers.Add(afOnFilterChangeTrigger);
                } else {
                    this.Triggers.Remove(afOnFilterChangeTrigger);
                }
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public bool AutoFocusAfterSetTime {
            get => autoFocusAfterSetTime;
            set {
                autoFocusAfterSetTime = value;
                if (value) {
                    this.Triggers.Add(autoFocusAfterSetTimeTrigger);
                } else {
                    this.Triggers.Remove(autoFocusAfterSetTimeTrigger);
                }
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public double AutoFocusSetTime {
            get => autoFocusAfterSetTimeTrigger.Amount;
            set {
                autoFocusAfterSetTimeTrigger.Amount = value;
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public bool AutoFocusAfterSetExposures {
            get => autoFocusAfterSetExposures;
            set {
                autoFocusAfterSetExposures = value;
                if (value) {
                    this.Triggers.Add(autoFocusAfterSetExposuresTrigger);
                } else {
                    this.Triggers.Remove(autoFocusAfterSetExposuresTrigger);
                }
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public int AutoFocusSetExposures {
            get => autoFocusAfterSetExposuresTrigger.AfterExposures;
            set {
                autoFocusAfterSetExposuresTrigger.AfterExposures = value;
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public bool AutoFocusAfterTemperatureChange {
            get => autoFocusAfterTemperatureChange;
            set {
                autoFocusAfterTemperatureChange = value;
                if (value) {
                    this.Triggers.Add(autoFocusAfterTemperatureChangeTrigger);
                } else {
                    this.Triggers.Remove(autoFocusAfterTemperatureChangeTrigger);
                }
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public double AutoFocusAfterTemperatureChangeAmount {
            get => autoFocusAfterTemperatureChangeTrigger.Amount;
            set {
                autoFocusAfterTemperatureChangeTrigger.Amount = value;
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public bool AutoFocusAfterHFRChange {
            get => autoFocusAfterHFRChange;
            set {
                autoFocusAfterHFRChange = value;
                if (value) {
                    this.Triggers.Add(autoFocusAfterHFRChangeTrigger);
                } else {
                    this.Triggers.Remove(autoFocusAfterHFRChangeTrigger);
                }
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public double AutoFocusAfterHFRChangeAmount {
            get => autoFocusAfterHFRChangeTrigger.Amount;
            set {
                autoFocusAfterHFRChangeTrigger.Amount = value;
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public InputTarget Target {
            get => target;
            set {
                if (Target != null) {
                    Target.PropertyChanged -= Target_PropertyChanged;
                }
                target = value;
                if (Target != null) {
                    Target.PropertyChanged += Target_PropertyChanged;
                }
                RaisePropertyChanged();
            }
        }

        public NighttimeData NighttimeData { get; }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            var startup = CreateStartupContainer();
            await startup.Execute(progress, token);

            await base.Execute(progress, token);

            var closure = CreateClosureContainer();
            await closure.Execute(progress, token);
        }

        private SequentialContainer CreateStartupContainer() {
            var startup = factory.GetContainer<SequentialContainer>();

            if (Delay > 0) {
                var wait = factory.GetItem<WaitForTimeSpan>();
                wait.Time = Delay;
                startup.Add(wait);
            }

            if (SlewToTarget) {
                var slew = factory.GetItem<SlewScopeToRaDec>();
                slew.Coordinates = Target.InputCoordinates;
                startup.Add(slew);
            }

            if (CenterTarget) {
                var center = factory.GetItem<Center>();
                center.Coordinates = Target.InputCoordinates;
                startup.Add(center);
            }

            if (RotateTarget) {
                var rotate = factory.GetItem<CenterAndRotate>();
                rotate.Coordinates = Target.InputCoordinates;
                rotate.Rotation = Target.Rotation;
                startup.Add(rotate);
            }

            if (StartGuiding) {
                startup.Add(factory.GetItem<StartGuiding>());
            }

            if (AutoFocusOnStart) {
                startup.Add(factory.GetItem<RunAutofocus>());
            }

            return startup;
        }

        private SequentialContainer CreateClosureContainer() {
            var closure = factory.GetContainer<SequentialContainer>();

            closure.Add(factory.GetItem<StopGuiding>());

            return closure;
        }

        public override object Clone() {
            var clone = new SimpleDSOContainer(factory, profileService, cameraMediator, nighttimeCalculator, framingAssistantVM, applicationMediator, planetariumFactory) {
                Icon = Icon,
                Name = Name,
                Category = Category,
                Description = Description,
                Items = new ObservableCollection<ISequenceItem>(Items.Select(i => i.Clone() as ISequenceItem)),
                Triggers = new ObservableCollection<ISequenceTrigger>(),
                Conditions = new ObservableCollection<ISequenceCondition>(),
                Target = new InputTarget(Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Latitude), Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Longitude))
            };

            clone.Target.TargetName = this.Target.TargetName;
            clone.Target.InputCoordinates.Coordinates = this.Target.InputCoordinates.Coordinates.Transform(Epoch.J2000);
            clone.Target.Rotation = this.Target.Rotation;

            clone.Delay = Delay;
            clone.StartGuiding = StartGuiding;
            clone.SlewToTarget = SlewToTarget;
            clone.CenterTarget = CenterTarget;
            clone.RotateTarget = RotateTarget;
            clone.AutoFocusOnStart = AutoFocusOnStart;
            clone.AutoFocusOnFilterChange = AutoFocusOnFilterChange;
            clone.AutoFocusAfterSetTime = AutoFocusAfterSetTime;
            clone.AutoFocusSetTime = AutoFocusSetTime;
            clone.AutoFocusAfterSetExposures = AutoFocusAfterSetExposures;
            clone.AutoFocusSetExposures = AutoFocusSetExposures;
            clone.AutoFocusAfterTemperatureChange = AutoFocusAfterTemperatureChange;
            clone.AutoFocusAfterTemperatureChangeAmount = AutoFocusAfterTemperatureChangeAmount;
            clone.AutoFocusAfterHFRChange = AutoFocusAfterHFRChange;
            clone.AutoFocusAfterHFRChangeAmount = AutoFocusAfterHFRChangeAmount;

            if (profileService.ActiveProfile.SequenceSettings.DoMeridianFlip) {
                clone.Triggers.Add(factory.GetTrigger<MeridianFlipTrigger>());
            }

            foreach (var item in clone.Items) {
                item.AttachNewParent(clone);
            }

            foreach (var condition in clone.Conditions) {
                condition.AttachNewParent(clone);
            }

            foreach (var trigger in clone.Triggers) {
                trigger.AttachNewParent(clone);
            }

            return clone;
        }

        public DeepSkyObjectContainer TransformToDSOContainer() {
            var c = factory.GetContainer<DeepSkyObjectContainer>();
            var t = new InputTarget(Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Latitude), Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Longitude));
            t.TargetName = this.Target.TargetName;
            t.InputCoordinates.Coordinates = this.Target.InputCoordinates.Coordinates.Transform(Epoch.J2000);
            t.Rotation = this.Target.Rotation;
            c.Target = t;
            c.Name = t.TargetName;

            var startup = CreateStartupContainer();
            startup.Name = Locale.Loc.Instance["Lbl_OldSequencer_TargetPreparation"];
            if (startup.Items.Count > 0) {
                c.Add(startup);
            }

            var imaging = factory.GetContainer<SequentialContainer>();
            foreach (var condition in Conditions) {
                imaging.Add((ISequenceCondition)condition.Clone());
            }
            foreach (var trigger in Triggers) {
                imaging.Add((ISequenceTrigger)trigger.Clone());
            }
            foreach (var item in Items) {
                var simple = item as SimpleExposure;
                if (simple.Enabled) {
                    imaging.Add(simple.TransformToSmartExposure());
                }
            }
            imaging.Name = Locale.Loc.Instance["Lbl_OldSequencer_TargetImaging"];
            c.Add(imaging);

            var closure = CreateClosureContainer();
            closure.Name = Locale.Loc.Instance["Lbl_OldSequencer_TargetClosure"];
            if (closure.Items.Count > 0) {
                c.Add(closure);
            }

            return c;
        }

        private void Target_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(InputTarget.TargetName)) {
                Name = Target.TargetName;
            }
            AfterParentChanged();
        }

        private async Task<bool> CoordsToFraming() {
            if (Target.DeepSkyObject?.Coordinates != null) {
                var dso = new DeepSkyObject(Target.DeepSkyObject.Name, Target.DeepSkyObject.Coordinates, profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository);
                dso.Rotation = Target.Rotation;
                applicationMediator.ChangeTab(ApplicationTab.FRAMINGASSISTANT);
                return await framingAssistantVM.SetCoordinates(dso);
            }
            return false;
        }

        private async Task<bool> CoordsFromPlanetarium() {
            IPlanetarium s = planetariumFactory.GetPlanetarium();
            DeepSkyObject resp = null;

            try {
                resp = await s.GetTarget();

                if (resp != null) {
                    Target.InputCoordinates.Coordinates = resp.Coordinates;
                    Target.TargetName = resp.Name;
                    this.Name = resp.Name;
                    Target.Rotation = 0;

                    if (s.CanGetRotationAngle) {
                        double rotationAngle = await s.GetRotationAngle();

                        if (!double.IsNaN(rotationAngle)) {
                            Target.Rotation = rotationAngle;
                        }
                    }
                    Notification.ShowSuccess(string.Format(Locale.Loc.Instance["LblPlanetariumCoordsOk"], s.Name));
                }
            } catch (PlanetariumObjectNotSelectedException) {
                Logger.Error($"Attempted to get coordinates from {s.Name} when no object was selected");
                Notification.ShowError(string.Format(Locale.Loc.Instance["LblPlanetariumObjectNotSelected"], s.Name));
            } catch (PlanetariumFailedToConnect ex) {
                Logger.Error($"Unable to connect to {s.Name}: {ex}");
                Notification.ShowError(string.Format(Locale.Loc.Instance["LblPlanetariumFailedToConnect"], s.Name));
            } catch (Exception ex) {
                Logger.Error($"Failed to get coordinates from {s.Name}: {ex}");
                Notification.ShowError(string.Format(Locale.Loc.Instance["LblPlanetariumCoordsError"], s.Name));
            }

            return (resp != null);
        }

        public ICommand AddSimpleExposureCommand { get; private set; }
        public ICommand RemoveSimpleExposureCommand { get; private set; }
        public ICommand ResetSimpleExposureCommand { get; private set; }
        public ICommand PromoteSimpleExposureCommand { get; private set; }
        public ICommand DemoteSimpleExposureCommand { get; private set; }
        public ICommand CoordsToFramingCommand { get; private set; }
        public ICommand CoordsFromPlanetariumCommand { get; private set; }
        public string FileName { get; set; }
    }
}