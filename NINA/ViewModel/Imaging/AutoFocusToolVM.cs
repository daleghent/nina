#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Core.Enum;
using NINA.Core.Interfaces;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Image.ImageAnalysis;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.Utility.AutoFocus;
using NINA.WPF.Base.ViewModel;
using NINA.WPF.Base.ViewModel.AutoFocus;
using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static NINA.WPF.Base.ViewModel.Imaging.AutoFocusToolVM;

namespace NINA.Imaging.ViewModel.Imaging {

    public class AutoFocusToolVM : DockableVM, ICameraConsumer, IFocuserConsumer, IAutoFocusToolVM {
        private CancellationTokenSource _autoFocusCancelToken;
        private AsyncObservableCollection<WPF.Base.ViewModel.Imaging.AutoFocusToolVM.Chart> _chartList;
        private bool _chartListSelectable;
        private WPF.Base.ViewModel.Imaging.AutoFocusToolVM.Chart _selectedChart;
        private ApplicationStatus _status;
        private readonly IApplicationStatusMediator applicationStatusMediator;
        private readonly IPluggableBehaviorSelector<IAutoFocusVMFactory> autoFocusVMFactorySelector;
        private CameraInfo cameraInfo;
        private readonly ICameraMediator cameraMediator;
        private readonly IFilterWheelMediator filterWheelMediator;
        private FocuserInfo focuserInfo;
        private readonly IFocuserMediator focuserMediator;
        private FileSystemWatcher reportFileWatcher;

        public AutoFocusToolVM(
                IProfileService profileService,
                ICameraMediator cameraMediator,
                IFilterWheelMediator filterWheelMediator,
                IFocuserMediator focuserMediator,
                IApplicationStatusMediator applicationStatusMediator,
                IPluggableBehaviorSelector<IAutoFocusVMFactory> autoFocusVMFactorySelector
        ) : base(profileService) {
            Title = Loc.Instance["LblAutoFocus"];
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current?.Resources["AutoFocusSVG"];

            this.cameraMediator = cameraMediator;
            this.cameraMediator.RegisterConsumer(this);

            this.filterWheelMediator = filterWheelMediator;

            this.focuserMediator = focuserMediator;
            this.focuserMediator.RegisterConsumer(this);

            this.applicationStatusMediator = applicationStatusMediator;

            this.autoFocusVMFactorySelector = autoFocusVMFactorySelector;
            // NOTE: pluggable behavior manager has not been initialized yet, so this will always return the built-in AF factory,
            // until the first AF run happens. LoadChart depends on this to not be null though. We should move this out to a separate object.
            // I think you said this already
            this.AutoFocusVM = autoFocusVMFactorySelector.SelectedBehavior.Create();

            ChartList = new AsyncObservableCollection<WPF.Base.ViewModel.Imaging.AutoFocusToolVM.Chart>();
            ChartListSelectable = true;

            reportFileWatcher = new FileSystemWatcher() {
                Path = WPF.Base.ViewModel.AutoFocus.AutoFocusVM.ReportDirectory,
                NotifyFilter = NotifyFilters.FileName,
                Filter = "*.json",
                EnableRaisingEvents = true,
                IncludeSubdirectories = false
            };
            reportFileWatcher.Created += ReportFileWatcher_Created;
            reportFileWatcher.Deleted += ReportFileWatcher_Deleted;
            Task.Run(() => {
                InitializeChartList();
            });

            StartAutoFocusCommand = new AsyncCommand<AutoFocusReport>(
                () =>
                    Task.Run(
                        async () => {
                            cameraMediator.RegisterCaptureBlock(this);
                            ChartListSelectable = false;
                            try {
                                var vm = autoFocusVMFactorySelector.SelectedBehavior.Create();
                                if (vm.GetType() != AutoFocusVM?.GetType()) {
                                    AutoFocusVM = vm;
                                    RaisePropertyChanged(nameof(AutoFocusVM));
                                }
                                return await AutoFocusVM.StartAutoFocus(CommandInitialization(), _autoFocusCancelToken.Token, new Progress<ApplicationStatus>(p => Status = p));
                            } finally {
                                cameraMediator.ReleaseCaptureBlock(this);
                                ChartListSelectable = true;
                            }
                        }
                    ),
                (p) => { return focuserInfo?.Connected == true && cameraInfo?.Connected == true && cameraMediator.IsFreeToCapture(this); }
            );
            CancelAutoFocusCommand = new RelayCommand(CancelAutoFocus);
            SelectionChangedCommand = new AsyncCommand<bool>(LoadChart);
        }

        public IAutoFocusVM AutoFocusVM { get; private set; }

        public ICommand CancelAutoFocusCommand { get; private set; }

        public AsyncObservableCollection<WPF.Base.ViewModel.Imaging.AutoFocusToolVM.Chart> ChartList {
            get {
                return _chartList;
            }
            set {
                _chartList = value;
                RaisePropertyChanged();
            }
        }

        public bool ChartListSelectable {
            get {
                return _chartListSelectable;
            }
            set {
                _chartListSelectable = value;
                RaisePropertyChanged();
            }
        }

        public new string ContentId {
            get {
                //Backwards compatibility for avalondock layouts prior to 1.11
                return "AutoFocusVM";
            }
        }

        public WPF.Base.ViewModel.Imaging.AutoFocusToolVM.Chart SelectedChart {
            get => _selectedChart;
            set {
                _selectedChart = value;
                RaisePropertyChanged();
            }
        }

        public IAsyncCommand SelectionChangedCommand { get; private set; }

        public IAsyncCommand StartAutoFocusCommand { get; private set; }

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

        private void CancelAutoFocus(object obj) {
            _autoFocusCancelToken?.Cancel();
        }

        private FilterInfo CommandInitialization() {
            _autoFocusCancelToken?.Dispose();
            _autoFocusCancelToken = new CancellationTokenSource();
            var filterInfo = filterWheelMediator.GetInfo();
            FilterInfo filter = null;
            if (filterInfo?.SelectedFilter != null) {
                filter = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters.Where(x => x.Position == filterInfo.SelectedFilter.Position).FirstOrDefault();
            }
            return filter;
        }

        private object lockobj = new object();

        private void InitializeChartList() {
            var files = Directory.GetFiles(Path.Combine(WPF.Base.ViewModel.AutoFocus.AutoFocusVM.ReportDirectory));
            var l = new SortedSet<WPF.Base.ViewModel.Imaging.AutoFocusToolVM.Chart>(new WPF.Base.ViewModel.Imaging.AutoFocusToolVM.ChartComparer());

            foreach (string file in files) {
                var item = new WPF.Base.ViewModel.Imaging.AutoFocusToolVM.Chart(Path.GetFileName(file), file);
                l.Add(item);
            }
            lock (lockobj) {
                ChartList = new AsyncObservableCollection<WPF.Base.ViewModel.Imaging.AutoFocusToolVM.Chart>(l);
                SelectedChart = ChartList.FirstOrDefault();
            }
            _ = LoadChart();
        }

        private void ReportFileWatcher_Created(object sender, FileSystemEventArgs e) {
            Logger.Debug($"New AutoFocus chart created at {e.FullPath}");
            var item = new WPF.Base.ViewModel.Imaging.AutoFocusToolVM.Chart(Path.GetFileName(e.FullPath), e.FullPath);

            lock (lockobj) {
                ChartList.Insert(0, item);
                SelectedChart = item;
            }

            _ = LoadChart();
        }

        private void ReportFileWatcher_Deleted(object sender, FileSystemEventArgs e) {
            lock (lockobj) {
                var toRemove = ChartList.FirstOrDefault(x => x.FilePath == e.FullPath);
                if (toRemove != null) {
                    Logger.Debug($"New AutoFocus chart deleted from {e.FullPath}");
                    ChartList.Remove(toRemove);
                    if (SelectedChart == null || object.ReferenceEquals(SelectedChart, toRemove)) {
                        SelectedChart = ChartList.FirstOrDefault();
                        _ = LoadChart();
                    }
                }
            }
        }

        public void Dispose() {
            this.cameraMediator.RemoveConsumer(this);
            this.focuserMediator.RemoveConsumer(this);
        }

        public async Task<bool> LoadChart() {
            if (SelectedChart != null && ChartListSelectable) {
                try {
                    var comparer = new FocusPointComparer();
                    var plotComparer = new PlotPointComparer();
                    AutoFocusVM.FocusPoints.Clear();
                    AutoFocusVM.PlotFocusPoints.Clear();

                    using (var reader = File.OpenText(SelectedChart.FilePath)) {
                        var text = await reader.ReadToEndAsync();
                        var report = JsonConvert.DeserializeObject<AutoFocusReport>(text);

                        if (Enum.TryParse<AFCurveFittingEnum>(report.Fitting, out var afCurveFittingEnum)) {
                            AutoFocusVM.FinalFocusPoint = new DataPoint(report.CalculatedFocusPoint.Position, report.CalculatedFocusPoint.Value);
                            AutoFocusVM.LastAutoFocusPoint = new ReportAutoFocusPoint { Focuspoint = AutoFocusVM.FinalFocusPoint, Temperature = report.Temperature, Timestamp = report.Timestamp, Filter = report.Filter };

                            var focusPoints = new AsyncObservableCollection<ScatterErrorPoint>();
                            var plotFocusPoints = new AsyncObservableCollection<DataPoint>();
                            foreach (FocusPoint fp in report.MeasurePoints) {
                                focusPoints.AddSorted(new ScatterErrorPoint(Convert.ToInt32(fp.Position), fp.Value, 0, fp.Error), comparer);
                                plotFocusPoints.AddSorted(new DataPoint(Convert.ToInt32(fp.Position), fp.Value), plotComparer);
                            }

                            AutoFocusVM.FocusPoints = focusPoints;
                            AutoFocusVM.PlotFocusPoints = plotFocusPoints;

                            AutoFocusVM.AutoFocusChartMethod = report.Method == AFMethodEnum.STARHFR.ToString() ? AFMethodEnum.STARHFR : AFMethodEnum.CONTRASTDETECTION;
                            AutoFocusVM.AutoFocusChartCurveFitting = afCurveFittingEnum;
                            AutoFocusVM.SetCurveFittings(report.Method, report.Fitting);
                            AutoFocusVM.AutoFocusDuration = report.Duration;
                        }

                        Logger.Debug("Finished loading latest AutoFocus chart");
                        return true;
                    }
                } catch (Exception ex) {
                    Logger.Error("Failed to load autofocus chart", ex);
                }
            }
            return false;
        }

        public void UpdateDeviceInfo(FocuserInfo deviceInfo) {
            this.focuserInfo = deviceInfo;
        }

        public void UpdateDeviceInfo(CameraInfo deviceInfo) {
            this.cameraInfo = deviceInfo;
        }

        public void UpdateEndAutoFocusRun(AutoFocusInfo info) {
            ;
        }

        public void UpdateUserFocused(FocuserInfo info) {
            ;
        }
    }
}