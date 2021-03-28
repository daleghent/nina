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