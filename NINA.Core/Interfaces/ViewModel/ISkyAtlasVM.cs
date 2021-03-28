using System;
using System.Collections.Generic;
using System.Windows.Input;
using NINA.Core.Enum;
using NINA.Model;
using NINA.Utility;
using NINA.Utility.Astrometry;

namespace NINA.ViewModel.Interfaces {

    public interface ISkyAtlasVM {
        AsyncObservableCollection<DateTime> AltitudeTimesFrom { get; set; }
        AsyncObservableCollection<DateTime> AltitudeTimesThrough { get; set; }
        AsyncObservableCollection<string> BrightnessFrom { get; set; }
        AsyncObservableCollection<string> BrightnessThrough { get; set; }
        ICommand CancelSearchCommand { get; }
        AsyncObservableCollection<string> Constellations { get; set; }
        AsyncObservableCollection<KeyValuePair<double?, string>> DecFrom { get; set; }
        AsyncObservableCollection<KeyValuePair<double?, string>> DecThrough { get; set; }
        AsyncObservableCollection<string> MagnitudesFrom { get; set; }
        AsyncObservableCollection<string> MagnitudesThrough { get; set; }
        AsyncObservableCollection<KeyValuePair<double, string>> MinimumAltitudeDegrees { get; set; }
        AsyncObservableCollection<DSOObjectType> ObjectTypes { get; set; }
        SkyAtlasOrderByDirectionEnum OrderByDirection { get; set; }
        SkyAtlasOrderByFieldsEnum OrderByField { get; set; }
        int PageSize { get; set; }
        AsyncObservableCollection<KeyValuePair<double?, string>> RAFrom { get; set; }
        AsyncObservableCollection<KeyValuePair<double?, string>> RAThrough { get; set; }
        ICommand ResetFiltersCommand { get; }
        ICommand SearchCommand { get; }
        string SearchObjectName { get; set; }
        PagedList<DeepSkyObject> SearchResult { get; set; }
        DateTime SelectedAltitudeTimeFrom { get; set; }
        DateTime SelectedAltitudeTimeThrough { get; set; }
        double? SelectedBrightnessFrom { get; set; }
        double? SelectedBrightnessThrough { get; set; }
        string SelectedConstellation { get; set; }
        double? SelectedDecFrom { get; set; }
        double? SelectedDecThrough { get; set; }
        double? SelectedMagnitudeFrom { get; set; }
        double? SelectedMagnitudeThrough { get; set; }
        double SelectedMinimumAltitudeDegrees { get; set; }
        double? SelectedRAFrom { get; set; }
        double? SelectedRAThrough { get; set; }
        double? SelectedSizeFrom { get; set; }
        double? SelectedSizeThrough { get; set; }
        ICommand SetFramingAssistantCoordinatesCommand { get; }
        ICommand SetSequencerTargetCommand { get; }
        AsyncObservableCollection<KeyValuePair<string, string>> SizesFrom { get; set; }
        AsyncObservableCollection<KeyValuePair<string, string>> SizesThrough { get; set; }
        IAsyncCommand SlewToCoordinatesCommand { get; }
        NighttimeData NighttimeData { get; set; }
    }
}