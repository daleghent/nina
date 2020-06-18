using NINA.Model.ImageData;
using NINA.Utility;
using NINA.ViewModel.AutoFocus;
using System.Collections.Generic;
using System.Windows.Input;

namespace NINA.ViewModel.Interfaces {

    public interface IImageHistoryVM : IDockableVM {
        AsyncObservableCollection<ImageHistoryVM.ImageHistoryPoint> AutoFocusPoints { get; set; }
        List<ImageHistoryVM.ImageHistoryPoint> ImageHistory { get; }
        AsyncObservableLimitedSizedStack<ImageHistoryVM.ImageHistoryPoint> LimitedImageHistoryStack { get; set; }
        ICommand PlotClearCommand { get; }

        void Add(IStarDetectionAnalysis starDetectionAnalysis);

        void AppendAutoFocusPoint(AutoFocusReport report);

        void PlotClear();
    }
}