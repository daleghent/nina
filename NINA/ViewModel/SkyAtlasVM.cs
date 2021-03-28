#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Mediator.Interfaces;
using NINA.Profile;
using OxyPlot;
using OxyPlot.Axes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using NINA.ViewModel.Interfaces;
using NINA.ViewModel.FramingAssistant;
using NINA.ViewModel.Sequencer;
using NINA.Sequencer.Container;
using NINA.Core.Database;
using NINA.Core.Enum;

namespace NINA.ViewModel {

    internal partial class SkyAtlasVM : BaseVM, ISkyAtlasVM {

        public SkyAtlasVM(IProfileService profileService, ITelescopeMediator telescopeMediator,
            IFramingAssistantVM framingAssistantVM, ISequenceMediator sequenceMediator, INighttimeCalculator nighttimeCalculator, IApplicationMediator applicationMediator) : base(profileService) {
            this.nighttimeCalculator = nighttimeCalculator;

            SearchCommand = new AsyncCommand<bool>(() => Search());
            CancelSearchCommand = new RelayCommand(CancelSearch);
            ResetFiltersCommand = new RelayCommand(ResetFilters);
            GetDSOTemplatesCommand = new RelayCommand((object o) => {
                DSOTemplates = sequenceMediator.GetDeepSkyObjectContainerTemplates();
                RaisePropertyChanged(nameof(DSOTemplates));
            });
            SetOldSequencerTargetCommand = new RelayCommand((object o) => {
                applicationMediator.ChangeTab(ApplicationTab.SEQUENCE);

                sequenceMediator.AddSimpleTarget(SearchResult.SelectedItem);
            });
            SetSequencerTargetCommand = new RelayCommand((object o) => {
                applicationMediator.ChangeTab(ApplicationTab.SEQUENCE);

                var template = o as IDeepSkyObjectContainer;

                var container = (IDeepSkyObjectContainer)template.Clone();
                container.Name = SearchResult.SelectedItem.Name;
                container.Target.TargetName = SearchResult.SelectedItem.Name;
                container.Target.Rotation = 0;
                container.Target.InputCoordinates.Coordinates = SearchResult.SelectedItem.Coordinates;

                sequenceMediator.AddAdvancedTarget(container);
            });
            SlewToCoordinatesCommand = new AsyncCommand<bool>(async () => {
                return await telescopeMediator.SlewToCoordinatesAsync(SearchResult.SelectedItem.Coordinates, CancellationToken.None);
            });
            SetFramingAssistantCoordinatesCommand = new AsyncCommand<bool>(async () => {
                applicationMediator.ChangeTab(ApplicationTab.FRAMINGASSISTANT);
                return await framingAssistantVM.SetCoordinates(SearchResult.SelectedItem);
            });

            Task.Run(() => { ResetFilters(null); InitializeFilters(); });
            PageSize = 50;

            profileService.LocationChanged += (object sender, EventArgs e) => {
                InitializeElevationFilters();
            };

            profileService.LocaleChanged += (object sender, EventArgs e) => {
                InitializeFilters();
            };
        }

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

        public IList<IDeepSkyObjectContainer> DSOTemplates { get; private set; }

        private DateTime filterDate;

        public DateTime FilterDate {
            get => filterDate;
            set {
                if (filterDate != value) {
                    filterDate = value;
                    RaisePropertyChanged();
                }
            }
        }

        private void ResetFilters(object obj) {
            FilterDate = DateTime.Now;

            SearchObjectName = string.Empty;

            foreach (var objecttype in ObjectTypes) {
                objecttype.Selected = false;
            }

            SelectedAltitudeTimeFrom = DateTime.MinValue;
            SelectedAltitudeTimeThrough = DateTime.MaxValue;
            SelectedMinimumAltitudeDegrees = 0;

            SelectedBrightnessFrom = null;
            SelectedBrightnessThrough = null;

            SelectedConstellation = string.Empty;

            SelectedDecFrom = null;
            SelectedDecThrough = null;

            SelectedMagnitudeFrom = null;
            SelectedMagnitudeThrough = null;

            SelectedMinimumAltitudeDegrees = 0;

            SelectedRAFrom = null;
            SelectedRAThrough = null;

            SelectedSizeFrom = null;
            SelectedSizeThrough = null;

            OrderByField = SkyAtlasOrderByFieldsEnum.SIZEMAX;
            OrderByDirection = SkyAtlasOrderByDirectionEnum.DESC;
        }

        private CancellationTokenSource _searchTokenSource;

        private void CancelSearch(object obj) {
            _searchTokenSource?.Cancel();
        }

        private readonly INighttimeCalculator nighttimeCalculator;

        private async Task<bool> Search() {
            _searchTokenSource?.Dispose();
            _searchTokenSource = new CancellationTokenSource();
            return await Task.Run(async () => {
                try {
                    SearchResult = null;
                    var db = new DatabaseInteraction();
                    var types = ObjectTypes.Where((x) => x.Selected).Select((x) => x.Name).ToList();

                    var searchParams = new DatabaseInteraction.DeepSkyObjectSearchParams();
                    searchParams.Constellation = SelectedConstellation;
                    searchParams.DsoTypes = types;
                    searchParams.ObjectName = SearchObjectName;
                    searchParams.RightAscension.From = SelectedRAFrom;
                    searchParams.RightAscension.Thru = SelectedRAThrough;
                    searchParams.Declination.From = Nullable.Compare(SelectedDecFrom, SelectedDecThrough) > 0 ? SelectedDecThrough : SelectedDecFrom;
                    searchParams.Declination.Thru = Nullable.Compare(SelectedDecFrom, SelectedDecThrough) > 0 ? SelectedDecFrom : SelectedDecThrough;
                    searchParams.Brightness.From = SelectedBrightnessFrom;
                    searchParams.Brightness.Thru = SelectedBrightnessThrough;
                    searchParams.Magnitude.From = SelectedMagnitudeFrom;
                    searchParams.Magnitude.Thru = SelectedMagnitudeThrough;
                    searchParams.Size.From = SelectedSizeFrom;
                    searchParams.Size.Thru = SelectedSizeThrough;
                    searchParams.SearchOrder.Field = OrderByField.ToString().ToLower();
                    searchParams.SearchOrder.Direction = OrderByDirection.ToString();

                    var result = await db.GetDeepSkyObjects(
                        profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository,
                        profileService.ActiveProfile.AstrometrySettings.Horizon,
                        searchParams,
                        _searchTokenSource.Token
                    );

                    var longitude = profileService.ActiveProfile.AstrometrySettings.Longitude;
                    var latitude = profileService.ActiveProfile.AstrometrySettings.Latitude;

                    NighttimeData = this.nighttimeCalculator.Calculate(FilterDate);
                    DateTime d = NighttimeData.ReferenceDate;

                    Parallel.ForEach(result, (obj) => {
                        var cloneDate = d;
                        obj.SetDateAndPosition(cloneDate, latitude, longitude);
                        _searchTokenSource.Token.ThrowIfCancellationRequested();
                    });

                    /* Check if Altitude Filter is not default */
                    if (!(SelectedAltitudeTimeFrom == DateTime.MinValue && SelectedAltitudeTimeThrough == DateTime.MaxValue && SelectedMinimumAltitudeDegrees == 0)) {
                        var filteredList = result.Where((x) => {
                            return x.Altitudes.Where((y) => {
                                return (y.X > DateTimeAxis.ToDouble(SelectedAltitudeTimeFrom) && y.X < DateTimeAxis.ToDouble(SelectedAltitudeTimeThrough));
                            }).All((z) => {
                                return z.Y > SelectedMinimumAltitudeDegrees;
                            });
                        });

                        var count = filteredList.Count();
                        /* Apply Altitude Filter */
                        SearchResult = new PagedList<DeepSkyObject>(PageSize, filteredList);
                    } else {
                        SearchResult = new PagedList<DeepSkyObject>(PageSize, result);
                    }
                } catch (OperationCanceledException) {
                }
                return true;
            });
        }

        private void InitializeFilters() {
            NighttimeData = this.nighttimeCalculator.Calculate();
            InitializeRADecFilters();
            InitializeConstellationFilters();
            InitializeObjectTypeFilters();
            InitializeMagnitudeFilters();
            InitializeBrightnessFilters();
            InitializeSizeFilters();
            InitializeElevationFilters();
        }

        private void InitializeElevationFilters() {
            AltitudeTimesFrom = new AsyncObservableCollection<DateTime>();
            AltitudeTimesThrough = new AsyncObservableCollection<DateTime>();
            MinimumAltitudeDegrees = new AsyncObservableCollection<KeyValuePair<double, string>>();

            var d = NighttimeData.ReferenceDate;

            AltitudeTimesFrom.Add(DateTime.MinValue);
            AltitudeTimesThrough.Add(DateTime.MaxValue);

            for (int i = 0; i <= 24; i++) {
                AltitudeTimesFrom.Add(d);
                AltitudeTimesThrough.Add(d);
                d = d.AddHours(1);
            }

            for (int i = 0; i <= 90; i += 10) {
                MinimumAltitudeDegrees.Add(new KeyValuePair<double, string>(i, i + "°"));
            }
        }

        private void InitializeSizeFilters() {
            SizesFrom = new AsyncObservableCollection<KeyValuePair<string, string>>();
            SizesThrough = new AsyncObservableCollection<KeyValuePair<string, string>>();

            SizesFrom.Add(new KeyValuePair<string, string>(string.Empty, string.Empty));
            SizesThrough.Add(new KeyValuePair<string, string>(string.Empty, string.Empty));

            SizesFrom.Add(new KeyValuePair<string, string>("1", "1 " + Locale.Loc.Instance["LblArcsec"]));
            SizesFrom.Add(new KeyValuePair<string, string>("5", "5 " + Locale.Loc.Instance["LblArcsec"]));
            SizesFrom.Add(new KeyValuePair<string, string>("10", "10 " + Locale.Loc.Instance["LblArcsec"]));
            SizesFrom.Add(new KeyValuePair<string, string>("30", "30 " + Locale.Loc.Instance["LblArcsec"]));
            SizesFrom.Add(new KeyValuePair<string, string>("60", "1 " + Locale.Loc.Instance["LblArcmin"]));
            SizesFrom.Add(new KeyValuePair<string, string>("300", "5 " + Locale.Loc.Instance["LblArcmin"]));
            SizesFrom.Add(new KeyValuePair<string, string>("600", "10 " + Locale.Loc.Instance["LblArcmin"]));
            SizesFrom.Add(new KeyValuePair<string, string>("1800", "30 " + Locale.Loc.Instance["LblArcmin"]));
            SizesFrom.Add(new KeyValuePair<string, string>("3600", "1 " + Locale.Loc.Instance["LblDegree"]));
            SizesFrom.Add(new KeyValuePair<string, string>("18000", "5 " + Locale.Loc.Instance["LblDegree"]));
            SizesFrom.Add(new KeyValuePair<string, string>("36000", "10 " + Locale.Loc.Instance["LblDegree"]));

            SizesThrough = new AsyncObservableCollection<KeyValuePair<string, string>>(SizesFrom);
        }

        private void InitializeBrightnessFilters() {
            BrightnessFrom = new AsyncObservableCollection<string>();
            BrightnessThrough = new AsyncObservableCollection<string>();

            BrightnessFrom.Add(string.Empty);
            BrightnessThrough.Add(string.Empty);

            for (var i = 2; i < 19; i++) {
                BrightnessFrom.Add(i.ToString());
                BrightnessThrough.Add(i.ToString());
            }
        }

        private void InitializeConstellationFilters() {
            var dbResult = new DatabaseInteraction().GetConstellations(new System.Threading.CancellationToken());
            var list = new AsyncObservableCollection<string>(dbResult.Result);
            list.Insert(0, string.Empty);
            Constellations = list;
        }

        private void InitializeObjectTypeFilters() {
            var task = new DatabaseInteraction().GetObjectTypes(new System.Threading.CancellationToken());
            var list = task.Result?.OrderBy(x => x).ToList();
            foreach (var type in list) {
                ObjectTypes.Add(new DSOObjectType(type));
            }
        }

        private void InitializeMagnitudeFilters() {
            MagnitudesFrom = new AsyncObservableCollection<string>();
            MagnitudesThrough = new AsyncObservableCollection<string>();

            MagnitudesFrom.Add(string.Empty);
            MagnitudesThrough.Add(string.Empty);

            for (var i = 1; i < 22; i++) {
                MagnitudesFrom.Add(i.ToString());
                MagnitudesThrough.Add(i.ToString());
            }
        }

        private void InitializeRADecFilters() {
            RAFrom = new AsyncObservableCollection<KeyValuePair<double?, string>>();
            RAThrough = new AsyncObservableCollection<KeyValuePair<double?, string>>();
            DecFrom = new AsyncObservableCollection<KeyValuePair<double?, string>>();
            DecThrough = new AsyncObservableCollection<KeyValuePair<double?, string>>();

            RAFrom.Add(new KeyValuePair<double?, string>(null, string.Empty));
            RAThrough.Add(new KeyValuePair<double?, string>(null, string.Empty));
            DecFrom.Add(new KeyValuePair<double?, string>(null, string.Empty));
            DecThrough.Add(new KeyValuePair<double?, string>(null, string.Empty));

            for (int i = 0; i < 25; i++) {
                Astrometry.HoursToDegrees(i);

                RAFrom.Add(new KeyValuePair<double?, string>(Astrometry.HoursToDegrees(i), i.ToString()));
                RAThrough.Add(new KeyValuePair<double?, string>(Astrometry.HoursToDegrees(i), i.ToString()));
            }
            for (int i = -90; i < 91; i = i + 5) {
                DecFrom.Add(new KeyValuePair<double?, string>(i, i.ToString()));
                DecThrough.Add(new KeyValuePair<double?, string>(i, i.ToString()));
            }
        }

        private string _searchObjectName;
        private AsyncObservableCollection<DSOObjectType> _objectTypes;
        private AsyncObservableCollection<string> _constellations;
        private string _selectedConstellation;
        private AsyncObservableCollection<KeyValuePair<double?, string>> _rAFrom;
        private AsyncObservableCollection<KeyValuePair<double?, string>> _rAThrough;
        private AsyncObservableCollection<KeyValuePair<double?, string>> _decFrom;
        private AsyncObservableCollection<KeyValuePair<double?, string>> _decThrough;
        private double? _selectedRAFrom;
        private double? _selectedRAThrough;
        private double? _selectedDecFrom;
        private double? _selectedDecThrough;
        private AsyncObservableCollection<string> _brightnessFrom;
        private AsyncObservableCollection<string> _brightnessThrough;
        private double? _selectedBrightnessFrom;
        private double? _selectedBrightnessThrough;
        private AsyncObservableCollection<KeyValuePair<string, string>> _sizesFrom;
        private AsyncObservableCollection<KeyValuePair<string, string>> _sizesThrough;
        private double? _selectedSizeFrom;
        private double? _selectedSizeThrough;
        private AsyncObservableCollection<string> _magnitudesFrom;
        private AsyncObservableCollection<string> _magnitudesThrough;
        private double? _selectedMagnitudeFrom;
        private double? _selectedMagnitudeThrough;
        private PagedList<DeepSkyObject> _searchResult;
        private AsyncObservableCollection<DateTime> _altitudeTimesFrom;
        private AsyncObservableCollection<DateTime> _altitudeTimesThrough;
        private AsyncObservableCollection<KeyValuePair<double, string>> _minimumAltitudeDegrees;
        private DateTime _selectedAltitudeTimeFrom;
        private DateTime _selectedAltitudeTimeThrough;
        private double _selectedMinimumAltitudeDegrees;

        public AsyncObservableCollection<DateTime> AltitudeTimesFrom {
            get {
                return _altitudeTimesFrom;
            }
            set {
                _altitudeTimesFrom = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<DateTime> AltitudeTimesThrough {
            get {
                return _altitudeTimesThrough;
            }
            set {
                _altitudeTimesThrough = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<KeyValuePair<double, string>> MinimumAltitudeDegrees {
            get {
                return _minimumAltitudeDegrees;
            }
            set {
                _minimumAltitudeDegrees = value;
                RaisePropertyChanged();
            }
        }

        public DateTime SelectedAltitudeTimeFrom {
            get {
                return _selectedAltitudeTimeFrom;
            }
            set {
                _selectedAltitudeTimeFrom = value;
                RaisePropertyChanged();
            }
        }

        public DateTime SelectedAltitudeTimeThrough {
            get {
                return _selectedAltitudeTimeThrough;
            }
            set {
                _selectedAltitudeTimeThrough = value;
                RaisePropertyChanged();
            }
        }

        public double SelectedMinimumAltitudeDegrees {
            get {
                return _selectedMinimumAltitudeDegrees;
            }
            set {
                _selectedMinimumAltitudeDegrees = value;
                RaisePropertyChanged();
            }
        }

        public string SearchObjectName {
            get {
                return _searchObjectName;
            }

            set {
                _searchObjectName = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<DSOObjectType> ObjectTypes {
            get {
                if (_objectTypes == null) {
                    _objectTypes = new AsyncObservableCollection<DSOObjectType>();
                }
                return _objectTypes;
            }

            set {
                _objectTypes = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<string> Constellations {
            get {
                return _constellations;
            }

            set {
                _constellations = value;
                RaisePropertyChanged();
            }
        }

        public string SelectedConstellation {
            get {
                return _selectedConstellation;
            }

            set {
                _selectedConstellation = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<KeyValuePair<double?, string>> RAFrom {
            get {
                return _rAFrom;
            }

            set {
                _rAFrom = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<KeyValuePair<double?, string>> RAThrough {
            get {
                return _rAThrough;
            }

            set {
                _rAThrough = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<KeyValuePair<double?, string>> DecFrom {
            get {
                return _decFrom;
            }

            set {
                _decFrom = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<KeyValuePair<double?, string>> DecThrough {
            get {
                return _decThrough;
            }

            set {
                _decThrough = value;
                RaisePropertyChanged();
            }
        }

        public double? SelectedRAFrom {
            get {
                return _selectedRAFrom;
            }

            set {
                _selectedRAFrom = value;
                RaisePropertyChanged();
            }
        }

        public double? SelectedRAThrough {
            get {
                return _selectedRAThrough;
            }

            set {
                _selectedRAThrough = value;
                RaisePropertyChanged();
            }
        }

        public double? SelectedDecFrom {
            get {
                return _selectedDecFrom;
            }

            set {
                _selectedDecFrom = value;
                RaisePropertyChanged();
            }
        }

        public double? SelectedDecThrough {
            get {
                return _selectedDecThrough;
            }

            set {
                _selectedDecThrough = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<string> BrightnessFrom {
            get {
                return _brightnessFrom;
            }

            set {
                _brightnessFrom = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<string> BrightnessThrough {
            get {
                return _brightnessThrough;
            }

            set {
                _brightnessThrough = value;
                RaisePropertyChanged();
            }
        }

        public double? SelectedBrightnessFrom {
            get {
                return _selectedBrightnessFrom;
            }

            set {
                _selectedBrightnessFrom = value;
                RaisePropertyChanged();
            }
        }

        public double? SelectedBrightnessThrough {
            get {
                return _selectedBrightnessThrough;
            }

            set {
                _selectedBrightnessThrough = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<KeyValuePair<string, string>> SizesFrom {
            get {
                return _sizesFrom;
            }

            set {
                _sizesFrom = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<KeyValuePair<string, string>> SizesThrough {
            get {
                return _sizesThrough;
            }

            set {
                _sizesThrough = value;
                RaisePropertyChanged();
            }
        }

        public double? SelectedSizeFrom {
            get {
                return _selectedSizeFrom;
            }

            set {
                _selectedSizeFrom = value;
                RaisePropertyChanged();
            }
        }

        public double? SelectedSizeThrough {
            get {
                return _selectedSizeThrough;
            }

            set {
                _selectedSizeThrough = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<string> MagnitudesFrom {
            get {
                return _magnitudesFrom;
            }

            set {
                _magnitudesFrom = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<string> MagnitudesThrough {
            get {
                return _magnitudesThrough;
            }

            set {
                _magnitudesThrough = value;
                RaisePropertyChanged();
            }
        }

        public double? SelectedMagnitudeFrom {
            get {
                return _selectedMagnitudeFrom;
            }

            set {
                _selectedMagnitudeFrom = value;
                RaisePropertyChanged();
            }
        }

        public double? SelectedMagnitudeThrough {
            get {
                return _selectedMagnitudeThrough;
            }

            set {
                _selectedMagnitudeThrough = value;
                RaisePropertyChanged();
            }
        }

        public ICommand ResetFiltersCommand { get; private set; }
        public ICommand SearchCommand { get; private set; }

        public ICommand CancelSearchCommand { get; private set; }

        public ICommand SetOldSequencerTargetCommand { get; private set; }
        public ICommand SetSequencerTargetCommand { get; private set; }

        public IAsyncCommand SlewToCoordinatesCommand { get; private set; }

        public ICommand SetFramingAssistantCoordinatesCommand { get; private set; }
        public ICommand GetDSOTemplatesCommand { get; private set; }

        private int _pageSize;

        public int PageSize {
            get {
                return _pageSize;
            }
            set {
                _pageSize = value;
                RaisePropertyChanged();
            }
        }

        private SkyAtlasOrderByDirectionEnum _orderByDirection;

        public SkyAtlasOrderByDirectionEnum OrderByDirection {
            get {
                return _orderByDirection;
            }
            set {
                _orderByDirection = value;
                RaisePropertyChanged();
            }
        }

        private SkyAtlasOrderByFieldsEnum _orderByField;

        public SkyAtlasOrderByFieldsEnum OrderByField {
            get {
                return _orderByField;
            }
            set {
                _orderByField = value;
                RaisePropertyChanged();
            }
        }

        public PagedList<DeepSkyObject> SearchResult {
            get {
                return _searchResult;
            }

            set {
                _searchResult = value;
                RaisePropertyChanged();
            }
        }
    }
}