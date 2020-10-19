using NINA.Model.ImageData;
using NINA.Utility;
using NINA.ViewModel.AutoFocus;
using System.Collections.Generic;
using System.Windows.Input;

namespace NINA.ViewModel.ImageHistory {

    public interface IImageHistoryVM : IDockableVM {
        AsyncObservableCollection<ImageHistoryPoint> AutoFocusPoints { get; set; }
        List<ImageHistoryPoint> ImageHistory { get; }
        AsyncObservableCollection<ImageHistoryPoint> ObservableImageHistory { get; set; }
        ICommand PlotClearCommand { get; }

        void Add(IImageStatistics statistics);

        void AppendStarDetection(IStarDetectionAnalysis starDetectionAnalysis);

        void AppendAutoFocusPoint(AutoFocusReport report);

        void PlotClear();
    }
}