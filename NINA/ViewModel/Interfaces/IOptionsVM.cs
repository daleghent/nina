using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using NINA.Model;
using NINA.Model.MyFilterWheel;
using NINA.Profile;
using NINA.Utility;
using NINA.Utility.Enum;

namespace NINA.ViewModel.Interfaces {

    internal interface IOptionsVM {
        IProfile ActiveProfile { get; }
        ICommand AddFilterCommand { get; }
        ICommand AddProfileCommand { get; }
        AutoUpdateSourceEnum AutoUpdateSource { get; set; }
        ObservableCollection<CultureInfo> AvailableLanguages { get; set; }
        ICommand CloneProfileCommand { get; }
        ICommand CopyToAlternativeCustomSchemaCommand { get; }
        ICommand CopyToCustomSchemaCommand { get; }
        ICommand DownloadIndexesCommand { get; }
        FileTypeEnum[] FileTypes { get; }
        ImagePatterns ImagePatterns { get; set; }
        ICommand ImportFiltersCommand { get; }
        ObservableCollection<string> IndexFiles { get; set; }
        CultureInfo Language { get; set; }
        double Latitude { get; set; }
        LogLevelEnum LogLevel { get; set; }
        double Longitude { get; set; }
        ICommand OpenASPSFileDiagCommand { get; }
        ICommand OpenASTAPFileDiagCommand { get; }
        ICommand OpenCygwinFileDiagCommand { get; }
        ICommand OpenImageFileDiagCommand { get; }
        ICommand OpenLogFolderCommand { get; }
        RelayCommand OpenPHD2DiagCommand { get; set; }
        ICommand OpenPS2FileDiagCommand { get; }
        ICommand OpenSequenceCommandAtCompletionDiagCommand { get; }
        ICommand OpenSequenceFolderDiagCommand { get; }
        ICommand OpenSequenceTemplateDiagCommand { get; }
        ICommand OpenSharpCapSensorAnalysisFolderDiagCommand { get; }
        ICommand OpenSkyAtlasImageRepositoryDiagCommand { get; }
        ICommand OpenSkySurveyCacheDirectoryDiagCommand { get; }
        ICommand OpenWebRequestCommand { get; }
        AsyncObservableCollection<ProfileMeta> Profiles { get; }
        ICommand RemoveFilterCommand { get; }
        ICommand RemoveProfileCommand { get; }
        FilterInfo SelectedFilter { get; set; }
        ProfileMeta SelectedProfile { get; set; }
        ICommand SelectProfileCommand { get; }
        ICommand SensorAnalysisFolderChangedCommand { get; }
        ICommand SetAutoFocusFilterCommand { get; }
        ICommand SiteFromGPSCommand { get; }
        ICommand SiteFromPlanetariumCommand { get; }
        TIFFCompressionTypeEnum[] TIFFCompressionTypes { get; }
        ICommand ToggleColorsCommand { get; }
        XISFChecksumTypeEnum[] XISFChecksumTypes { get; }
        XISFCompressionTypeEnum[] XISFCompressionTypes { get; }
    }
}