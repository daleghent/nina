#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Locale;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Equipment.MyFlatDevice;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Profile.Interfaces;
using NINA.Utility;
using NINA.Astrometry;
using NINA.ViewModel.Interfaces;
using Nito.AsyncEx;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using NINA.Core.Enum;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Core.Utility;
using NINA.Core.Interfaces;
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Equipment.Model;
using NINA.Image.ImageAnalysis;
using NINA.Core.Utility.Notification;
using NINA.Image.FileFormat;
using NINA.Core.Utility.WindowService;
using NINA.Profile;
using NINA.Astrometry.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Equipment.Equipment;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.ViewModel;
using NINA.WPF.Base.Model;
using NINA.WPF.Base.Utility.AutoFocus;

namespace NINA.ViewModel.FlatWizard {

    internal class FlatWizardVM : DockableVM, IFlatWizardVM {
        private readonly IApplicationStatusMediator applicationStatusMediator;
        private readonly IFlatDeviceMediator flatDeviceMediator;
        private readonly IImagingVM imagingVM;
        private readonly IFilterWheelMediator filterWheelMediator;
        private readonly ICameraMediator cameraMediator;
        private readonly ITelescopeMediator telescopeMediator;
        private readonly IProgress<ApplicationStatus> progress;
        private readonly IFlatWizardUserPromptVM errorDialog;
        private readonly IMyMessageBoxVM messageBox;
        private readonly ITwilightCalculator twilightCalculator;
        private ObserveAllCollection<FilterInfo> watchedFilterList;
        private const int MAX_TRIES = 6;
        private const int EXPOSURE_TIME_PRECISION = 5;

        public FlatWizardVM(IProfileService profileService,
                            IImagingVM imagingVM,
                            IFlatWizardUserPromptVM errorDialog,
                            ICameraMediator cameraMediator,
                            IFilterWheelMediator filterWheelMediator,
                            ITelescopeMediator telescopeMediator,
                            IFlatDeviceMediator flatDeviceMediator,
                            IImageGeometryProvider imageGeometryProvider,
                            IApplicationStatusMediator applicationStatusMediator,
                            IMyMessageBoxVM messageBox,
                            ITwilightCalculator twilightCalculator) : base(profileService) {
            Title = Loc.Instance["LblFlatWizard"];
            ImageGeometry = imageGeometryProvider.GetImageGeometry("FlatWizardSVG");

            var pauseTokenSource = new PauseTokenSource();

            progress = new Progress<ApplicationStatus>(p => Status = p);
            StartFlatSequenceCommand = new AsyncCommand<bool>(() => CaptureForSingleFilter(pauseTokenSource.Token), (object o) => cameraInfo.Connected && cameraMediator.IsFreeToCapture(this));
            StartMultiFlatSequenceCommand = new AsyncCommand<bool>(() => CaptureForSelectedFilters(pauseTokenSource.Token), (object o) => cameraInfo.Connected && filterWheelInfo.Connected && cameraMediator.IsFreeToCapture(this));
            SlewToZenithCommand = new AsyncCommand<bool>(() => SlewToZenith(CancellationToken.None), (object o) => telescopeInfo.Connected);

            CancelFlatExposureSequenceCommand = new RelayCommand(CancelFindExposureTime);
            PauseFlatExposureSequenceCommand = new RelayCommand(obj => { IsPaused = true; pauseTokenSource.IsPaused = IsPaused; });
            ResumeFlatExposureSequenceCommand = new RelayCommand(obj => { IsPaused = false; pauseTokenSource.IsPaused = IsPaused; });

            FlatCount = profileService.ActiveProfile.FlatWizardSettings.FlatCount;
            DarkFlatCount = profileService.ActiveProfile.FlatWizardSettings.DarkFlatCount;
            BinningMode = profileService.ActiveProfile.FlatWizardSettings.BinningMode;
            AltitudeSite = profileService.ActiveProfile.FlatWizardSettings.AltitudeSite;
            FlatWizardMode = profileService.ActiveProfile.FlatWizardSettings.FlatWizardMode;

            profileService.ProfileChanged += (sender, args) => {
                UpdateSingleFlatWizardFilterSettings();
                watchedFilterList.CollectionChanged -= FiltersCollectionChanged;
                watchedFilterList = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
                watchedFilterList.CollectionChanged += FiltersCollectionChanged;
                UpdateFilterWheelsSettings();
            };

            watchedFilterList = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
            watchedFilterList.CollectionChanged += FiltersCollectionChanged;

            // first update filters
            UpdateSingleFlatWizardFilterSettings();
            UpdateFilterWheelsSettings();

            // then register consumers and get the cameraInfo so it's populated to all filters including the singleflatwizardfiltersettings
            this.cameraMediator = cameraMediator;
            this.cameraMediator.RegisterConsumer(this);
            this.filterWheelMediator = filterWheelMediator;
            this.filterWheelMediator.RegisterConsumer(this);
            this.telescopeMediator = telescopeMediator;
            this.telescopeMediator.RegisterConsumer(this);
            this.flatDeviceMediator = flatDeviceMediator;
            this.flatDeviceMediator.RegisterConsumer(this);

            this.applicationStatusMediator = applicationStatusMediator;
            this.imagingVM = imagingVM;
            this.errorDialog = errorDialog;
            this.messageBox = messageBox;
            this.twilightCalculator = twilightCalculator;

            TargetName = "FlatWizard";
        }

        public void Dispose() {
            imagingVM.Dispose();
            cameraMediator.RemoveConsumer(this);
            filterWheelMediator.RemoveConsumer(this);
            telescopeMediator.RemoveConsumer(this);
            flatDeviceMediator.RemoveConsumer(this);
        }

        public AltitudeSite AltitudeSite {
            get => profileService.ActiveProfile.FlatWizardSettings.AltitudeSite;
            set {
                if (profileService.ActiveProfile.FlatWizardSettings.AltitudeSite != value) {
                    profileService.ActiveProfile.FlatWizardSettings.AltitudeSite = value;
                    RaisePropertyChanged();
                }
            }
        }

        private async Task<bool> SlewToZenith(CancellationToken token) {
            var latitude = Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Latitude);
            var longitude = Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Longitude);
            var azimuth = AltitudeSite == AltitudeSite.WEST ? Angle.ByDegree(90) : Angle.ByDegree(270);
            await telescopeMediator.SlewToCoordinatesAsync(new TopocentricCoordinates(azimuth, Angle.ByDegree(89), latitude, longitude), token);
            telescopeMediator.SetTrackingEnabled(false);
            return true;
        }

        private void UpdateSingleFlatWizardFilterSettings() {
            if (SingleFlatWizardFilterSettings != null) {
                SingleFlatWizardFilterSettings.Settings.PropertyChanged -= UpdateProfileValues;
            }

            var bitDepth = GetBitDepth();

            SingleFlatWizardFilterSettings = new FlatWizardFilterSettingsWrapper(null, new FlatWizardFilterSettings {
                HistogramMeanTarget = profileService.ActiveProfile.FlatWizardSettings.HistogramMeanTarget,
                HistogramTolerance = profileService.ActiveProfile.FlatWizardSettings.HistogramTolerance,
                MaxFlatExposureTime = profileService.ActiveProfile.CameraSettings.MaxFlatExposureTime,
                MinFlatExposureTime = profileService.ActiveProfile.CameraSettings.MinFlatExposureTime,
                StepSize = profileService.ActiveProfile.FlatWizardSettings.StepSize
            }, bitDepth, cameraInfo, flatDeviceInfo);
            SingleFlatWizardFilterSettings.Settings.PropertyChanged += UpdateProfileValues;
        }

        private int GetBitDepth() {
            var bitDepth = cameraInfo?.BitDepth ?? (int)profileService.ActiveProfile.CameraSettings.BitDepth;
            if (profileService.ActiveProfile.CameraSettings.BitScaling) bitDepth = 16;

            return bitDepth;
        }

        private BinningMode binningMode;

        public BinningMode BinningMode {
            get => binningMode;
            set {
                binningMode = value;
                if (!Equals(binningMode, profileService.ActiveProfile.FlatWizardSettings.BinningMode)) {
                    profileService.ActiveProfile.FlatWizardSettings.BinningMode = binningMode;
                }

                RaisePropertyChanged();
            }
        }

        private double calculatedExposureTime;

        public double CalculatedExposureTime {
            get => calculatedExposureTime;
            set {
                calculatedExposureTime = value;
                RaisePropertyChanged();
            }
        }

        private double calculatedHistogramMean;

        public double CalculatedHistogramMean {
            get => calculatedHistogramMean;
            set {
                calculatedHistogramMean = value;
                RaisePropertyChanged();
            }
        }

        private bool cameraConnected;

        public bool CameraConnected {
            get => cameraConnected;
            set {
                cameraConnected = value;
                RaisePropertyChanged();
            }
        }

        private string targetName;

        public string TargetName {
            get => targetName;
            set {
                targetName = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<FilterInfo> FilterInfos => new ObservableCollection<FilterInfo>(Filters.Select(f => f.Filter).ToList());

        private ObservableCollection<FlatWizardFilterSettingsWrapper> filters;

        public ObservableCollection<FlatWizardFilterSettingsWrapper> Filters {
            get => filters ?? (filters = new ObservableCollection<FlatWizardFilterSettingsWrapper>());
            set {
                filters = value;
                RaisePropertyChanged();
            }
        }

        private int flatCount;

        public int FlatCount {
            get => flatCount;
            set {
                flatCount = value;
                if (flatCount != profileService.ActiveProfile.FlatWizardSettings.FlatCount) {
                    profileService.ActiveProfile.FlatWizardSettings.FlatCount = flatCount;
                }

                RaisePropertyChanged();
            }
        }

        private int darkFlatCount;

        public int DarkFlatCount {
            get => darkFlatCount;
            set {
                darkFlatCount = value;
                if (darkFlatCount != profileService.ActiveProfile.FlatWizardSettings.DarkFlatCount) {
                    profileService.ActiveProfile.FlatWizardSettings.DarkFlatCount = darkFlatCount;
                }
                RaisePropertyChanged();
            }
        }

        private int gain = -1;

        public int Gain {
            get => gain;
            set {
                gain = value;
                RaisePropertyChanged();
            }
        }

        private BitmapSource image;

        public BitmapSource Image {
            get => image;
            set {
                image = value;
                RaisePropertyChanged();
            }
        }

        private bool isPaused;

        public bool IsPaused {
            get => isPaused;
            set {
                isPaused = value;
                RaisePropertyChanged();
            }
        }

        private int mode;

        public int Mode {
            get => mode;
            set {
                mode = value;
                RaisePropertyChanged();
            }
        }

        public FilterInfo SelectedFilter {
            get => singleFlatWizardFilterSettings.Filter;
            set {
                singleFlatWizardFilterSettings.Filter = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(SingleFlatWizardFilterSettings));
            }
        }

        private CameraInfo cameraInfo;

        public CameraInfo CameraInfo {
            get => cameraInfo ?? DeviceInfo.CreateDefaultInstance<CameraInfo>();
            set {
                cameraInfo = value;
                RaisePropertyChanged();
            }
        }

        private FlatWizardFilterSettingsWrapper singleFlatWizardFilterSettings;

        public FlatWizardFilterSettingsWrapper SingleFlatWizardFilterSettings {
            get => singleFlatWizardFilterSettings;
            set {
                singleFlatWizardFilterSettings = value;
                RaisePropertyChanged();
            }
        }

        private ApplicationStatus status;
        private ApplicationStatus prevStatus;

        public ApplicationStatus Status {
            get => status;
            set {
                status = value;
                if (status.Source == null) {
                    status.Source = Loc.Instance["LblFlatWizardCapture"];
                } else if (status.Source == Title) {
                    if (prevStatus != null) {
                        if (string.IsNullOrWhiteSpace(status.Status) && (status.Status2 != null || status.Status3 != null)) {
                            status.Status = prevStatus.Status;
                        }

                        if (status.Status2 == null && prevStatus.Status2 != null) {
                            status.Status2 = prevStatus.Status2;
                            status.Progress2 = prevStatus.Progress2;
                            status.MaxProgress2 = prevStatus.MaxProgress2;
                            status.ProgressType2 = prevStatus.ProgressType2;
                        }
                        if (status.Status3 == null && prevStatus.Status3 != null) {
                            status.Status3 = prevStatus.Status3;
                            status.Progress3 = prevStatus.Progress3;
                            status.MaxProgress3 = prevStatus.MaxProgress3;
                            status.ProgressType3 = prevStatus.ProgressType3;
                        }
                    }
                    prevStatus = status;
                }

                RaisePropertyChanged();

                applicationStatusMediator.StatusUpdate(status);
            }
        }

        private bool pauseBetweenFilters;

        public bool PauseBetweenFilters {
            get => pauseBetweenFilters;
            set {
                pauseBetweenFilters = value;
                RaisePropertyChanged();
            }
        }

        public FlatWizardMode FlatWizardMode {
            get => profileService.ActiveProfile.FlatWizardSettings.FlatWizardMode;
            set {
                if (profileService.ActiveProfile.FlatWizardSettings.FlatWizardMode != value) {
                    profileService.ActiveProfile.FlatWizardSettings.FlatWizardMode = value;
                    RaisePropertyChanged();
                }
            }
        }

        private FlatDeviceInfo flatDeviceInfo;

        public FlatDeviceInfo FlatDeviceInfo {
            get => flatDeviceInfo;
            private set {
                flatDeviceInfo = value;
                RaisePropertyChanged();
            }
        }

        private CancellationTokenSource flatSequenceCts = new CancellationTokenSource();

        private void CancelFindExposureTime(object obj) {
            flatSequenceCts?.Cancel();
        }

        public async Task<double> FindFlatExposureTime(PauseToken pt, FlatWizardFilterSettingsWrapper filter) {
            var result = 0d;
            var dataPoints = new List<ScatterErrorPoint>();
            double GetNextDataPoint() {
                var trendLine = new Trendline(dataPoints);
                return Math.Round((filter.Settings.HistogramMeanTarget * HistogramMath.CameraBitDepthToAdu(filter.BitDepth) - trendLine.Offset) / trendLine.Slope, 2);
            }

            progress.Report(new ApplicationStatus { Status = string.Format(Loc.Instance["LblFlatExposureCalcStart"], filter.Settings.MinFlatExposureTime), Source = Title });

            Task showImageTask = null;
            var exposureTime = filter.Settings.MinFlatExposureTime;
            HistogramMath.ExposureAduState exposureAduState;
            do {
                // Set flat panel brightness to static brightness
                if (flatDeviceInfo?.Connected == true) {
                    await flatDeviceMediator.SetBrightness(filter.Settings.MaxAbsoluteFlatDeviceBrightness, flatSequenceCts.Token);
                }

                var sequence = new CaptureSequence(exposureTime, CaptureSequence.ImageTypes.FLAT, filter.Filter, BinningMode, 1) { Gain = Gain };

                var captureImageTask = imagingVM.CaptureImage(sequence, flatSequenceCts.Token, progress);

                //stretch the last image while the next exposure is taken
                if (showImageTask != null) {
                    await Task.WhenAll(captureImageTask, showImageTask);
                } else {
                    await captureImageTask;
                }

                var imageData = await captureImageTask.Result.ToImageData(progress, flatSequenceCts.Token);
                showImageTask = Task.Run(() => {
                    Image = imagingVM.PrepareImage(imageData, new PrepareImageParameters(true, false), flatSequenceCts.Token).Result.Image;
                }, flatSequenceCts.Token);

                var imageStatistics = await imageData.Statistics.Task;
                exposureAduState = HistogramMath.GetExposureAduState(imageStatistics.Mean,
                    filter.Settings.HistogramMeanTarget, filter.BitDepth, filter.Settings.HistogramTolerance);
                dataPoints.Add(new ScatterErrorPoint(exposureTime, imageStatistics.Mean, 1, 1));

                switch (exposureAduState) {
                    case HistogramMath.ExposureAduState.ExposureWithinBounds:
                        CalculatedHistogramMean = imageStatistics.Mean;
                        result = exposureTime;
                        if (flatDeviceInfo?.Connected == true) {
                            var actualGain = Gain == -1 ? profileService.ActiveProfile.CameraSettings.Gain ?? -1 : Gain;
                            profileService.ActiveProfile.FlatDeviceSettings.AddBrightnessInfo(
                                new FlatDeviceFilterSettingsKey(filter.Filter?.Position, BinningMode, actualGain),
                                new FlatDeviceFilterSettingsValue(filter.Settings.MaxAbsoluteFlatDeviceBrightness, exposureTime));
                            Logger.Debug($"Recording flat settings as filter position {filter.Filter?.Position} ({filter.Filter?.Name}), binning: {BinningMode}, " +
                                         $"gain: {actualGain}, panel brightness {filter.Settings.MaxAbsoluteFlatDeviceBrightness} and exposure time: {exposureTime}.");
                        }

                        progress.Report(new ApplicationStatus {
                            Status = string.Format(Loc.Instance["LblFlatExposureCalcFinished"],
                                Math.Round(CalculatedHistogramMean), Math.Round(result, EXPOSURE_TIME_PRECISION)),
                            Source = Title
                        });
                        break;

                    case HistogramMath.ExposureAduState.ExposureBelowLowerBound:
                        switch (dataPoints.Count) {
                            case int n when n < 2:
                                exposureTime += filter.Settings.StepSize;
                                break;

                            case int n when n >= 2 && n < MAX_TRIES:
                                exposureTime = GetNextDataPoint();
                                //less than 0 only happens with the simulator camera, because changing time does not change adu there
                                if (exposureTime < 0 || exposureTime > filter.Settings.MaxFlatExposureTime) await ShowErrorAndHandleInput(Loc.Instance["LblFlatUserPromptFlatTooDim"], exposureTime);
                                break;

                            default:
                                await ShowErrorAndHandleInput(Loc.Instance["LblFLatUserPromptAdjustStepSize"], exposureTime);
                                break;
                        }
                        progress.Report(new ApplicationStatus {
                            Status = string.Format(Loc.Instance["LblFlatExposureCalcContinue"],
                                Math.Round(imageStatistics.Mean), Math.Round(exposureTime, EXPOSURE_TIME_PRECISION)),
                            Source = Title
                        });

                        break;

                    case HistogramMath.ExposureAduState.ExposureAboveUpperBound:
                        if (dataPoints.Count < MAX_TRIES) {
                            exposureTime = GetNextDataPoint();
                            if (exposureTime < 0 || exposureTime > filter.Settings.MaxFlatExposureTime) await ShowErrorAndHandleInput(Loc.Instance["LblFlatUserPromptFlatTooBright"], exposureTime);
                        } else {
                            await ShowErrorAndHandleInput(Loc.Instance["LblFLatUserPromptAdjustStepSize"], exposureTime);
                        }
                        progress.Report(new ApplicationStatus {
                            Status = string.Format(Loc.Instance["LblFlatExposureCalcContinue"],
                                Math.Round(imageStatistics.Mean), Math.Round(exposureTime, EXPOSURE_TIME_PRECISION)),
                            Source = Title
                        });
                        break;

                    default:
                        Logger.Error($"Invalid Histogram State {exposureAduState}.");
                        throw new ArgumentOutOfRangeException();
                }

                async Task ShowErrorAndHandleInput(string message, double time) {
                    errorDialog.Message = message;
                    errorDialog.CurrentMean = imageStatistics.Mean;
                    errorDialog.CameraBitDepth = HistogramMath.CameraBitDepthToAdu(filter.BitDepth);
                    errorDialog.Settings = filter;
                    errorDialog.ExpectedExposureTime = time;
                    errorDialog.ExpectedBrightness = filter.Settings.MaxAbsoluteFlatDeviceBrightness;
                    errorDialog.FlatWizardMode = FlatWizardMode;
                    errorDialog.CameraInfo = cameraInfo;
                    errorDialog.FlatDeviceInfo = flatDeviceInfo;

                    await WindowService.ShowDialog(errorDialog, Loc.Instance["LblFlatUserPromptFailure"],
                        ResizeMode.NoResize, WindowStyle.ToolWindow);
                    switch (errorDialog.Result) {
                        case FlatWizardUserPromptResult.Continue:
                            progress.Report(new ApplicationStatus {
                                Status = string.Format(Loc.Instance["LblFlatExposureCalcContinue"],
                                    Math.Round(imageStatistics.Mean), Math.Round(time, EXPOSURE_TIME_PRECISION)),
                                Source = Title
                            });
                            break;

                        case FlatWizardUserPromptResult.ResetAndContinue:
                            dataPoints.Clear();
                            exposureTime = filter.Settings.MinFlatExposureTime;
                            break;

                        case FlatWizardUserPromptResult.Cancel:
                            flatSequenceCts.Cancel();
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                await WaitWhilePaused(pt);

                // collect garbage to reduce ram usage
                GC.Collect();
                GC.WaitForPendingFinalizers();
                // throw a cancellation if user requested a cancel as well
                flatSequenceCts.Token.ThrowIfCancellationRequested();
            } while (exposureAduState != HistogramMath.ExposureAduState.ExposureWithinBounds);

            return result;
        }

        public async Task<int> FindFlatDeviceBrightness(PauseToken pt, FlatWizardFilterSettingsWrapper filter) {
            if (flatDeviceInfo?.Connected != true) throw new Exception(Loc.Instance["LblFlatDeviceNotConnected"]); ;
            var result = 0;
            var dataPoints = new List<ScatterErrorPoint>();
            int GetNextDataPoint() {
                var trendLine = new ParabolicTrendline(dataPoints);
                var solution = trendLine.Solve(filter.Settings.HistogramMeanTarget * HistogramMath.CameraBitDepthToAdu(filter.BitDepth));
                return solution[0] > 0 ? (int)Math.Round(solution[0]) : (int)Math.Round(solution[1]);
            }

            progress.Report(new ApplicationStatus { Status = string.Format(Loc.Instance["LblFlatExposureCalcStartBrightness"], $"{filter.Settings.MinAbsoluteFlatDeviceBrightness}"), Source = Title });

            Task showImageTask = null;
            var brightness = filter.Settings.MinAbsoluteFlatDeviceBrightness;
            HistogramMath.ExposureAduState exposureAduState;
            do {
                await flatDeviceMediator.SetBrightness(brightness, flatSequenceCts.Token);

                var sequence = new CaptureSequence(filter.Settings.MaxFlatExposureTime, CaptureSequence.ImageTypes.FLAT, filter.Filter, BinningMode, 1) { Gain = Gain };

                var captureImageTask = imagingVM.CaptureImage(sequence, flatSequenceCts.Token, progress);

                //stretch the last image while the next exposure is taken
                if (showImageTask != null) {
                    await Task.WhenAll(captureImageTask, showImageTask);
                } else {
                    await captureImageTask;
                }

                var imageData = await captureImageTask.Result.ToImageData(progress, flatSequenceCts.Token);
                showImageTask = Task.Run(() => {
                    Image = imagingVM.PrepareImage(imageData, new PrepareImageParameters(true, false), flatSequenceCts.Token).Result.Image;
                }, flatSequenceCts.Token);

                var imageStatistics = await imageData.Statistics.Task;
                exposureAduState = HistogramMath.GetExposureAduState(imageStatistics.Mean,
                    filter.Settings.HistogramMeanTarget, filter.BitDepth, filter.Settings.HistogramTolerance);
                dataPoints.Add(new ScatterErrorPoint(brightness, imageStatistics.Mean, 1, 1));

                switch (exposureAduState) {
                    case HistogramMath.ExposureAduState.ExposureWithinBounds:
                        CalculatedHistogramMean = imageStatistics.Mean;
                        result = brightness;
                        if (flatDeviceInfo?.Connected == true) {
                            var actualGain = Gain == -1 ? profileService.ActiveProfile.CameraSettings.Gain ?? -1 : Gain;
                            profileService.ActiveProfile.FlatDeviceSettings.AddBrightnessInfo(
                                new FlatDeviceFilterSettingsKey(filter.Filter?.Position, BinningMode, actualGain),
                                new FlatDeviceFilterSettingsValue(result, filter.Settings.MaxFlatExposureTime));
                            Logger.Debug($"Recording flat settings as filter position {filter.Filter?.Position} ({filter.Filter?.Name}), binning: {BinningMode}, " +
                                         $"gain: {actualGain}, panel brightness {result} and exposure time: {filter.Settings.MaxFlatExposureTime}.");
                        }

                        progress.Report(new ApplicationStatus {
                            Status = string.Format(Loc.Instance["LblFlatExposureCalcFinishedBrightness"],
                                Math.Round(CalculatedHistogramMean), $"{result}"),
                            Source = Title
                        });
                        break;

                    case HistogramMath.ExposureAduState.ExposureBelowLowerBound:
                        switch (dataPoints.Count) {
                            case int n when n < 2:
                                brightness += filter.Settings.FlatDeviceAbsoluteStepSize;
                                break;

                            case int n when n >= 2 && n < MAX_TRIES:
                                brightness = GetNextDataPoint();
                                if (brightness > filter.Settings.MaxAbsoluteFlatDeviceBrightness) await ShowErrorAndHandleInput(Loc.Instance["LblFlatUserPromptFlatTooDim"], brightness);
                                break;

                            default:
                                await ShowErrorAndHandleInput(Loc.Instance["LblFLatUserPromptAdjustStepSize"], brightness);
                                break;
                        }
                        progress.Report(new ApplicationStatus {
                            Status = string.Format(Loc.Instance["LblFlatExposureCalcContinueBrightness"],
                                Math.Round(imageStatistics.Mean), $"{brightness}"),
                            Source = Title
                        });
                        break;

                    case HistogramMath.ExposureAduState.ExposureAboveUpperBound:
                        switch (dataPoints.Count) {
                            case int n when n < 2:
                                await ShowErrorAndHandleInput(Loc.Instance["LblFlatUserPromptFlatTooBright"], brightness);
                                break;

                            case int n when n >= 2 && n < MAX_TRIES:
                                brightness = GetNextDataPoint();
                                if (brightness > filter.Settings.MaxAbsoluteFlatDeviceBrightness) await ShowErrorAndHandleInput(Loc.Instance["LblFlatUserPromptFlatTooBright"], brightness);
                                break;

                            default:
                                await ShowErrorAndHandleInput(Loc.Instance["LblFLatUserPromptAdjustStepSize"], brightness);
                                break;
                        }
                        progress.Report(new ApplicationStatus {
                            Status = string.Format(Loc.Instance["LblFlatExposureCalcContinueBrightness"],
                                Math.Round(imageStatistics.Mean), $"{brightness}"),
                            Source = Title
                        });
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                async Task ShowErrorAndHandleInput(string message, double aBrightness) {
                    errorDialog.Message = message;
                    errorDialog.CurrentMean = imageStatistics.Mean;
                    errorDialog.CameraBitDepth = HistogramMath.CameraBitDepthToAdu(filter.BitDepth);
                    errorDialog.Settings = filter;
                    errorDialog.ExpectedExposureTime = filter.Settings.MaxFlatExposureTime;
                    errorDialog.ExpectedBrightness = aBrightness;
                    errorDialog.FlatWizardMode = FlatWizardMode;
                    errorDialog.CameraInfo = cameraInfo;
                    errorDialog.FlatDeviceInfo = flatDeviceInfo;

                    await WindowService.ShowDialog(errorDialog, Loc.Instance["LblFlatUserPromptFailure"],
                        ResizeMode.NoResize, WindowStyle.ToolWindow);
                    switch (errorDialog.Result) {
                        case FlatWizardUserPromptResult.Continue:
                            progress.Report(new ApplicationStatus {
                                Status = string.Format(Loc.Instance["LblFlatExposureCalcContinueBrightness"],
                                    Math.Round(imageStatistics.Mean), $"{aBrightness}"),
                                Source = Title
                            });
                            break;

                        case FlatWizardUserPromptResult.ResetAndContinue:
                            dataPoints.Clear();
                            brightness = filter.Settings.MinAbsoluteFlatDeviceBrightness;
                            break;

                        case FlatWizardUserPromptResult.Cancel:
                            flatSequenceCts.Cancel();
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                await WaitWhilePaused(pt);

                // collect garbage to reduce ram usage
                GC.Collect();
                GC.WaitForPendingFinalizers();
                // throw a cancellation if user requested a cancel as well
                flatSequenceCts.Token.ThrowIfCancellationRequested();
            } while (exposureAduState != HistogramMath.ExposureAduState.ExposureWithinBounds);

            return result;
        }

        private async Task<bool> CaptureForSelectedFilters(PauseToken pt) {
            try {
                cameraMediator.RegisterCaptureBlock(this);
                return await StartFlatMagic(Filters.Where(f => f.IsChecked), pt);
            } finally {
                cameraMediator.ReleaseCaptureBlock(this);
            }
        }

        private async Task<bool> CaptureForSingleFilter(PauseToken pt) {
            try {
                cameraMediator.RegisterCaptureBlock(this);
                return await StartFlatMagic(new List<FlatWizardFilterSettingsWrapper> { SingleFlatWizardFilterSettings }, pt);
            } finally {
                cameraMediator.ReleaseCaptureBlock(this);
            }
        }

        public async Task<bool> StartFlatMagic(IEnumerable<FlatWizardFilterSettingsWrapper> filterWrappers, PauseToken pt) {
            try {
                if (!HasWritePermission(profileService.ActiveProfile.ImageFileSettings.FilePath)) return false;
                flatSequenceCts?.Dispose();
                flatSequenceCts = new CancellationTokenSource();
                var filterCount = 0;
                if ((flatDeviceInfo?.Connected & flatDeviceInfo?.SupportsOpenClose) == true) {
                    await flatDeviceMediator.CloseCover(flatSequenceCts.Token);
                }

                if (flatDeviceInfo?.Connected == true) {
                    await flatDeviceMediator.ToggleLight(true, flatSequenceCts.Token);
                }

                var regularTimes = new Dictionary<FlatWizardFilterSettingsWrapper, (double time, double brightness)>();
                var skyFlatTimes = new Dictionary<FlatWizardFilterSettingsWrapper, List<double>>();
                var wrappers = filterWrappers.ToList();
                foreach (var filter in wrappers) {
                    filterCount++;
                    if (PauseBetweenFilters) {
                        var dialogResult = messageBox.Show(
                            string.Format(Loc.Instance["LblPrepFlatFilterMsgBox"], filter.Filter?.Name ?? string.Empty),
                            Loc.Instance["LblFlatWizard"], MessageBoxButton.OKCancel, MessageBoxResult.OK);
                        if (dialogResult == MessageBoxResult.Cancel)
                            throw new OperationCanceledException();
                    }

                    progress.Report(new ApplicationStatus {
                        Status2 = $"{Loc.Instance["LblFilter"]} {filter.Filter?.Name ?? string.Empty}",
                        Progress2 = filterCount,
                        MaxProgress2 = wrappers.Count,
                        ProgressType2 = ApplicationStatus.StatusProgressType.ValueOfMaxValue,
                        Status3 = Loc.Instance["LblExposures"],
                        Progress3 = 0,
                        MaxProgress3 = 0,
                        ProgressType3 = ApplicationStatus.StatusProgressType.ValueOfMaxValue,
                        Source = Title
                    });

                    try {
                        double time;
                        int brightness;
                        switch (FlatWizardMode) {
                            case FlatWizardMode.DYNAMICEXPOSURE:
                                time = await FindFlatExposureTime(pt, filter);
                                brightness = filter.Settings.MaxAbsoluteFlatDeviceBrightness;
                                await TakeRegularFlats(regularTimes, time, brightness, filter, pt);
                                break;

                            case FlatWizardMode.DYNAMICBRIGHTNESS:
                                time = filter.Settings.MaxFlatExposureTime;
                                brightness = await FindFlatDeviceBrightness(pt, filter);
                                await TakeRegularFlats(regularTimes, time, brightness, filter, pt);

                                break;

                            case FlatWizardMode.SKYFLAT:
                                time = await FindFlatExposureTime(pt, filter);
                                await TakeSkyFlats(skyFlatTimes, time, filter, pt);
                                break;

                            default:
                                Logger.Error($"Invalid flat wizard mode {FlatWizardMode}.");
                                throw new ArgumentOutOfRangeException();
                        }
                    } catch (OperationCanceledException) {
                        Logger.Debug("Capturing flats cancelled.");
                    } finally {
                        CalculatedExposureTime = 0;
                        CalculatedHistogramMean = 0;
                    }
                }

                await TakeDarkFlats(regularTimes, skyFlatTimes, pt);
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(ex.Message);
                return false;
            } finally {
                if (flatDeviceInfo?.Connected == true) { await flatDeviceMediator.ToggleLight(false, flatSequenceCts.Token); }
                imagingVM.DestroyImage();
                Image = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
                progress.Report(new ApplicationStatus());
                progress.Report(new ApplicationStatus { Source = Title });
            }

            return true;
        }

        private async Task TakeRegularFlats(IDictionary<FlatWizardFilterSettingsWrapper, (double time, double brightness)> exposureTimes, double time, int brightness, FlatWizardFilterSettingsWrapper filter, PauseToken pt) {
            CalculatedExposureTime = time;
            exposureTimes.Add(filter, (time, brightness));

            if (flatDeviceInfo?.Connected == true) {
                await flatDeviceMediator.SetBrightness(brightness, flatSequenceCts.Token);
            }
            var flatSequence = new CaptureSequence(time, CaptureSequence.ImageTypes.FLAT, filter.Filter, BinningMode, FlatCount) { Gain = Gain };
            await CaptureImages(flatSequence, pt);

            filter.IsChecked = false;
        }

        /// <summary>
        /// This method will take twilight sky flat exposures by adjusting the exposure time based on the changing sky conditions during the runtime.
        /// A paper which explains the math behind the algorithm can be found here
        /// http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.56.3178&rep=rep1&type=pdf
        /// </summary>
        /// <param name="skyFlatTimes"></param>
        /// <param name="firstExposureTime"></param>
        /// <param name="filter"></param>
        /// <param name="pt"></param>
        /// <remarks></remarks>
        /// <returns></returns>
        private async Task TakeSkyFlats(
                IDictionary<FlatWizardFilterSettingsWrapper,
                List<double>> skyFlatTimes,
                double firstExposureTime,
                FlatWizardFilterSettingsWrapper filter,
                PauseToken pt) {
            var stopWatch = Stopwatch.StartNew();

            var springTwilight = twilightCalculator.GetTwilightDuration(new DateTime(DateTime.Now.Year, 03, 20), 30.0, 0d).TotalMilliseconds;
            var todayTwilight = twilightCalculator.GetTwilightDuration(DateTime.Now, profileService.ActiveProfile.AstrometrySettings.Latitude, profileService.ActiveProfile.AstrometrySettings.Longitude).TotalMilliseconds;

            var tau = todayTwilight / springTwilight;
            var k = DateTime.Now.Hour < 12 ? 0.094 / 60 : -0.094 / 60;

            var s = firstExposureTime;
            var a = Math.Pow(10, k / tau);

            CalculatedExposureTime = firstExposureTime;
            var exposureTimes = new List<double>();

            Task showImageTask = null;
            Task saveTask = null;
            var time = firstExposureTime;
            for (var i = 0; i < FlatCount; i++) {
                progress.Report(new ApplicationStatus {
                    Status3 = Loc.Instance["LblExposures"],
                    Progress3 = i + 1,
                    MaxProgress3 = FlatCount,
                    ProgressType3 = ApplicationStatus.StatusProgressType.ValueOfMaxValue,
                    Source = Title
                });

                var sequence = new CaptureSequence(time, CaptureSequence.ImageTypes.FLAT, filter.Filter, BinningMode, FlatCount) { Gain = Gain };
                var ti = stopWatch.Elapsed;
                var captureImageTask = imagingVM.CaptureImage(sequence, flatSequenceCts.Token, progress);
                //stretch the last image while the next exposure is taken
                if (showImageTask != null) {
                    await Task.WhenAll(captureImageTask, showImageTask);
                } else {
                    await captureImageTask;
                }

                var imageData = await captureImageTask.Result.ToImageData(progress, flatSequenceCts.Token);
                imageData.MetaData.Target.Name = TargetName;
                showImageTask = Task.Run(() => {
                    Image = imagingVM.PrepareImage(imageData, new PrepareImageParameters(true, false), flatSequenceCts.Token).Result.Image;
                }, flatSequenceCts.Token);

                var imageStatistics = await imageData.Statistics.Task;
                switch (
                        HistogramMath.GetExposureAduState(
                            imageStatistics.Mean,
                            filter.Settings.HistogramMeanTarget,
                            filter.BitDepth,
                            filter.Settings.HistogramTolerance)
                        ) {
                    case HistogramMath.ExposureAduState.ExposureBelowLowerBound:
                    case HistogramMath.ExposureAduState.ExposureAboveUpperBound:
                        Logger.Warning($"Skyflat correction did not work and is outside of tolerance: " +
                                     $"first exposure time {firstExposureTime}, " +
                                     $"current exposure time {time}, " +
                                     $"elapsed time: {stopWatch.ElapsedMilliseconds / 1000d}, " +
                                     $"current mean adu: {imageStatistics.Mean}. " +
                                     $"The sky flat exposure time will be determined again and the exposure will be repeated.");
                        Notification.ShowWarning($"Skyflat correction did not work and is outside of tolerance:" + Environment.NewLine +
                                     $"first exposure time {firstExposureTime:#.####}" + Environment.NewLine +
                                     $"current exposure time {time:#.####}, " + Environment.NewLine +
                                     $"elapsed time: {stopWatch.ElapsedMilliseconds / 1000d:#.##}" + Environment.NewLine +
                                     $"mean adu: {imageStatistics.Mean:#.##}." + Environment.NewLine +
                                     $"The sky flat exposure time will be determined again and the exposure will be repeated.");

                        firstExposureTime = await FindFlatExposureTime(pt, filter);
                        k = DateTime.Now.Hour < 12 ? 0.094 / 60 : -0.094 / 60;
                        a = Math.Pow(10, k / tau);
                        s = firstExposureTime;
                        CalculatedExposureTime = firstExposureTime;
                        time = firstExposureTime;
                        Logger.Info($"New exposure time for sky flat determined - {time}");
                        stopWatch = Stopwatch.StartNew();
                        i--;
                        continue;
                }

                exposureTimes.Add(time);
                if (saveTask != null && !saveTask.IsCompleted) {
                    progress.Report(new ApplicationStatus { Status = Loc.Instance["LblSavingImage"] });
                    await saveTask;
                }

                saveTask = imageData.SaveToDisk(new FileSaveInfo(profileService), flatSequenceCts.Token);

                progress.Report(new ApplicationStatus { Status = Loc.Instance["LblSavingImage"] });
                var trot = stopWatch.Elapsed - ti - TimeSpan.FromMilliseconds(time * 1000d);
                if (trot.TotalMilliseconds < 0) trot = TimeSpan.FromMilliseconds(0);

                var tiPlus1 = Math.Log(Math.Pow(a, ti.TotalMilliseconds / 1000d + trot.TotalMilliseconds / 1000d) + s * Math.Log(a))
                              / Math.Log(a);
                time = tiPlus1 - (ti + trot).TotalMilliseconds / 1000d;

                //TODO: Move this to Trace, once confirmed working well
                Logger.Info($"ti:{ti}, trot:{trot}, tiPlus1:{tiPlus1}, eiPlus1:{time}");
            }
            skyFlatTimes.Add(filter, exposureTimes);
        }

        private async Task TakeDarkFlats(Dictionary<FlatWizardFilterSettingsWrapper, (double time, double brightness)> exposureTimes,
            Dictionary<FlatWizardFilterSettingsWrapper, List<double>> skyFlatTimes, PauseToken pt) {
            if ((exposureTimes.Count > 0 || skyFlatTimes.Count > 0) && DarkFlatCount > 0) {
                progress.Report(new ApplicationStatus { Status = Loc.Instance["LblPreparingDarkFlatSequence"], Source = Title });
                if (flatDeviceInfo?.Connected == true) { await flatDeviceMediator.ToggleLight(false, flatSequenceCts.Token); }
                if ((flatDeviceInfo?.Connected & flatDeviceInfo?.SupportsOpenClose) == true && profileService.ActiveProfile.FlatDeviceSettings.OpenForDarkFlats) { await flatDeviceMediator.OpenCover(flatSequenceCts.Token); }
                var dialogResult = messageBox.Show(Loc.Instance["LblCoverScopeMsgBox"],
                    Loc.Instance["LblCoverScopeMsgBoxTitle"], MessageBoxButton.OKCancel, MessageBoxResult.OK);
                if (dialogResult == MessageBoxResult.OK) {
                    var filterCount = 0;
                    foreach (var keyValuePair in exposureTimes) {
                        filterCount++;
                        var filterName = keyValuePair.Key.Filter?.Name ?? string.Empty;
                        progress.Report(new ApplicationStatus {
                            Status2 = $"{Loc.Instance["LblFilter"]} {filterName}",
                            Progress2 = filterCount,
                            MaxProgress2 = exposureTimes.Count,
                            ProgressType2 = ApplicationStatus.StatusProgressType.ValueOfMaxValue,
                            Status3 = Loc.Instance["LblExposures"],
                            Progress3 = 0,
                            MaxProgress3 = 0,
                            ProgressType3 = ApplicationStatus.StatusProgressType.ValueOfMaxValue,
                            Source = Title
                        });

                        var darkFlatsSequence = new CaptureSequence(keyValuePair.Value.time, CaptureSequence.ImageTypes.DARKFLAT,
                            keyValuePair.Key.Filter, BinningMode, DarkFlatCount) { Gain = Gain };
                        await CaptureImages(darkFlatsSequence, pt);
                    }

                    filterCount = 0;
                    foreach (var keyValuePair in skyFlatTimes) {
                        filterCount++;
                        var filterName = keyValuePair.Key.Filter?.Name ?? string.Empty;
                        progress.Report(new ApplicationStatus {
                            Status2 = $"{Loc.Instance["LblFilter"]} {filterName}",
                            Progress2 = filterCount,
                            MaxProgress2 = exposureTimes.Count,
                            ProgressType2 = ApplicationStatus.StatusProgressType.ValueOfMaxValue,
                            Status3 = Loc.Instance["LblExposures"],
                            Progress3 = 0,
                            MaxProgress3 = 0,
                            ProgressType3 = ApplicationStatus.StatusProgressType.ValueOfMaxValue,
                            Source = Title
                        });
                        foreach (var singleDarkFlatSequence in keyValuePair.Value.Select(time => new CaptureSequence(time, CaptureSequence.ImageTypes.DARKFLAT,
                            keyValuePair.Key.Filter, BinningMode, 1) { Gain = Gain })) {
                            await CaptureImages(singleDarkFlatSequence, pt);
                        }
                    }
                }
            }
        }

        private async Task CaptureImages(CaptureSequence sequence, PauseToken pt) {
            Task saveTask = null;
            Task showImageTask = null;
            while (sequence.ProgressExposureCount < sequence.TotalExposureCount) {
                progress.Report(new ApplicationStatus {
                    Status3 = Loc.Instance["LblExposures"],
                    Progress3 = sequence.ProgressExposureCount + 1,
                    MaxProgress3 = sequence.TotalExposureCount,
                    ProgressType3 = ApplicationStatus.StatusProgressType.ValueOfMaxValue,
                    Source = Title
                });

                var captureImageTask = imagingVM.CaptureImage(sequence, flatSequenceCts.Token, progress);

                //stretch the last image while the next exposure is taken
                if (showImageTask != null) {
                    await Task.WhenAll(captureImageTask, showImageTask);
                } else {
                    await captureImageTask;
                }

                var imageData = await captureImageTask.Result.ToImageData(progress, flatSequenceCts.Token);
                imageData.MetaData.Target.Name = TargetName;
                showImageTask = Task.Run(() => {
                    Image = imagingVM.PrepareImage(imageData, new PrepareImageParameters(true, false), flatSequenceCts.Token).Result.Image;
                }, flatSequenceCts.Token);

                if (saveTask != null && !saveTask.IsCompleted) {
                    progress.Report(new ApplicationStatus { Status = Loc.Instance["LblSavingImage"] });
                    await saveTask;
                }

                saveTask = imageData.SaveToDisk(new FileSaveInfo(profileService), flatSequenceCts.Token);

                sequence.ProgressExposureCount++;

                await WaitWhilePaused(pt);

                flatSequenceCts.Token.ThrowIfCancellationRequested();
            }

            if (saveTask == null || saveTask.IsCompleted) return;
            progress.Report(new ApplicationStatus { Status = Loc.Instance["LblSavingImage"] });
            await Task.WhenAll(saveTask, showImageTask);
        }

        public IWindowService WindowService { get; set; } = new WindowService();

        private void FiltersCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            if (watchedFilterList.Count != filters.Count) {
                UpdateFilterWheelsSettings();
            }
        }

        private void UpdateFilterWheelsSettings() {
            var selectedFilter = SelectedFilter?.Position;
            Filters.Clear();
            foreach (var filter in profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters
                .Select(s => new FlatWizardFilterSettingsWrapper(s, s.FlatWizardFilterSettings, GetBitDepth(),
                    cameraInfo, flatDeviceInfo))) {
                Filters.Add(filter);
            }

            SelectedFilter = Filters.FirstOrDefault(f => f.Filter?.Position == selectedFilter)?.Filter;

            RaisePropertyChanged(nameof(Filters));
            RaisePropertyChanged(nameof(FilterInfos));
        }

        private void UpdateProfileValues(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (Math.Abs(profileService.ActiveProfile.FlatWizardSettings.HistogramMeanTarget - SingleFlatWizardFilterSettings.Settings.HistogramMeanTarget) > 0.1) {
                profileService.ActiveProfile.FlatWizardSettings.HistogramMeanTarget = SingleFlatWizardFilterSettings.Settings.HistogramMeanTarget;
            }

            if (Math.Abs(profileService.ActiveProfile.FlatWizardSettings.HistogramTolerance - SingleFlatWizardFilterSettings.Settings.HistogramTolerance) > 0.1) {
                profileService.ActiveProfile.FlatWizardSettings.HistogramTolerance = SingleFlatWizardFilterSettings.Settings.HistogramTolerance;
            }

            if (Math.Abs(profileService.ActiveProfile.CameraSettings.MaxFlatExposureTime - SingleFlatWizardFilterSettings.Settings.MaxFlatExposureTime) > 0.0001) {
                profileService.ActiveProfile.CameraSettings.MaxFlatExposureTime = SingleFlatWizardFilterSettings.Settings.MaxFlatExposureTime;
            }

            if (Math.Abs(profileService.ActiveProfile.CameraSettings.MinFlatExposureTime - SingleFlatWizardFilterSettings.Settings.MinFlatExposureTime) > 0.0001) {
                profileService.ActiveProfile.CameraSettings.MinFlatExposureTime = SingleFlatWizardFilterSettings.Settings.MinFlatExposureTime;
            }

            if (Math.Abs(profileService.ActiveProfile.FlatWizardSettings.StepSize - SingleFlatWizardFilterSettings.Settings.StepSize) > 0.0001) {
                profileService.ActiveProfile.FlatWizardSettings.StepSize = SingleFlatWizardFilterSettings.Settings.StepSize;
            }
        }

        public bool HasWritePermission(string dir) {
            if (!Directory.Exists(dir)) {
                Notification.ShowError(Loc.Instance["LblDirectoryNotFound"]);
                return false;
            }

            try {
                using (var fs = File.Create(Path.Combine(dir, Path.GetRandomFileName()), 1,
                    FileOptions.DeleteOnClose)) {
                }
                return true;
            } catch (UnauthorizedAccessException) {
                Notification.ShowError(Loc.Instance["LblDirectoryNotWritable"]);
                return false;
            }
        }

        private async Task WaitWhilePaused(PauseToken pt) {
            if (!pt.IsPaused) return;
            IsPaused = true;
            progress.Report(new ApplicationStatus { Status = Loc.Instance["LblPaused"], Source = Title });
            await pt.WaitWhilePausedAsync(flatSequenceCts.Token);
            IsPaused = false;
        }

        public void UpdateDeviceInfo(CameraInfo deviceInfo) {
            var prevBitDepth = cameraInfo?.BitDepth ?? 0;
            CameraInfo = deviceInfo;
            CameraConnected = cameraInfo?.Connected ?? false;

            if (prevBitDepth == cameraInfo?.BitDepth) return;
            var bitDepth = GetBitDepth();
            SingleFlatWizardFilterSettings.BitDepth = bitDepth;
            foreach (var filter in Filters) {
                filter.BitDepth = bitDepth;
            }
        }

        private FilterWheelInfo filterWheelInfo;

        public void UpdateDeviceInfo(FilterWheelInfo deviceInfo) {
            filterWheelInfo = deviceInfo;
        }

        private TelescopeInfo telescopeInfo;

        public void UpdateDeviceInfo(TelescopeInfo deviceInfo) {
            telescopeInfo = deviceInfo;
        }

        public void UpdateDeviceInfo(FlatDeviceInfo deviceInfo) {
            FlatDeviceInfo = deviceInfo;
            if (FlatDeviceInfo.Connected) {
                AdjustSettingForBrightness(SingleFlatWizardFilterSettings);

                foreach (var setting in Filters) {
                    AdjustSettingForBrightness(setting);
                }
            }
        }

        private void AdjustSettingForBrightness(FlatWizardFilterSettingsWrapper setting) {
            if (setting.Settings.MinAbsoluteFlatDeviceBrightness < FlatDeviceInfo.MinBrightness) {
                setting.Settings.MinAbsoluteFlatDeviceBrightness = FlatDeviceInfo.MinBrightness;
            }
            if (setting.Settings.MaxAbsoluteFlatDeviceBrightness < FlatDeviceInfo.MinBrightness) {
                setting.Settings.MaxAbsoluteFlatDeviceBrightness = FlatDeviceInfo.MinBrightness;
            }

            if (setting.Settings.MinAbsoluteFlatDeviceBrightness > FlatDeviceInfo.MaxBrightness) {
                setting.Settings.MinAbsoluteFlatDeviceBrightness = FlatDeviceInfo.MaxBrightness;
            }
            if (setting.Settings.MaxAbsoluteFlatDeviceBrightness > FlatDeviceInfo.MaxBrightness) {
                setting.Settings.MaxAbsoluteFlatDeviceBrightness = FlatDeviceInfo.MaxBrightness;
            }
        }

        public RelayCommand CancelFlatExposureSequenceCommand { get; }
        public RelayCommand PauseFlatExposureSequenceCommand { get; }
        public RelayCommand ResumeFlatExposureSequenceCommand { get; }
        public IAsyncCommand StartFlatSequenceCommand { get; }
        public IAsyncCommand StartMultiFlatSequenceCommand { get; }
        public IAsyncCommand SlewToZenithCommand { get; }
    }
}