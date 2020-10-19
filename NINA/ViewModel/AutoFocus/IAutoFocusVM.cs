using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Model.MyFocuser;
using NINA.Utility;
using NINA.ViewModel.AutoFocus;
using OxyPlot;
using OxyPlot.Series;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel {

    public interface IAutoFocusVM : IDockableVM, IDisposable {
        double AverageContrast { get; }
        ICommand CancelAutoFocusCommand { get; }
        double ContrastStdev { get; }
        DataPoint FinalFocusPoint { get; set; }
        AsyncObservableCollection<ScatterErrorPoint> FocusPoints { get; set; }
        GaussianFitting GaussianFitting { get; set; }
        HyperbolicFitting HyperbolicFitting { get; set; }
        AutoFocusPoint LastAutoFocusPoint { get; set; }
        AsyncObservableCollection<DataPoint> PlotFocusPoints { get; set; }
        QuadraticFitting QuadraticFitting { get; set; }
        ICommand StartAutoFocusCommand { get; }
        ApplicationStatus Status { get; set; }
        TrendlineFitting TrendlineFitting { get; set; }
        Boolean ChartListSelectable { get; }
        ICommand LoadChartCommand { get; }

        void Dispose();

        Task<AutoFocusReport> StartAutoFocus(FilterInfo filter, CancellationToken token, IProgress<ApplicationStatus> progress);

        void UpdateDeviceInfo(CameraInfo cameraInfo);

        void UpdateDeviceInfo(FilterWheelInfo deviceInfo);

        void UpdateDeviceInfo(FocuserInfo focuserInfo);
    }
}