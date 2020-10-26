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
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.Container.ExecutionStrategy;
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

    internal class SequenceVM : DockableVM, ISequenceVM, ICameraConsumer, IRotatorConsumer {

        public SequenceVM(
                IProfileService profileService,
                ISequenceMediator sequenceMediator,
                ICameraMediator cameraMediator,
                IRotatorMediator rotatorMediator,
                IApplicationStatusMediator applicationStatusMediator,
                INighttimeCalculator nighttimeCalculator,
                IPlanetariumFactory planetariumFactory,
                IDeepSkyObjectSearchVM deepSkyObjectSearchVM,
                IFramingAssistantVM framingAssistantVM,
                IApplicationMediator applicationMediator
        ) : base(profileService) {
            this.applicationMediator = applicationMediator;
            this.rotatorMediator = rotatorMediator;
            this.rotatorMediator.RegisterConsumer(this);

            this.sequenceMediator = sequenceMediator;
            this.sequenceMediator.RegisterConstructor(this);

            this.cameraMediator = cameraMediator;
            this.cameraMediator.RegisterConsumer(this);
            this.nighttimeCalculator = nighttimeCalculator;
            this.NighttimeData = this.nighttimeCalculator.Calculate();
            this.planetariumFactory = planetariumFactory;
            this.DeepSkyObjectSearchVM = deepSkyObjectSearchVM;
            this.framingAssistantVM = framingAssistantVM;
            this.applicationMediator = applicationMediator;

            this.DeepSkyObjectSearchVM.PropertyChanged += DeepSkyObjectDetailVM_PropertyChanged;

            this.profileService = profileService;
            Title = "LblSequence";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current?.Resources["SequenceSVG"];

            AddSequenceRowCommand = new RelayCommand(AddSequenceRow);
            AddTargetCommand = new RelayCommand(AddDefaultTarget);
            RemoveTargetCommand = new RelayCommand(RemoveTarget);
            ResetTargetCommand = new RelayCommand(ResetTarget, ResetTargetEnabled);
            RemoveSequenceRowCommand = new RelayCommand(RemoveSequenceRow);
            PromoteSequenceRowCommand = new RelayCommand(PromoteSequenceRow);
            DemoteSequenceRowCommand = new RelayCommand(DemoteSequenceRow);
            ResetSequenceRowCommand = new RelayCommand(ResetSequenceRow, ResetSequenceRowEnabled);
            BuildSequenceCommand = new AsyncCommand<bool>(async () => {
                await BuildSequence();
                return true;
            });
            SaveSequenceCommand = new RelayCommand(SaveSequence);
            SaveAsSequenceCommand = new RelayCommand(SaveAsSequence);
            LoadSequenceCommand = new RelayCommand(LoadSequence);
            PromoteTargetCommand = new RelayCommand(PromoteTarget);
            DemoteTargetCommand = new RelayCommand(DemoteTarget);
            SaveTargetSetCommand = new RelayCommand(SaveTargetSet);
            LoadTargetSetCommand = new RelayCommand(LoadTargetSet);
            CoordsFromPlanetariumCommand = new AsyncCommand<bool>(() => Task.Run(CoordsFromPlanetarium));
            CoordsToFramingCommand = new AsyncCommand<bool>(() => Task.Run(CoordsToFraming));
            ImportTargetsCommand = new RelayCommand(ImportTargets);
            OpenSequenceCommandAtCompletionDiagCommand = new RelayCommand(OpenSequenceCommandAtCompletionDiag);

            autoUpdateTimer = new DispatcherTimer(DispatcherPriority.Background);
            autoUpdateTimer.Interval = TimeSpan.FromSeconds(1);
            autoUpdateTimer.IsEnabled = true;
            autoUpdateTimer.Tick += (sender, args) => CalculateETA();

            profileService.LocationChanged += (object sender, EventArgs e) => {
                foreach (var seq in this.Targets) {
                    var dso = new DeepSkyObject(seq.DSO.Name, seq.DSO.Coordinates, profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository);
                    dso.SetDateAndPosition(Utility.Astrometry.NighttimeCalculator.GetReferenceDate(DateTime.Now), profileService.ActiveProfile.AstrometrySettings.Latitude, profileService.ActiveProfile.AstrometrySettings.Longitude);
                    seq.SetSequenceTarget(dso);
                }
            };

            autoUpdateTimer.Start();

            PropertyChanged += SequenceVM_PropertyChanged;
            EstimatedDownloadTime = profileService.ActiveProfile.SequenceSettings.EstimatedDownloadTime;
        }

        private void ImportTargets(object obj) {
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
                            this.Targets.Clear();
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
                                template.TargetName = name;
                                template.Coordinates = new Coordinates(Angle.ByDegree(ra), Angle.ByDegree(dec), Epoch.J2000);
                                template.Rotation = angle;
                                template.MarkAsUnchanged();
                                this.Targets.Add(template);
                            }
                            Sequence = Targets.First();
                        } else if (Array.FindIndex(headerColumns, x => x.ToLower() == "familiar name") > -1) {
                            this.Targets.Clear();
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
                                template.TargetName = string.IsNullOrWhiteSpace(name) ? columns[idxCatalogue] : name;
                                template.Coordinates = new Coordinates(Angle.ByDegree(ra), Angle.ByDegree(dec), Epoch.J2000);
                                if (idxAngle >= 0 && !string.IsNullOrWhiteSpace(columns[idxAngle])) {
                                    //Nina orientation is not east of north, but flipped
                                    var angle = 360 - Astrometry.EuclidianModulus(double.Parse(columns[idxAngle], CultureInfo.InvariantCulture), 360);
                                    template.Rotation = angle;
                                } else {
                                    template.Rotation = 0;
                                }
                                template.Rotation = 0;
                                template.MarkAsUnchanged();
                                this.Targets.Add(template);
                            }
                            Sequence = Targets.First();
                        } else {
                            Notification.ShowError(Locale.Loc.Instance["LblUnknownImportFormat"]);
                        }
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(Locale.Loc.Instance["LblUnknownImportFormat"]);
                }
            }
        }

        private void OpenSequenceCommandAtCompletionDiag(object o) {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Title = Locale.Loc.Instance["LblSequenceCommandAtCompletionTitle"];
            dialog.FileName = "SequenceCompleteCommand";
            dialog.DefaultExt = ".*";
            dialog.Filter = "Any executable command |*.*";

            if (dialog.ShowDialog() == true) {
                ActiveProfile.SequenceSettings.SequenceCompleteCommand = dialog.FileName;
            }
        }

        private readonly IFramingAssistantVM framingAssistantVM;
        private readonly IApplicationMediator applicationMediator;

        private void SequenceVM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(Sequence))
                ActiveSequenceChanged();
        }

        private void DeepSkyObjectDetailVM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(DeepSkyObjectSearchVM.SelectedTargetSearchResult)) {
                if (DeepSkyObjectSearchVM.SelectedTargetSearchResult != null) {
                    Sequence.PropertyChanged -= Sequence_PropertyChanged;

                    Sequence.TargetName = DeepSkyObjectSearchVM.SelectedTargetSearchResult.Column1;
                    Sequence.Coordinates = DeepSkyObjectSearchVM.Coordinates;

                    Sequence.PropertyChanged += Sequence_PropertyChanged;
                }
            }
        }

        private DispatcherTimer autoUpdateTimer;

        private void RemoveTarget(object obj) {
            if (this.Targets.Count > 0) {
                var l = (CaptureSequenceList)obj;

                if (l.HasChanged) {
                    if (MyMessageBox.MyMessageBox.Show(
                        string.Format(Locale.Loc.Instance["LblChangedSequenceWarning"], l.TargetName),
                        Locale.Loc.Instance["LblChangedSequenceWarningTitle"], System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel) != System.Windows.MessageBoxResult.OK) {
                        return;
                    }
                }

                var switchTab = false;
                if (Object.Equals(l, Sequence)) {
                    switchTab = true;
                }
                this.Targets.Remove(l);
                if (switchTab) {
                    Sequence = this.Targets.FirstOrDefault();
                }
            }
        }

        private void ResetTarget(object obj) {
            var target = (CaptureSequenceList)obj;
            foreach (CaptureSequence cs in target) {
                cs.ProgressExposureCount = 0;
            }

            target.IsFinished = false;
        }

        private bool ResetTargetEnabled(object obj) {
            var target = (CaptureSequenceList)obj;
            foreach (CaptureSequence cs in target) {
                if (cs.ProgressExposureCount != 0) return true;
            }
            return false;
        }

        private void AddDefaultTarget(object obj) {
            this.Targets.Add(GetTemplate());
            Sequence = Targets.Last();
        }

        public void AddTarget(DeepSkyObject deepSkyObject) {
            var target = GetTemplate();
            target.SetSequenceTarget(deepSkyObject);
            this.Targets.Add(target);
            Sequence = Targets.Last();
        }

        private void PromoteTarget(object obj) {
            if (Sequence != null) {
                // Promoting a target moves it earlier in the sequence so the UI moves it to the left
                int activeTargetIndex = Targets.IndexOf(Sequence);
                if (activeTargetIndex > 0)
                    Targets.Move(activeTargetIndex, activeTargetIndex - 1);
            }
        }

        private void DemoteTarget(object obj) {
            if (Sequence != null) {
                // Demoting a target moves it later in the sequence so the UI moves it to the right
                int activeTargetIndex = Targets.IndexOf(Sequence);
                if ((activeTargetIndex < Targets.Count - 1) && (activeTargetIndex > -1))
                    Targets.Move(activeTargetIndex, activeTargetIndex + 1);
            }
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
                CaptureSequenceList.SaveSequenceSet(Targets, dialog.FileName);
            }
        }

        private void LoadTargetSet(object obj) {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Multiselect = false;
            dialog.Title = Locale.Loc.Instance["LblLoadTargetSet"];
            dialog.InitialDirectory = profileService.ActiveProfile.SequenceSettings.DefaultSequenceFolder;
            dialog.FileName = "";
            dialog.DefaultExt = "ninaTargetSet";
            dialog.Filter = "N.I.N.A target set files|*." + dialog.DefaultExt;

            if (dialog.ShowDialog() == true) {
                using (var s = new FileStream(dialog.FileName, FileMode.Open)) {
                    Targets = new AsyncObservableCollection<CaptureSequenceList>(CaptureSequenceList.LoadSequenceSet(
                        s,
                        profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters,
                        profileService.ActiveProfile.AstrometrySettings.Latitude,
                        profileService.ActiveProfile.AstrometrySettings.Longitude
                    ));
                    foreach (var l in Targets) {
                        AdjustCaptureSequenceListForSynchronization(l);
                    }
                    Sequence = Targets.FirstOrDefault();
                }
            }
        }

        private void LoadSequence(object obj) {
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
                    AdjustCaptureSequenceListForSynchronization(l);
                    this.Targets.Add(l);
                    l.MarkAsUnchanged();

                    // set the last one loaded as the current sequence
                    Sequence = l;
                }
            }
        }

        private bool isUsingSynchronizedGuider;

        public bool IsUsingSynchronizedGuider {
            get => isUsingSynchronizedGuider;
            set {
                isUsingSynchronizedGuider = value;
                RaisePropertyChanged();
            }
        }

        public bool OKtoExit() {
            if (Targets.Any(t => t.HasChanged))
                if (MyMessageBox.MyMessageBox.Show(
                    string.Format(Locale.Loc.Instance["LblChangedSequenceWarning"],
                        Targets.Where(t => t.HasChanged)
                                .Aggregate("", (list, t) => list + ", " + t.TargetName)
                                .TrimStart(new char[] { ',', ' ' })),
                    Locale.Loc.Instance["LblChangedSequenceWarningTitle"], System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel) != System.Windows.MessageBoxResult.OK)
                    return false;

            return true;
        }

        private void SaveSequence(object obj) {
            // SaveSequence now saves only the active sequence
            if (!Sequence.HasFileName) {
                SaveAsSequence(obj);
            } else {
                Sequence.Save(Sequence.SequenceFileName);
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
            dialog.FileName = r.Replace(Sequence.TargetName, "-");

            if (dialog.ShowDialog().Value) {
                Sequence.SequenceFileName = dialog.FileName;

                Sequence.Save(Sequence.SequenceFileName);
            }
        }

        public void AddDownloadTime(TimeSpan t) {
            _actualDownloadTimes.Add(t);
            double doubleAverageTicks = _actualDownloadTimes.Average(timeSpan => timeSpan.Ticks);
            long longAverageTicks = Convert.ToInt64(doubleAverageTicks);
            EstimatedDownloadTime = new TimeSpan(longAverageTicks);
        }

        private void CalculateETA() {
            if (Targets.Count > 0) {
                TimeSpan time = new TimeSpan();
                foreach (var seq in Targets) {
                    seq.EstimatedStartTime = DateTime.Now.AddSeconds(time.TotalSeconds);
                    foreach (CaptureSequence cs in seq) {
                        if (cs.Enabled) {
                            var exposureCount = cs.TotalExposureCount - cs.ProgressExposureCount;
                            time = time.Add(
                                TimeSpan.FromSeconds(exposureCount *
                                                     (cs.ExposureTime + EstimatedDownloadTime.TotalSeconds)));
                        }
                    }
                    seq.EstimatedEndTime = DateTime.Now.AddSeconds(time.TotalSeconds);
                    seq.EstimatedDuration = seq.EstimatedEndTime - seq.EstimatedStartTime;
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
                RaisePropertyChanged(nameof(SequenceEstimatedStartTime));
                RaisePropertyChanged(nameof(SequenceEstimatedEndTime));
                RaisePropertyChanged(nameof(SequenceEstimatedDuration));
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
                RaisePropertyChanged(nameof(SequenceEstimatedStartTime));
                RaisePropertyChanged(nameof(SequenceEstimatedEndTime));
                RaisePropertyChanged(nameof(SequenceEstimatedDuration));
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
                RaisePropertyChanged(nameof(SequenceEstimatedStartTime));
                RaisePropertyChanged(nameof(SequenceEstimatedEndTime));
                RaisePropertyChanged(nameof(SequenceEstimatedDuration));
            }
        }

        public DateTime SequenceEstimatedStartTime {
            get {
                return (Sequence != null) ? Sequence.EstimatedStartTime : DateTime.Now;
            }
        }

        public DateTime SequenceEstimatedEndTime {
            get {
                return (Sequence != null) ? Sequence.EstimatedEndTime : DateTime.Now;
            }
        }

        public TimeSpan SequenceEstimatedDuration {
            get {
                return (Sequence != null) ? Sequence.EstimatedDuration : TimeSpan.MinValue;
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

        private Task BuildSequence() {
            return Task.Run(() => {
                // ===============================================
                // ========== USE NEW SEQUENCER ==================
                // ===============================================
                var factory = sequenceMediator.GetFactory();

                var container = factory.GetContainer<SequenceRootContainer>();
                container.Add(factory.GetContainer<StartAreaContainer>());
                container.Add(factory.GetContainer<TargetAreaContainer>());
                container.Add(factory.GetContainer<EndAreaContainer>());

                var startOfSequence = factory.GetContainer<ParallelContainer>();
                startOfSequence.Name = Locale.Loc.Instance["Lbl_OldSequencer_StartOfSequence"];

                if (profileService.ActiveProfile.SequenceSettings.CoolCameraAtSequenceStart) {
                    startOfSequence.Add(factory.GetItem<CoolCamera>());
                }
                if (profileService.ActiveProfile.SequenceSettings.UnparMountAtSequenceStart) {
                    startOfSequence.Add(factory.GetItem<UnparkScope>());
                }
                if (profileService.ActiveProfile.SequenceSettings.OpenDomeShutterAtSequenceStart) {
                    startOfSequence.Add(factory.GetItem<OpenDomeShutter>());
                }
                if (profileService.ActiveProfile.FlatDeviceSettings.OpenAtSequenceStart) {
                    startOfSequence.Add(factory.GetItem<OpenCover>());
                }

                if (startOfSequence.Items.Count > 0) {
                    (container.Items[0] as ISequenceContainer).Add(startOfSequence);
                }

                var hasSkippedRows = false;
                foreach (var target in Targets) {
                    var dsoContainer = factory.GetContainer<DeepSkyObjectContainer>();
                    dsoContainer.IsExpanded = false;
                    dsoContainer.Target.Expanded = false;
                    dsoContainer.Name = target.TargetName;
                    dsoContainer.Target.TargetName = target.TargetName;
                    dsoContainer.Target.Rotation = target.Rotation;
                    dsoContainer.Target.InputCoordinates.Coordinates = target.Coordinates;

                    if (target.Delay > 0) {
                        var delay = factory.GetItem<WaitForTimeSpan>();
                        delay.Time = target.Delay;
                        dsoContainer.Add(delay);
                    }
                    if (target.SlewToTarget) {
                        dsoContainer.Add(factory.GetItem<SlewScopeToRaDec>());
                    }
                    if (target.CenterTarget) {
                        dsoContainer.Add(factory.GetItem<Center>());
                    }
                    if (target.RotateTarget) {
                        dsoContainer.Add(factory.GetItem<CenterAndRotate>());
                    }

                    // Usually when guiding is disabled during autofocus an OAG is used and therefore autofocus should happen before guiding
                    // When guiding is enabled for Autofocus the guiding should start prior to autofocus
                    if (profileService.ActiveProfile.FocuserSettings.AutoFocusDisableGuiding) {
                        if (target.AutoFocusOnStart) {
                            dsoContainer.Add(factory.GetItem<RunAutofocus>());
                        }
                        if (target.StartGuiding) {
                            dsoContainer.Add(factory.GetItem<StartGuiding>());
                        }
                    } else {
                        if (target.StartGuiding) {
                            dsoContainer.Add(factory.GetItem<StartGuiding>());
                        }

                        if (target.AutoFocusOnStart) {
                            dsoContainer.Add(factory.GetItem<RunAutofocus>());
                        }
                    }

                    var imagingContainer = factory.GetContainer<SequentialContainer>();
                    imagingContainer.Name = Locale.Loc.Instance["Lbl_OldSequencer_TargetImaging"];

                    /* Triggers */
                    if (profileService.ActiveProfile.MeridianFlipSettings.Enabled) {
                        var trigger = factory.GetTrigger<MeridianFlipTrigger>();
                        imagingContainer.Add(trigger);
                    }

                    if (target.AutoFocusAfterSetTime) {
                        var trigger = factory.GetTrigger<AutofocusAfterTimeTrigger>();
                        trigger.Amount = target.AutoFocusSetTime;
                        imagingContainer.Add(trigger);
                    }

                    if (target.AutoFocusAfterSetExposures) {
                        var trigger = factory.GetTrigger<AutofocusAfterExposures>();
                        trigger.AfterExposures = (int)target.AutoFocusSetExposures;
                        imagingContainer.Add(trigger);
                    }

                    if (target.AutoFocusAfterTemperatureChange) {
                        var trigger = factory.GetTrigger<AutofocusAfterTemperatureChangeTrigger>();
                        trigger.Amount = target.AutoFocusAfterTemperatureChangeAmount;
                        imagingContainer.Add(trigger);
                    }

                    if (target.AutoFocusAfterHFRChange) {
                        var trigger = factory.GetTrigger<AutofocusAfterHFRIncreaseTrigger>();
                        trigger.Amount = target.AutoFocusAfterHFRChangeAmount;
                        imagingContainer.Add(trigger);
                    }

                    if (target.AutoFocusOnFilterChange) {
                        var trigger = factory.GetTrigger<AutofocusAfterFilterChange>();
                        imagingContainer.Add(trigger);
                    }
                    if (target.Mode == SequenceMode.STANDARD) {
                        foreach (var row in target.Items) {
                            var exposures = row.TotalExposureCount - row.ProgressExposureCount;
                            if (row.Enabled && (exposures > 0)) {
                                var captureContainer = factory.GetContainer<SequentialContainer>();
                                captureContainer.IsExpanded = false;
                                captureContainer.Name = $"{exposures}x{row.ExposureTime}s {row.FilterType?.Name}";

                                if (row.ImageType == CaptureSequence.ImageTypes.FLAT && profileService.ActiveProfile.FlatDeviceSettings.UseWizardTrainedValues) {
                                    var item = factory.GetContainer<TrainedFlatExposure>();
                                    item.GetIterations().Iterations = exposures;
                                    if (row.FilterType != null) {
                                        item.GetSwitchFilterItem().Filter = row.FilterType;
                                    }
                                    var exposure = item.GetExposureItem();
                                    exposure.Gain = row.Gain;
                                    exposure.Offset = row.Offset;
                                    exposure.Binning = row.Binning;
                                    captureContainer.Add(item);
                                } else {
                                    var loop = factory.GetCondition<LoopCondition>();
                                    loop.Iterations = exposures;
                                    captureContainer.Add(loop);

                                    if (row.FilterType != null) {
                                        var filter = factory.GetItem<SwitchFilter>();
                                        filter.Filter = row.FilterType;
                                        captureContainer.Add(filter);
                                    }

                                    var expose = factory.GetItem<TakeExposure>();
                                    expose.ExposureTime = row.ExposureTime;
                                    expose.Binning = row.Binning;
                                    expose.ImageType = row.ImageType;
                                    expose.Gain = row.Gain;
                                    expose.Offset = row.Offset;

                                    captureContainer.Add(expose);

                                    if (row.Dither) {
                                        var dither = factory.GetTrigger<DitherAfterExposures>();
                                        dither.AfterExposures = row.DitherAmount;
                                        captureContainer.Add(dither);
                                    }
                                }

                                imagingContainer.Add(captureContainer);
                            } else {
                                hasSkippedRows = true;
                            }
                        }
                    } else {
                        // loop mode
                        var dict = new Dictionary<string, int>();
                        var max = target.Items.Where(x => x.Enabled == true).Max(y => y.TotalExposureCount - y.ProgressExposureCount);

                        var iterator = 0;
                        do {
                            var min = target.Items.Where(x => x.Enabled == true && (x.TotalExposureCount - x.ProgressExposureCount) > iterator)
                            .Min(y => y.TotalExposureCount - y.ProgressExposureCount);

                            var captureContainer = factory.GetContainer<SequentialContainer>();
                            captureContainer.IsExpanded = false;

                            var dither = false;
                            var ditherAmount = int.MaxValue;

                            foreach (var row in target.Items) {
                                if (!row.Enabled || row.TotalExposureCount - row.ProgressExposureCount <= 0) { hasSkippedRows = true; }

                                if (row.Enabled && ((row.TotalExposureCount - row.ProgressExposureCount - iterator) > 0)) {
                                    captureContainer.Name += $"{min - iterator}x{row.ExposureTime}s {row.FilterType?.Name} - ";

                                    if (row.ImageType == CaptureSequence.ImageTypes.FLAT && profileService.ActiveProfile.FlatDeviceSettings.UseWizardTrainedValues) {
                                        var item = factory.GetContainer<TrainedFlatExposure>();
                                        item.GetIterations().Iterations = 1;
                                        if (row.FilterType != null) {
                                            item.GetSwitchFilterItem().Filter = row.FilterType;
                                        }
                                        var exposure = item.GetExposureItem();
                                        exposure.Gain = row.Gain;
                                        exposure.Offset = row.Offset;
                                        exposure.Binning = row.Binning;
                                        captureContainer.Add(item);
                                    } else {
                                        if (row.FilterType != null) {
                                            var filter = factory.GetItem<SwitchFilter>();
                                            filter.Filter = row.FilterType;
                                            captureContainer.Add(filter);
                                        }

                                        var expose = factory.GetItem<TakeExposure>();
                                        expose.ExposureTime = row.ExposureTime;
                                        expose.Binning = row.Binning;
                                        expose.ImageType = row.ImageType;
                                        expose.Gain = row.Gain;
                                        expose.Offset = row.Offset;

                                        captureContainer.Add(expose);
                                        if (row.Dither) {
                                            dither = true;
                                            ditherAmount = Math.Min(ditherAmount, row.DitherAmount);
                                        }
                                    }
                                }
                            }

                            var loop = factory.GetCondition<LoopCondition>();
                            loop.Iterations = min - iterator;
                            captureContainer.Add(loop);

                            if (dither) {
                                var ditherItem = factory.GetTrigger<DitherAfterExposures>();
                                ditherItem.AfterExposures = ditherAmount;
                                captureContainer.Add(ditherItem);
                            }
                            captureContainer.Name = captureContainer.Name.Remove(captureContainer.Name.Length - 3);

                            imagingContainer.Add(captureContainer);

                            iterator = min;
                        } while (iterator < max);
                    }

                    if (imagingContainer.Items.Count > 0) {
                        dsoContainer.Add(imagingContainer);

                        (container.Items[1] as ISequenceContainer).Add(dsoContainer);
                    }
                }

                if (hasSkippedRows) {
                    Notification.ShowWarning(Locale.Loc.Instance["Lbl_OldSequencer_HasSkippedRows"]);
                }

                var firstContainer = ((container.Items[1] as ISequenceContainer).Items.FirstOrDefault() as IDeepSkyObjectContainer);
                if (firstContainer != null) {
                    firstContainer.IsExpanded = true;
                }

                var endOfSequence = factory.GetContainer<SequentialContainer>();
                endOfSequence.Name = Locale.Loc.Instance["Lbl_OldSequencer_EndOfSequence"];

                var parallelActions = factory.GetContainer<ParallelContainer>();
                parallelActions.Name = Locale.Loc.Instance["Lbl_OldSequencer_ParallelEndOfSequence"];
                if (profileService.ActiveProfile.SequenceSettings.WarmCamAtSequenceEnd) {
                    parallelActions.Add(factory.GetItem<WarmCamera>());
                }
                if (profileService.ActiveProfile.SequenceSettings.ParkMountAtSequenceEnd) {
                    parallelActions.Add(factory.GetItem<ParkScope>());
                }
                if (profileService.ActiveProfile.FlatDeviceSettings.CloseAtSequenceEnd) {
                    parallelActions.Add(factory.GetItem<CloseCover>());
                }
                if (profileService.ActiveProfile.SequenceSettings.ParkDomeAtSequenceEnd) {
                    parallelActions.Add(factory.GetItem<ParkDome>());
                }
                if (profileService.ActiveProfile.SequenceSettings.CloseDomeShutterAtSequenceEnd) {
                    parallelActions.Add(factory.GetItem<CloseDomeShutter>());
                }

                if (parallelActions.Items.Count > 0) {
                    endOfSequence.Add(parallelActions);
                }

                if (ExternalCommandExecutor.CommandExists(profileService.ActiveProfile.SequenceSettings.SequenceCompleteCommand)) {
                    var script = factory.GetItem<ExternalScript>();
                    script.Script = profileService.ActiveProfile.SequenceSettings.SequenceCompleteCommand;
                    endOfSequence.Add(script);
                }

                if (endOfSequence.Items.Count > 0) {
                    (container.Items[2] as ISequenceContainer).Add(endOfSequence);
                }

                sequenceMediator.SetRootContainer(container);

                applicationMediator.ChangeTab(ApplicationTab.SEQUENCE2);
            });
        }

        private async Task<bool> SetSequenceTarget(DeepSkyObject dso) {
            Sequence.PropertyChanged -= Sequence_PropertyChanged;

            var sequenceDso = new DeepSkyObject(dso.AlsoKnownAs.FirstOrDefault() ?? dso.Name ?? string.Empty, dso.Coordinates, profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository);
            sequenceDso.Rotation = dso.Rotation;
            await Task.Run(() => {
                sequenceDso.SetDateAndPosition(Utility.Astrometry.NighttimeCalculator.GetReferenceDate(DateTime.Now), profileService.ActiveProfile.AstrometrySettings.Latitude, profileService.ActiveProfile.AstrometrySettings.Longitude);
            });

            Sequence.SetSequenceTarget(sequenceDso);
            NighttimeData = nighttimeCalculator.Calculate();

            return true;
        }

        public async Task<bool> SetMultipleSequenceTargets(ICollection<DeepSkyObject> deepSkyObjects, bool replace = true) {
            if (replace) {
                Targets.Clear();
                Sequence = null;
            }

            foreach (var dso in deepSkyObjects) {
                AddDefaultTarget(null);
                Sequence = Targets.Last();
                await SetSequenceTarget(dso);
            }
            Sequence = Targets.FirstOrDefault();
            return true;
        }

        private AsyncObservableCollection<CaptureSequenceList> targets;

        public AsyncObservableCollection<CaptureSequenceList> Targets {
            get {
                if (targets == null) {
                    targets = new AsyncObservableCollection<CaptureSequenceList>();
                }
                return targets;
            }
            set {
                targets = value;
                RaisePropertyChanged();
            }
        }

        private void AdjustCaptureSequenceListForSynchronization(CaptureSequenceList csl) {
            if (IsUsingSynchronizedGuider) {
                foreach (var item in csl.Items) {
                    item.Dither = true;
                    item.DitherAmount = 1;
                }
            }
        }

        private CaptureSequenceList GetTemplate() {
            CaptureSequenceList csl = null;
            if (File.Exists(profileService.ActiveProfile.SequenceSettings.TemplatePath)) {
                csl = CaptureSequenceList.Load(profileService.ActiveProfile.SequenceSettings.TemplatePath,
                    profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters,
                    profileService.ActiveProfile.AstrometrySettings.Latitude,
                    profileService.ActiveProfile.AstrometrySettings.Longitude
                );
                AdjustCaptureSequenceListForSynchronization(csl);
            } else {
                var seq = new CaptureSequence();
                csl = new CaptureSequenceList(seq) { TargetName = "Target" };
                csl.DSO?.SetDateAndPosition(
                    Utility.Astrometry.NighttimeCalculator.GetReferenceDate(DateTime.Now),
                    profileService.ActiveProfile.AstrometrySettings.Latitude,
                    profileService.ActiveProfile.AstrometrySettings.Longitude
                );
                AdjustCaptureSequenceListForSynchronization(csl);
            }

            csl.MarkAsUnchanged();
            return csl;
        }

        private CaptureSequenceList _sequence;

        public CaptureSequenceList Sequence {
            get {
                return _sequence;
            }
            set {
                if (_sequence != null) {
                    _sequence.PropertyChanged -= Sequence_PropertyChanged;
                }

                _sequence = value;
                if (_sequence != null) {
                    _sequence.PropertyChanged += Sequence_PropertyChanged;
                }

                RaisePropertyChanged();
            }
        }

        public IDeepSkyObjectSearchVM DeepSkyObjectSearchVM { get; private set; }

        private void Sequence_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(CaptureSequenceList.TargetName)) {
                if (Sequence.TargetName.Length > 1) {
                    DeepSkyObjectSearchVM.TargetName = Sequence.TargetName;
                }
            }
            if (e.PropertyName == nameof(CaptureSequenceList.HasChanged)) {
                RaisePropertyChanged(nameof(SequenceSaveable));
                RaisePropertyChanged(nameof(SequenceModified));
            }
            if (e.PropertyName == nameof(CaptureSequenceList.HasFileName)) {
                RaisePropertyChanged(nameof(HasSequenceFileName));
            }
            if (e.PropertyName == nameof(CaptureSequenceList.EstimatedStartTime)) {
                RaisePropertyChanged("SequenceEstimatedStartTime");
            }
            if (e.PropertyName == nameof(CaptureSequenceList.EstimatedEndTime)) {
                RaisePropertyChanged("SequenceEstimatedEndTime");
            }
        }

        private int _selectedSequenceIdx;

        public int SelectedSequenceRowIdx {
            get {
                return _selectedSequenceIdx;
            }
            set {
                _selectedSequenceIdx = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<string> _imageTypes;
        private ISequenceMediator sequenceMediator;
        private ICameraMediator cameraMediator;
        private INighttimeCalculator nighttimeCalculator;
        private readonly IPlanetariumFactory planetariumFactory;
        private CameraInfo cameraInfo = DeviceInfo.CreateDefaultInstance<CameraInfo>();
        private IRotatorMediator rotatorMediator;
        private RotatorInfo rotatorInfo;

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

        public bool SequenceModified { get { return (Sequence != null) && (Sequence.HasChanged); } }
        public bool HasSequenceFileName { get { return (Sequence != null) && (Sequence.HasFileName); } }
        public bool SequenceSaveable { get { return SequenceModified && HasSequenceFileName; } }

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

        public void AddSequenceRow(object o) {
            if (Sequence != null) {
                CaptureSequence newSeq = Sequence.Items.Any() ? Sequence.Items.Last().Clone() : new CaptureSequence();
                Sequence.Add(newSeq);
                SelectedSequenceRowIdx = Sequence.Count - 1;
                AdjustCaptureSequenceListForSynchronization(Sequence);
            }
        }

        private void RemoveSequenceRow(object obj) {
            if (Sequence != null) {
                var idx = SelectedSequenceRowIdx;
                if (idx > -1) {
                    Sequence.RemoveAt(idx);
                    if (idx < Sequence.Count - 1) {
                        SelectedSequenceRowIdx = idx;
                    } else {
                        SelectedSequenceRowIdx = Sequence.Count - 1;
                    }
                }
            }
        }

        private void ResetSequenceRow(object obj) {
            var idx = SelectedSequenceRowIdx;
            Sequence.ResetAt(idx);
            Sequence.IsFinished = false;
        }

        private bool ResetSequenceRowEnabled(object obj) {
            if (Sequence == null) { return false; }
            var idx = SelectedSequenceRowIdx;
            if (idx < 0 || idx >= Sequence.Items.Count) return false;
            return Sequence.Items[idx].ProgressExposureCount != 0;
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

        public void UpdateDeviceInfo(RotatorInfo deviceInfo) {
            this.rotatorInfo = deviceInfo;
        }

        private async Task<bool> CoordsFromPlanetarium() {
            IPlanetarium s = planetariumFactory.GetPlanetarium();
            DeepSkyObject resp = null;

            try {
                resp = await s.GetTarget();

                if (resp != null) {
                    Sequence.Coordinates = resp.Coordinates;
                    Sequence.TargetName = resp.Name;
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

        private async Task<bool> CoordsToFraming() {
            if (Sequence.Coordinates != null) {
                var dso = new DeepSkyObject(Sequence.TargetName, Sequence.Coordinates, profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository);
                dso.Rotation = Sequence.Rotation;
                applicationMediator.ChangeTab(ApplicationTab.FRAMINGASSISTANT);
                return await framingAssistantVM.SetCoordinates(dso);
            }
            return false;
        }

        public ICommand CoordsFromPlanetariumCommand { get; set; }
        public ICommand CoordsToFramingCommand { get; set; }
        public ICommand OpenSequenceCommandAtCompletionDiagCommand { get; private set; }
        public ICommand AddSequenceRowCommand { get; private set; }
        public ICommand AddTargetCommand { get; private set; }
        public ICommand RemoveTargetCommand { get; private set; }
        public ICommand ResetTargetCommand { get; private set; }
        public ICommand RemoveSequenceRowCommand { get; private set; }
        public ICommand PromoteSequenceRowCommand { get; private set; }
        public ICommand DemoteSequenceRowCommand { get; private set; }

        public ICommand ResetSequenceRowCommand { get; private set; }

        public IAsyncCommand BuildSequenceCommand { get; private set; }

        public ICommand LoadSequenceCommand { get; private set; }
        public ICommand SaveSequenceCommand { get; private set; }
        public ICommand SaveAsSequenceCommand { get; private set; }

        public ICommand PromoteTargetCommand { get; private set; }
        public ICommand DemoteTargetCommand { get; private set; }
        public ICommand SaveTargetSetCommand { get; private set; }
        public ICommand LoadTargetSetCommand { get; private set; }
        public ICommand ImportTargetsCommand { get; private set; }

        private void PromoteSequenceRow(object obj) {
            if (Sequence != null) {
                var idx = SelectedSequenceRowIdx;
                if (idx > 0) {
                    CaptureSequence seq = Sequence.Items[idx];
                    Sequence.RemoveAt(idx);
                    Sequence.AddAt(idx - 1, seq);
                    SelectedSequenceRowIdx = idx - 1;
                }
            }
        }

        private void DemoteSequenceRow(object obj) {
            if (Sequence != null) {
                var idx = SelectedSequenceRowIdx;
                if ((idx < Sequence.Count - 1) && (idx > -1)) {
                    CaptureSequence seq = Sequence.Items[idx];
                    Sequence.RemoveAt(idx);
                    Sequence.AddAt(idx + 1, seq);
                    SelectedSequenceRowIdx = idx + 1;
                }
            }
        }

        internal void ActiveSequenceChanged() {
            // refresh properties that depend on Sequence
            RaisePropertyChanged(nameof(SequenceModified));
            RaisePropertyChanged(nameof(HasSequenceFileName));
            RaisePropertyChanged(nameof(SequenceSaveable));
        }

        public void Dispose() {
            this.rotatorMediator.RemoveConsumer(this);
            this.cameraMediator.RemoveConsumer(this);
        }
    }
}