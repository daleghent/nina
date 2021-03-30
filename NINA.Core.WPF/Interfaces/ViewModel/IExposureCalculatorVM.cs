#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Windows.Input;
using NINA.Model.ImageData;
using NINA.Model.MyFilterWheel;
using NINA.Utility;
using NINA.Utility.ImageAnalysis;

namespace NINA.ViewModel.Imaging {

    public interface IExposureCalculatorVM : IDockableVM {
        double BiasMedian { get; set; }
        ICommand CancelDetermineBiasCommand { get; }
        ICommand CancelDetermineExposureTimeCommand { get; }
        IAsyncCommand DetermineBiasCommand { get; }
        IAsyncCommand DetermineExposureTimeCommand { get; }
        double FullWellCapacity { get; set; }
        bool IsSharpCapSensorAnalysisEnabled { get; set; }
        string MySharpCapSensor { get; set; }
        double ReadNoise { get; set; }
        double RecommendedExposureTime { get; set; }
        ICommand ReloadSensorAnalysisCommand { get; }
        ObservableCollection<string> SharpCapSensorNames { get; set; }
        double SnapExposureDuration { get; set; }
        FilterInfo SnapFilter { get; set; }
        int SnapGain { get; set; }
        AllImageStatistics Statistics { get; set; }

        ImmutableDictionary<string, SharpCapSensorAnalysisData> LoadSensorAnalysisData(string path);
    }
}