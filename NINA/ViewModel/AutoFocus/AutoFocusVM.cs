#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Model.MyFocuser;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using NINA.Profile;
using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NINA.Utility.ImageAnalysis;
using NINA.Model.ImageData;
using NINA.Utility.Enum;
using NINA.Utility.Mediator;
using NINA.ViewModel.AutoFocus;
using Newtonsoft.Json;
using System.IO;

namespace NINA.ViewModel {

    internal class AutoFocusVM : DockableVM, ICameraConsumer, IFocuserConsumer, IFilterWheelConsumer, IAutoFocusVM {
        private static readonly string ReportDirectory = Path.Combine(Utility.Utility.APPLICATIONTEMPPATH, "AutoFocus");

        static AutoFocusVM() {
            if (!Directory.Exists(ReportDirectory)) {
                Directory.CreateDirectory(ReportDirectory);
            } else {
                Utility.Utility.DirectoryCleanup(ReportDirectory, TimeSpan.FromDays(-30));
            }
        }

        public AutoFocusVM(
            IProfileService profileService,
            IFocuserMediator focuserMediator,
            IGuiderMediator guiderMediator,
            IImagingMediator imagingMediator,
            IApplicationStatusMediator applicationStatusMediator) : this(profileService, null, null, focuserMediator, guiderMediator, imagingMediator, applicationStatusMediator) {
        }

        public AutoFocusVM(
                IProfileService profileService,
                ICameraMediator cameraMediator,
                IFilterWheelMediator filterWheelMediator,
                IFocuserMediator focuserMediator,
                IGuiderMediator guiderMediator,
                IImagingMediator imagingMediator,
                IApplicationStatusMediator applicationStatusMediator
        ) : base(profileService) {
            Title = "LblAutoFocus";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["AutoFocusSVG"];

            if (cameraMediator != null) {
                this.cameraMediator = cameraMediator;
                this.cameraMediator.RegisterConsumer(this);
            }

            if (filterWheelMediator != null) {
                this.filterWheelMediator = filterWheelMediator;
                this.filterWheelMediator.RegisterConsumer(this);
            }

            this.focuserMediator = focuserMediator;
            this.focuserMediator.RegisterConsumer(this);

            this.imagingMediator = imagingMediator;
            this.guiderMediator = guiderMediator;
            this.applicationStatusMediator = applicationStatusMediator;

            FocusPoints = new AsyncObservableCollection<ScatterErrorPoint>();
            PlotFocusPoints = new AsyncObservableCollection<DataPoint>();

            StartAutoFocusCommand = new AsyncCommand<AutoFocusReport>(
                () =>
                    Task.Run(
                        async () => {
                            cameraMediator.RegisterCaptureBlock(this);
                            try {
                                var result = await StartAutoFocus(CommandInitializization(), _autoFocusCancelToken.Token, new Progress<ApplicationStatus>(p => Status = p));
                                return result;
                            } finally {
                                cameraMediator.ReleaseCaptureBlock(this);
                            }
                        }
                    ),
                (p) => { return focuserInfo?.Connected == true && cameraInfo?.Connected == true && cameraMediator.IsFreeToCapture(this); }
            );
            CancelAutoFocusCommand = new RelayCommand(CancelAutoFocus);
        }

        private CancellationTokenSource _autoFocusCancelToken;
        private AsyncObservableCollection<ScatterErrorPoint> _focusPoints;
        private AsyncObservableCollection<DataPoint> _plotFocusPoints;
        private ICameraMediator cameraMediator;
        private IImagingMediator imagingMediator;
        private IGuiderMediator guiderMediator;
        private IApplicationStatusMediator applicationStatusMediator;
        private List<Accord.Point> brightestStarPositions = new List<Accord.Point>();
        public double AverageContrast { get; private set; }
        public double ContrastStdev { get; private set; }

        public AsyncObservableCollection<ScatterErrorPoint> FocusPoints {
            get {
                return _focusPoints;
            }
            set {
                _focusPoints = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<DataPoint> PlotFocusPoints {
            get {
                return _plotFocusPoints;
            }
            set {
                _plotFocusPoints = value;
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
                _status.Source = Title;
                RaisePropertyChanged();

                this.applicationStatusMediator.StatusUpdate(_status);
            }
        }

        private TrendlineFitting _trendLineFitting;

        public TrendlineFitting TrendlineFitting {
            get => _trendLineFitting;
            set {
                _trendLineFitting = value;
                RaisePropertyChanged();
            }
        }

        private QuadraticFitting _quadraticFitting;

        public QuadraticFitting QuadraticFitting {
            get => _quadraticFitting;
            set {
                _quadraticFitting = value;
                RaisePropertyChanged();
            }
        }

        private HyperbolicFitting _hyperbolicFitting;

        public HyperbolicFitting HyperbolicFitting {
            get {
                return _hyperbolicFitting;
            }
            set {
                _hyperbolicFitting = value;
                RaisePropertyChanged();
            }
        }

        private GaussianFitting _gaussianFitting;

        public GaussianFitting GaussianFitting {
            get {
                return _gaussianFitting;
            }
            set {
                _gaussianFitting = value;
                RaisePropertyChanged();
            }
        }

        private DataPoint _finalFocusPoint;

        public DataPoint FinalFocusPoint {
            get {
                return _finalFocusPoint;
            }
            set {
                _finalFocusPoint = value;
                RaisePropertyChanged();
            }
        }

        private int _focusPosition;

        private FilterInfo CommandInitializization() {
            _autoFocusCancelToken?.Dispose();
            _autoFocusCancelToken = new CancellationTokenSource();
            FilterInfo filter = null;
            if (this.filterInfo?.SelectedFilter != null) {
                filter = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters.Where(x => x.Position == this.filterInfo.SelectedFilter.Position).FirstOrDefault();
            }
            return filter;
        }

        private async Task GetFocusPoints(FilterInfo filter, int nrOfSteps, IProgress<ApplicationStatus> progress, CancellationToken token, int offset = 0) {
            var stepSize = profileService.ActiveProfile.FocuserSettings.AutoFocusStepSize;

            if (offset != 0) {
                //Move to initial position
                _focusPosition = await focuserMediator.MoveFocuserRelative(offset * stepSize);
            }

            var comparer = new FocusPointComparer();
            var plotComparer = new PlotPointComparer();

            for (int i = 0; i < nrOfSteps; i++) {
                token.ThrowIfCancellationRequested();

                MeasureAndError measurement = await GetAverageMeasurement(filter, profileService.ActiveProfile.FocuserSettings.AutoFocusNumberOfFramesPerPoint, token, progress);

                //If star Measurement is 0, we didn't detect any stars or shapes, and want this point to be ignored by the fitting as much as possible. Setting a very high Stdev will do the trick.
                if (measurement.Measure == 0) {
                    measurement.Stdev = 1000;
                }

                token.ThrowIfCancellationRequested();

                FocusPoints.AddSorted(new ScatterErrorPoint(_focusPosition, measurement.Measure, 0, Math.Max(0.001, measurement.Stdev)), comparer);
                PlotFocusPoints.AddSorted(new DataPoint(_focusPosition, measurement.Measure), plotComparer);
                if (i < nrOfSteps - 1) {
                    Logger.Trace("Moving focuser to next autofocus position");
                    _focusPosition = await focuserMediator.MoveFocuserRelative(-stepSize);
                }

                token.ThrowIfCancellationRequested();

                TrendlineFitting = new TrendlineFitting().Calculate(FocusPoints);

                if (profileService.ActiveProfile.FocuserSettings.AutoFocusMethod == AFMethodEnum.STARHFR) {
                    if (FocusPoints.Count() >= 3
                        && (profileService.ActiveProfile.FocuserSettings.AutoFocusCurveFitting == Utility.Enum.AFCurveFittingEnum.PARABOLIC
                        || profileService.ActiveProfile.FocuserSettings.AutoFocusCurveFitting == Utility.Enum.AFCurveFittingEnum.TRENDPARABOLIC)) {
                        QuadraticFitting = new QuadraticFitting().Calculate(FocusPoints);
                    }
                    if (FocusPoints.Count() >= 3
                        && (profileService.ActiveProfile.FocuserSettings.AutoFocusCurveFitting == Utility.Enum.AFCurveFittingEnum.HYPERBOLIC
                        || profileService.ActiveProfile.FocuserSettings.AutoFocusCurveFitting == Utility.Enum.AFCurveFittingEnum.TRENDHYPERBOLIC)) {
                        HyperbolicFitting = new HyperbolicFitting().Calculate(FocusPoints);
                    }
                } else if (FocusPoints.Count() >= 3) {
                    GaussianFitting = new GaussianFitting().Calculate(FocusPoints);
                }
            }
        }

        private async Task<IRenderedImage> TakeExposure(FilterInfo filter, CancellationToken token, IProgress<ApplicationStatus> progress) {
            Logger.Trace("Starting Exposure for autofocus");
            double expTime = profileService.ActiveProfile.FocuserSettings.AutoFocusExposureTime;
            if (filter != null && filter.AutoFocusExposureTime > 0) {
                expTime = filter.AutoFocusExposureTime;
            }
            var seq = new CaptureSequence(expTime, CaptureSequence.ImageTypes.SNAPSHOT, filter, null, 1);
            seq.EnableSubSample = _setSubSample;
            seq.Binning = new BinningMode(profileService.ActiveProfile.FocuserSettings.AutoFocusBinning, profileService.ActiveProfile.FocuserSettings.AutoFocusBinning);

            bool autoStretch = true;
            //If using contrast based statistics, no need to stretch
            if (profileService.ActiveProfile.FocuserSettings.AutoFocusMethod == AFMethodEnum.CONTRASTDETECTION && profileService.ActiveProfile.FocuserSettings.ContrastDetectionMethod == ContrastDetectionMethodEnum.Statistics) {
                autoStretch = false;
            }
            var prepareParameters = new PrepareImageParameters(autoStretch: autoStretch, detectStars: false);
            IRenderedImage image;
            try {
                image = await imagingMediator.CaptureAndPrepareImage(seq, prepareParameters, token, progress);
            } catch (Exception e) {
                if (!_setSubSample) {
                    throw e;
                }

                Logger.Warning("Camera error, trying without subsample");
                Logger.Warning(e.Message);
                _setSubSample = false;
                seq.EnableSubSample = _setSubSample;
                image = await imagingMediator.CaptureAndPrepareImage(seq, prepareParameters, token, progress);
            }

            return image;
        }

        private async Task<MeasureAndError> EvaluateExposure(IRenderedImage image, CancellationToken token, IProgress<ApplicationStatus> progress) {
            Logger.Trace("Evaluating Exposure");

            var imageProperties = image.RawImageData.Properties;
            var imageStatistics = await image.RawImageData.Statistics.Task;

            //Very simple to directly provide result if we use statistics based contrast detection
            if (profileService.ActiveProfile.FocuserSettings.AutoFocusMethod == AFMethodEnum.CONTRASTDETECTION && profileService.ActiveProfile.FocuserSettings.ContrastDetectionMethod == ContrastDetectionMethodEnum.Statistics) {
                return new MeasureAndError() { Measure = 100 * imageStatistics.StDev / imageStatistics.Mean, Stdev = 0.01 };
            }

            System.Windows.Media.PixelFormat pixelFormat;

            if (imageProperties.IsBayered && profileService.ActiveProfile.ImageSettings.DebayerImage) {
                pixelFormat = System.Windows.Media.PixelFormats.Rgb48;
            } else {
                pixelFormat = System.Windows.Media.PixelFormats.Gray16;
            }

            var analysis = new StarDetection(image, pixelFormat, profileService.ActiveProfile.ImageSettings.StarSensitivity, profileService.ActiveProfile.ImageSettings.NoiseReduction);
            if (profileService.ActiveProfile.FocuserSettings.AutoFocusInnerCropRatio < 1 && !_setSubSample) {
                analysis.UseROI = true;
                analysis.InnerCropRatio = profileService.ActiveProfile.FocuserSettings.AutoFocusInnerCropRatio;
                analysis.OuterCropRatio = profileService.ActiveProfile.FocuserSettings.AutoFocusOuterCropRatio;
            }

            if (profileService.ActiveProfile.FocuserSettings.AutoFocusMethod == AFMethodEnum.STARHFR) {
                //Let's set the brightest star list - if it's the first exposure, it's going to be empty
                analysis.BrightestStarPositions = brightestStarPositions;
                analysis.NumberOfAFStars = profileService.ActiveProfile.FocuserSettings.AutoFocusUseBrightestStars;
                await analysis.DetectAsync(progress, token);

                //If current star list is empty, we're doing the first AF point, let's get the brightest star lists from the Star Detector instance
                if (brightestStarPositions.Count() == 0) {
                    brightestStarPositions = analysis.BrightestStarPositions;
                }

                if (profileService.ActiveProfile.ImageSettings.AnnotateImage) {
                    imagingMediator.SetImage(analysis.GetAnnotatedImage());
                }

                Logger.Debug(string.Format("Current Focus: Position: {0}, HRF: {1}", _focusPosition, analysis.AverageHFR));

                return new MeasureAndError() { Measure = analysis.AverageHFR, Stdev = analysis.HFRStdDev };
            } else {
                analysis.ContrastDetectionMethod = profileService.ActiveProfile.FocuserSettings.ContrastDetectionMethod;
                await analysis.MeasureContrastAsync(progress, token);

                if (profileService.ActiveProfile.ImageSettings.AnnotateImage) {
                    imagingMediator.SetImage(analysis.GetAnnotatedImage());
                }

                MeasureAndError ContrastMeasurement = new MeasureAndError() { Measure = analysis.AverageContrast, Stdev = analysis.ContrastStdev };
                return ContrastMeasurement;
            }
        }

        private async Task<bool> ValidateCalculatedFocusPosition(DataPoint focusPoint, FilterInfo filter, CancellationToken token, IProgress<ApplicationStatus> progress, double initialHFR) {
            _focusPosition = await focuserMediator.MoveFocuser((int)focusPoint.X);

            if (profileService.ActiveProfile.FocuserSettings.AutoFocusMethod == AFMethodEnum.STARHFR) {
                double hfr = (await GetAverageMeasurement(filter, profileService.ActiveProfile.FocuserSettings.AutoFocusNumberOfFramesPerPoint, token, progress)).Measure;

                if (hfr > (focusPoint.Y * 1.25)) {
                    Notification.ShowWarning(string.Format(Locale.Loc.Instance["LblFocusPointValidationFailed"], focusPoint.X, focusPoint.Y, hfr));
                }

                if (initialHFR != 0 && hfr > (initialHFR * 1.15)) {
                    Notification.ShowWarning(string.Format(Locale.Loc.Instance["LblAutoFocusNewWorseThanOriginal"], hfr, initialHFR));
                    Logger.Warning(string.Format("New focus point HFR {0} is significantly worse than original HFR {1}", hfr, initialHFR));
                    return false;
                }
            }
            return true;
        }

        private async Task<MeasureAndError> GetAverageMeasurement(FilterInfo filter, int exposuresPerFocusPoint, CancellationToken token, IProgress<ApplicationStatus> progress) {
            //Average HFR  of multiple exposures (if configured this way)
            double sumMeasure = 0;
            double sumVariances = 0;
            for (int i = 0; i < exposuresPerFocusPoint; i++) {
                var image = await TakeExposure(filter, token, progress);
                var partialMeasurement = await EvaluateExposure(image, token, progress);
                sumMeasure = sumMeasure + partialMeasurement.Measure;
                sumVariances = sumVariances + partialMeasurement.Stdev * partialMeasurement.Stdev;
                token.ThrowIfCancellationRequested();
            }

            return new MeasureAndError() { Measure = sumMeasure / exposuresPerFocusPoint, Stdev = Math.Sqrt(sumVariances / exposuresPerFocusPoint) };
        }

        public async Task<AutoFocusReport> StartAutoFocus(FilterInfo filter, CancellationToken token, IProgress<ApplicationStatus> progress) {
            AutoFocusReport report = null;
            Logger.Trace("Starting Autofocus");
            FocusPoints.Clear();
            PlotFocusPoints.Clear();
            TrendlineFitting = null;
            QuadraticFitting = null;
            HyperbolicFitting = null;
            GaussianFitting = null;
            FinalFocusPoint = new DataPoint(0, 0);
            int numberOfAttempts = 0;
            int initialFocusPosition = focuserInfo.Position;
            double initialHFR = 0;
            //Remember imaging filter, and get autofocus filter, if any
            FilterInfo imagingFilter = filter;
            FilterInfo defaultFocusFilter = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters.Where(f => f.AutoFocusFilter == true).FirstOrDefault();

            System.Drawing.Rectangle oldSubSample = new System.Drawing.Rectangle();

            if (profileService.ActiveProfile.FocuserSettings.AutoFocusInnerCropRatio < 1 && profileService.ActiveProfile.FocuserSettings.AutoFocusOuterCropRatio == 1 && cameraInfo.CanSubSample) {
                Logger.Debug("Setting camera subsample");
                oldSubSample = new System.Drawing.Rectangle(cameraInfo.SubSampleX, cameraInfo.SubSampleY, cameraInfo.SubSampleWidth, cameraInfo.SubSampleHeight);
                int subSampleWidth = (int)Math.Round(cameraInfo.XSize * profileService.ActiveProfile.FocuserSettings.AutoFocusInnerCropRatio);
                int subSampleHeight = (int)Math.Round(cameraInfo.YSize * profileService.ActiveProfile.FocuserSettings.AutoFocusInnerCropRatio);
                int subSampleX = (int)Math.Round((cameraInfo.XSize - subSampleWidth) / 2.0d);
                int subSampleY = (int)Math.Round((cameraInfo.YSize - subSampleHeight) / 2.0d);
                try {
                    cameraMediator.SetSubSampleArea(subSampleX, subSampleY, subSampleWidth, subSampleHeight);
                } catch (Exception e) {
                    Logger.Warning("Could not set subsample of rectangle X = " + subSampleX + ", Y = " + subSampleY + ", Width = " + subSampleWidth + ", Height = " + subSampleHeight);
                    Logger.Warning(e.Message);
                    _setSubSample = false;
                }
                _setSubSample = true;
            }

            bool tempComp = false;
            bool guidingStopped = false;

            try {
                if (focuserInfo.TempCompAvailable && focuserInfo.TempComp) {
                    tempComp = true;
                    focuserMediator.ToggleTempComp(false);
                }

                if (profileService.ActiveProfile.FocuserSettings.AutoFocusDisableGuiding) {
                    guidingStopped = await this.guiderMediator.StopGuiding(token);
                }

                initialFocusPosition = focuserInfo.Position;

                if (profileService.ActiveProfile.FocuserSettings.AutoFocusMethod == AFMethodEnum.STARHFR) {
                    //Get initial position information, as average of multiple exposures, if configured this way
                    initialHFR = (await GetAverageMeasurement(filter, profileService.ActiveProfile.FocuserSettings.AutoFocusNumberOfFramesPerPoint, token, progress)).Measure;
                }

                bool reattempt;
                do {
                    reattempt = false;
                    numberOfAttempts = numberOfAttempts + 1;

                    var offsetSteps = profileService.ActiveProfile.FocuserSettings.AutoFocusInitialOffsetSteps;
                    var offset = offsetSteps;

                    var nrOfSteps = offsetSteps + 1;

                    //Set the filter to the autofocus filter if necessary, and move to it so autofocus X indexing works properly when invoking GetFocusPoints()
                    if (defaultFocusFilter != null && profileService.ActiveProfile.FocuserSettings.UseFilterWheelOffsets) {
                        try {
                            filter = await filterWheelMediator.ChangeFilter(defaultFocusFilter, token, progress);
                        } catch (Exception e) {
                            Logger.Error(e.Message);
                            Notification.ShowWarning(e.Message);
                        }
                    }

                    await GetFocusPoints(filter, nrOfSteps, progress, token, offset);

                    var laststeps = offset;

                    int leftcount = TrendlineFitting.LeftTrend.DataPoints.Count(), rightcount = TrendlineFitting.RightTrend.DataPoints.Count();
                    //When datapoints are not sufficient analyze and take more
                    do {
                        if (leftcount == 0 && rightcount == 0) {
                            Notification.ShowWarning(Locale.Loc.Instance["LblAutoFocusNotEnoughtSpreadedPoints"]);
                            progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblAutoFocusNotEnoughtSpreadedPoints"] });
                            //Reattempting in this situation is very likely meaningless - just move back to initial focus position and call it a day
                            await focuserMediator.MoveFocuser(initialFocusPosition);
                            return null;
                        }

                        // Let's keep moving in, one step at a time, until we have enough left trend points. Then we can think about moving out to fill in the right trend points
                        if (TrendlineFitting.LeftTrend.DataPoints.Count() < offsetSteps && FocusPoints.Where(dp => dp.X < TrendlineFitting.Minimum.X && dp.Y == 0).Count() < offsetSteps) {
                            Logger.Trace("More datapoints needed to the left of the minimum");
                            //Move to the leftmost point - this should never be necessary since we're already there, but just in case
                            if (focuserInfo.Position != (int)Math.Round(FocusPoints.FirstOrDefault().X)) {
                                await focuserMediator.MoveFocuser((int)Math.Round(FocusPoints.FirstOrDefault().X));
                            }
                            //More points needed to the left
                            await GetFocusPoints(filter, 1, progress, token, -1);
                        } else if (TrendlineFitting.RightTrend.DataPoints.Count() < offsetSteps && FocusPoints.Where(dp => dp.X > TrendlineFitting.Minimum.X && dp.Y == 0).Count() < offsetSteps) { //Now we can go to the right, if necessary
                            Logger.Trace("More datapoints needed to the right of the minimum");
                            //More points needed to the right. Let's get to the rightmost point, and keep going right one point at a time
                            if (focuserInfo.Position != (int)Math.Round(FocusPoints.LastOrDefault().X)) {
                                await focuserMediator.MoveFocuser((int)Math.Round(FocusPoints.LastOrDefault().X));
                            }
                            await GetFocusPoints(filter, 1, progress, token, 1);
                        }

                        leftcount = TrendlineFitting.LeftTrend.DataPoints.Count();
                        rightcount = TrendlineFitting.RightTrend.DataPoints.Count();

                        token.ThrowIfCancellationRequested();
                    } while (rightcount + FocusPoints.Where(dp => dp.X > TrendlineFitting.Minimum.X && dp.Y == 0).Count() < offsetSteps || leftcount + FocusPoints.Where(dp => dp.X < TrendlineFitting.Minimum.X && dp.Y == 0).Count() < offsetSteps);

                    token.ThrowIfCancellationRequested();

                    FinalFocusPoint = DetermineFinalFocusPoint();

                    report = GenerateReport(initialFocusPosition, initialHFR);

                    LastAutoFocusPoint = new AutoFocusPoint { Focuspoint = FinalFocusPoint, Temperature = focuserInfo.Temperature, Timestamp = DateTime.Now };

                    //Set the filter to the autofocus filter if necessary, but do not move to it yet (otherwise filter offset will be ignored in final validation). This will be done as part of the capture in ValidateCalculatedFocusPosition
                    if (defaultFocusFilter != null && profileService.ActiveProfile.FocuserSettings.UseFilterWheelOffsets) {
                        filter = imagingFilter;
                    }

                    bool goodAutoFocus = await ValidateCalculatedFocusPosition(FinalFocusPoint, filter, token, progress, initialHFR);

                    if (!goodAutoFocus) {
                        if (numberOfAttempts < profileService.ActiveProfile.FocuserSettings.AutoFocusTotalNumberOfAttempts) {
                            Notification.ShowWarning(Locale.Loc.Instance["LblAutoFocusReattempting"]);
                            await focuserMediator.MoveFocuser(initialFocusPosition);
                            Logger.Warning("Potentially bad auto-focus. Reattempting.");
                            FocusPoints.Clear();
                            PlotFocusPoints.Clear();
                            TrendlineFitting = null;
                            QuadraticFitting = null;
                            HyperbolicFitting = null;
                            GaussianFitting = null;
                            FinalFocusPoint = new DataPoint(0, 0);
                            reattempt = true;
                        } else {
                            Notification.ShowWarning(Locale.Loc.Instance["LblAutoFocusRestoringOriginalPosition"]);
                            Logger.Warning("Potentially bad auto-focus. Restoring original focus position.");
                            reattempt = false;
                            await focuserMediator.MoveFocuser(initialFocusPosition);
                            return null;
                        }
                    }
                } while (reattempt);

                //_focusPosition = await Mediator.Instance.RequestAsync(new MoveFocuserMessage() { Position = (int)p.X, Absolute = true, Token = token });
            } catch (OperationCanceledException) {
                FocusPoints.Clear();
                PlotFocusPoints.Clear();
                //Get back to original filter, if necessary
                try {
                    await filterWheelMediator.ChangeFilter(imagingFilter);
                } catch (Exception e) {
                    Logger.Error(e.Message);
                    Notification.ShowError(e.Message);
                }
            } catch (Exception ex) {
                Notification.ShowError(ex.Message);
                Logger.Error(ex);
            } finally {
                //Restore original sub-sample rectangle, if appropriate
                if (_setSubSample && oldSubSample.X >= 0 && oldSubSample.Y >= 0 && oldSubSample.Width > 0 && oldSubSample.Height > 0) {
                    try {
                        cameraMediator.SetSubSampleArea((int)oldSubSample.X, (int)oldSubSample.Y, (int)oldSubSample.Width, (int)oldSubSample.Height);
                    } catch (Exception e) {
                        Logger.Warning("Could not set back old sub sample area");
                        Logger.Warning(e.Message);
                        Notification.ShowError(e.Message);
                    }
                }
                //Restore the temperature compensation of the focuser
                if (focuserInfo.TempCompAvailable && tempComp) {
                    focuserMediator.ToggleTempComp(true);
                }

                brightestStarPositions.Clear();
                if (guidingStopped) {
                    var startGuidingTask = this.guiderMediator.StartGuiding(token);
                    var completedTask = await Task.WhenAny(Task.Delay(60000), startGuidingTask);
                    if (startGuidingTask != completedTask) {
                        Notification.ShowWarning(Locale.Loc.Instance["LblStartGuidingFailed"]);
                    }
                }
                progress.Report(new ApplicationStatus() { Status = string.Empty });
            }
            return report;
        }

        private DataPoint DetermineFinalFocusPoint() {
            using (MyStopWatch.Measure()) {
                var method = profileService.ActiveProfile.FocuserSettings.AutoFocusMethod;

                TrendlineFitting = new TrendlineFitting().Calculate(FocusPoints);

                HyperbolicFitting = new HyperbolicFitting().Calculate(FocusPoints);

                QuadraticFitting = new QuadraticFitting().Calculate(FocusPoints);

                GaussianFitting = new GaussianFitting().Calculate(FocusPoints);

                if (method == AFMethodEnum.STARHFR) {
                    var fitting = profileService.ActiveProfile.FocuserSettings.AutoFocusCurveFitting;
                    if (fitting == Utility.Enum.AFCurveFittingEnum.TRENDLINES) {
                        return TrendlineFitting.Intersection;
                    }

                    if (fitting == Utility.Enum.AFCurveFittingEnum.HYPERBOLIC) {
                        return HyperbolicFitting.Minimum;
                    }

                    if (fitting == Utility.Enum.AFCurveFittingEnum.PARABOLIC) {
                        return QuadraticFitting.Minimum;
                    }

                    if (fitting == Utility.Enum.AFCurveFittingEnum.TRENDPARABOLIC) {
                        return new DataPoint(Math.Round((TrendlineFitting.Intersection.X + QuadraticFitting.Minimum.X) / 2), (TrendlineFitting.Intersection.Y + QuadraticFitting.Minimum.Y) / 2);
                    }

                    if (fitting == Utility.Enum.AFCurveFittingEnum.TRENDHYPERBOLIC) {
                        return new DataPoint(Math.Round((TrendlineFitting.Intersection.X + HyperbolicFitting.Minimum.X) / 2), (TrendlineFitting.Intersection.Y + HyperbolicFitting.Minimum.Y) / 2);
                    }

                    Logger.Error($"Invalid AutoFocus Fitting {fitting} for method {method}");
                    return new DataPoint();
                } else {
                    return GaussianFitting.Maximum;
                }
            }
        }

        /// <summary>
        /// Generates a JSON report into %localappdata%\NINA\AutoFocus for the complete autofocus run containing all the measurements
        /// </summary>
        /// <param name="initialFocusPosition"></param>
        /// <param name="initialHFR"></param>
        private AutoFocusReport GenerateReport(double initialFocusPosition, double initialHFR) {
            try {
                var method = profileService.ActiveProfile.FocuserSettings.AutoFocusMethod;
                var fitting = profileService.ActiveProfile.FocuserSettings.AutoFocusCurveFitting;

                var report = AutoFocusReport.GenerateReport(
                    FocusPoints,
                    initialFocusPosition,
                    initialHFR,
                    FinalFocusPoint,
                    LastAutoFocusPoint,
                    method,
                    fitting,
                    TrendlineFitting,
                    QuadraticFitting,
                    HyperbolicFitting,
                    GaussianFitting,
                    focuserInfo.Temperature
                );

                File.WriteAllText(Path.Combine(ReportDirectory, DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + ".json"), JsonConvert.SerializeObject(report));
                return report;
            } catch (Exception ex) {
                Logger.Error(ex);
                return null;
            }
        }

        private AutoFocusPoint _lastAutoFocusPoint;
        private CameraInfo cameraInfo = DeviceInfo.CreateDefaultInstance<CameraInfo>();
        private FocuserInfo focuserInfo = DeviceInfo.CreateDefaultInstance<FocuserInfo>();
        private IFocuserMediator focuserMediator;
        private IFilterWheelMediator filterWheelMediator;
        private FilterWheelInfo filterInfo;
        private bool _setSubSample = false;

        public AutoFocusPoint LastAutoFocusPoint {
            get {
                return _lastAutoFocusPoint;
            }
            set {
                _lastAutoFocusPoint = value;
                RaisePropertyChanged();
            }
        }

        private void CancelAutoFocus(object obj) {
            _autoFocusCancelToken?.Cancel();
        }

        public void UpdateDeviceInfo(CameraInfo cameraInfo) {
            this.cameraInfo = cameraInfo;
        }

        public void UpdateDeviceInfo(FocuserInfo focuserInfo) {
            this.focuserInfo = focuserInfo;
        }

        public void UpdateDeviceInfo(FilterWheelInfo deviceInfo) {
            this.filterInfo = deviceInfo;
        }

        public void Dispose() {
            this.cameraMediator?.RemoveConsumer(this);
            this.filterWheelMediator?.RemoveConsumer(this);
            this.focuserMediator?.RemoveConsumer(this);
        }

        public ICommand StartAutoFocusCommand { get; private set; }
        public ICommand CancelAutoFocusCommand { get; private set; }
    }
}