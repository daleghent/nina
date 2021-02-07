using NINA.Model.ImageData;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel.AutoFocus;
using System.Collections.Generic;
using System.Windows.Input;

namespace NINA.ViewModel.ImageHistory {

    public interface IImageHistoryVM : IDockableVM {
        AsyncObservableCollection<ImageHistoryPoint> AutoFocusPoints { get; set; }
        List<ImageHistoryPoint> ImageHistory { get; }
        AsyncObservableCollection<ImageHistoryPoint> ObservableImageHistory { get; set; }
        ICommand PlotClearCommand { get; }

        void Add(int id, IImageStatistics statistics, string imageType);

        void AppendImageProperties(ImageSavedEventArgs imageSavedEventArgs);

        void AppendAutoFocusPoint(AutoFocusReport report);

        void PlotClear();
    }
}