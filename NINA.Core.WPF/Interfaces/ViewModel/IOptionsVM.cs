#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using NINA.Core.Enum;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using NINA.Core.Model.Equipment;
using NINA.Core.Model;
using NINA.Profile;

namespace NINA.WPF.Base.Interfaces.ViewModel {

    public interface IOptionsVM {
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
        ICommand OpenPS2FileDiagCommand { get; }
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