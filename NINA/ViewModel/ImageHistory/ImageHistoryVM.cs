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
using NINA.Utility.Enum;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Forms;
using System.IO;
using CsvHelper;
using System.Globalization;

namespace NINA.ViewModel.ImageHistory {

    public class ImageHistoryVM : DockableVM, IImageHistoryVM {

        public ImageHistoryVM(IProfileService profileService, IImageSaveMediator imageSaveMediator) : base(profileService) {
            Title = "LblHFRHistory";

            if (System.Windows.Application.Current != null) {
                ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["HFRHistorySVG"];
            }

            this.imageSaveMediator = imageSaveMediator;
            this.imageSaveMediator.ImageSaved += ImageSaveMediator_ImageSaved;

            ObservableImageHistory = new AsyncObservableCollection<ImageHistoryPoint>();
            ObservableImageHistoryView = new AsyncObservableCollection<ImageHistoryPoint>();
            AutoFocusPoints = new AsyncObservableCollection<ImageHistoryPoint>();
            AutoFocusPointsView = new AsyncObservableCollection<ImageHistoryPoint>();

            ImageHistoryLeftSelected = ImageHistoryEnum.HFR;
            ImageHistoryRightSelected = ImageHistoryEnum.Stars;

            FilterList = new AsyncObservableCollection<string>();
            AllFilters = Locale.Loc.Instance["LblHFRHistoryAllFilters"];
            FilterList.Add(AllFilters);
            SelectedFilter = AllFilters;

            PlotClearCommand = new RelayCommand((object o) => PlotClear());
            PlotSaveCommand = new RelayCommand((object o) => PlotSave());
        }

        private string AllFilters { get; set; }

        private void ImageSaveMediator_ImageSaved(object sender, ImageSavedEventArgs e) {
            this.AppendImageProperties(e);
        }

        private IImageSaveMediator imageSaveMediator;
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

        private AsyncObservableCollection<ImageHistoryPoint> _observableImageHistoryView;

        public AsyncObservableCollection<ImageHistoryPoint> ObservableImageHistoryView {
            get { return this._observableImageHistoryView; }
            private set {
                if (value == this._observableImageHistoryView) {
                    return;
                }

                this._observableImageHistoryView = value;
                RaisePropertyChanged();
            }
        }

        private int index { get; set; }

        public void FilterImageHistoryList() {
            // Clear the view and recreate it based on selected filters
            ObservableImageHistoryView.Clear();
            index = 1;
            foreach (ImageHistoryPoint imageHistoryPoint in ObservableImageHistory) {
                if ((this.SelectedFilter.Equals(AllFilters) || imageHistoryPoint.Filter.Equals(this.SelectedFilter)) && (ShowSnapshots || imageHistoryPoint.Type == "LIGHT")) {
                    imageHistoryPoint.Index = index++;
                    ObservableImageHistoryView.Add(imageHistoryPoint);
                }
            }
            // Now check if AutoFocus points need to be filtered.
            FilterAutoFocusPoints();
        }

        public void FilterAutoFocusPoints() {
            AutoFocusPointsView.Clear();
            // Check if the autofocuspoint is not filtered
            foreach (ImageHistoryPoint imageHistoryPoint in AutoFocusPoints) {
                var imageHistoryItem = ObservableImageHistoryView.FirstOrDefault(item => item.Id == imageHistoryPoint.Id);
                if (imageHistoryItem != null) {
                    imageHistoryPoint.Index = imageHistoryItem.Index;
                    AutoFocusPointsView.Add(imageHistoryPoint);
                }
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

        private AsyncObservableCollection<ImageHistoryPoint> autoFocusPointsView;

        public AsyncObservableCollection<ImageHistoryPoint> AutoFocusPointsView {
            get {
                return autoFocusPointsView;
            }
            set {
                autoFocusPointsView = value;
                RaisePropertyChanged();
            }
        }

        private AsyncObservableCollection<string> _filterList;

        public AsyncObservableCollection<string> FilterList {
            get {
                return _filterList;
            }
            set {
                _filterList = value;
                RaisePropertyChanged();
            }
        }

        private string _selectedFilter;

        public string SelectedFilter {
            get {
                return _selectedFilter;
            }
            set {
                _selectedFilter = value;
                FilterImageHistoryList();
                RaisePropertyChanged();
            }
        }

        private ImageHistoryEnum _imageHistoryLeftSelected;

        public ImageHistoryEnum ImageHistoryLeftSelected {
            get {
                return _imageHistoryLeftSelected;
            }
            set {
                _imageHistoryLeftSelected = value;
                RaisePropertyChanged();
            }
        }

        private ImageHistoryEnum _imageHistoryRightSelected;

        public ImageHistoryEnum ImageHistoryRightSelected {
            get {
                return _imageHistoryRightSelected;
            }
            set {
                _imageHistoryRightSelected = value;
                RaisePropertyChanged();
            }
        }

        private bool showSnapshots = false;

        public bool ShowSnapshots {
            get => showSnapshots;
            set {
                showSnapshots = value;
                FilterImageHistoryList();
                RaisePropertyChanged();
            }
        }

        public List<ImageHistoryPoint> ImageHistory { get; private set; } = new List<ImageHistoryPoint>();

        private object lockObj = new object();

        public void Add(int id, IImageStatistics statistics, string imageType) {
            lock (lockObj) {
                var point = new ImageHistoryPoint(id, statistics, imageType);
                ImageHistory.Add(point);
            }
        }

        public void AppendImageProperties(ImageSavedEventArgs imageSavedEventArgs) {
            if (imageSavedEventArgs != null) {
                lock (lockObj) {
                    var imageHistoryItem = ImageHistory.FirstOrDefault(item => item.Id == imageSavedEventArgs.MetaData.Image.Id);
                    if (imageHistoryItem != null) {
                        imageHistoryItem.PopulateProperties(imageSavedEventArgs);
                        ObservableImageHistory.Add(imageHistoryItem);
                        // Check if the filter needs to be added to the list
                        if (!FilterList.Contains(imageSavedEventArgs.Filter))
                            FilterList.Add(imageSavedEventArgs.Filter);
                        // Add to view if it's not filtered
                        if ((this.SelectedFilter.Equals(AllFilters) || imageHistoryItem.Filter.Equals(this.SelectedFilter)) && (ShowSnapshots || imageHistoryItem.Type == "LIGHT")) {
                            imageHistoryItem.Index = index++;
                            ObservableImageHistoryView.Add(imageHistoryItem);
                        }
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
                        if ((this.SelectedFilter.Equals(AllFilters) || last.Filter.Equals(this.SelectedFilter)) && (ShowSnapshots || last.Type == "LIGHT")) {
                            AutoFocusPointsView.Add(last);
                        }
                    }
                }
            }
        }

        public void PlotClear() {
            this.ObservableImageHistory.Clear();
            this.AutoFocusPoints.Clear();
            this.ObservableImageHistoryView.Clear();
        }

        public void PlotSave() {
            if (this.ObservableImageHistory.Count != 0) {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.FileName = "history.csv";
                if (sfd.ShowDialog() == DialogResult.OK) {
                    using (var writer = new StreamWriter(sfd.FileName))
                    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture)) {
                        csv.Configuration.RegisterClassMap<ImageHistoryPointMap>();
                        csv.WriteRecords(ObservableImageHistory);
                    }
                }
            }
        }

        public ICommand PlotClearCommand { get; private set; }
        public ICommand PlotSaveCommand { get; private set; }
    }
}