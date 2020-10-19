#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model.MyCamera;
using NINA.Utility;
using NINA.Profile;
using NINA.Model.ImageData;
using System.Windows.Input;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using NINA.ViewModel.Interfaces;
using NINA.Utility.Mediator.Interfaces;
using System;

namespace NINA.ViewModel.ImageHistory {

    public class ImageHistoryVM : DockableVM, IImageHistoryVM {

        public ImageHistoryVM(IProfileService profileService, IImageSaveMediator imageSaveMediator) : base(profileService) {
            Title = "LblHFRHistory";

            if (System.Windows.Application.Current != null) {
                ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["HFRHistorySVG"];
            }

            this.imageSaveMediator = imageSaveMediator;
            this.imageSaveMediator.ImageSaved += ImageSaveMediator_ImageSaved;

            _nextStatHistoryId = 0;
            ObservableImageHistory = new AsyncObservableCollection<ImageHistoryPoint>();
            AutoFocusPoints = new AsyncObservableCollection<ImageHistoryPoint>();

            PlotClearCommand = new RelayCommand((object o) => PlotClear());
        }

        private void ImageSaveMediator_ImageSaved(object sender, ImageSavedEventArgs e) {
            this.AppendStarDetection(e.StarDetectionAnalysis);
        }

        private IImageSaveMediator imageSaveMediator;
        private int _nextStatHistoryId;
        private AsyncObservableCollection<ImageHistoryPoint> _limitedImageHistoryStack;

        public AsyncObservableCollection<ImageHistoryPoint> ObservableImageHistory {
            get {
                return _limitedImageHistoryStack;
            }
            set {
                _limitedImageHistoryStack = value;
                RaisePropertyChanged();
            }
        }

        private AsyncObservableCollection<ImageHistoryPoint> autoFocusPoints;

        public AsyncObservableCollection<ImageHistoryPoint> AutoFocusPoints {
            get {
                return autoFocusPoints;
            }
            set {
                autoFocusPoints = value;
                RaisePropertyChanged();
            }
        }

        public List<ImageHistoryPoint> ImageHistory { get; private set; } = new List<ImageHistoryPoint>();

        private object lockObj = new object();

        public void Add(IImageStatistics statistics) {
            lock (lockObj) {
                var point = new ImageHistoryPoint(Interlocked.Increment(ref _nextStatHistoryId), statistics);
                ImageHistory.Add(point);
            }
        }

        public void AppendStarDetection(IStarDetectionAnalysis starDetectionAnalysis) {
            if (starDetectionAnalysis != null) {
                lock (lockObj) {
                    var last = ImageHistory.LastOrDefault();
                    if (last != null) {
                        last.PopulateSDPoint(starDetectionAnalysis);
                        ObservableImageHistory.Add(last);
                    }
                }
            }
        }

        public void AppendAutoFocusPoint(AutoFocus.AutoFocusReport report) {
            if (report != null) {
                lock (lockObj) {
                    var last = ImageHistory.LastOrDefault();
                    if (last != null) {
                        last.PopulateAFPoint(report);
                        AutoFocusPoints.Add(last);
                    }
                }
            }
        }

        public void PlotClear() {
            this.ObservableImageHistory.Clear();
            this.AutoFocusPoints.Clear();
        }

        public ICommand PlotClearCommand { get; private set; }
    }
}