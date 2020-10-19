using System;
using System.Threading.Tasks;
using System.Windows.Input;
using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Utility;

namespace NINA.ViewModel.Imaging {

    internal interface IAnchorableSnapshotVM : IDockableVM {
        CameraInfo CameraInfo { get; set; }
        ICommand CancelSnapCommand { get; }
        bool IsLooping { get; set; }
        bool LiveViewEnabled { get; set; }
        bool Loop { get; set; }
        BinningMode SnapBin { get; set; }
        IAsyncCommand SnapCommand { get; }
        double SnapExposureDuration { get; set; }
        FilterInfo SnapFilter { get; set; }
        int SnapGain { get; set; }
        bool SnapSave { get; set; }
        bool SnapSubSample { get; set; }
        IAsyncCommand StartLiveViewCommand { get; }
        ApplicationStatus Status { get; set; }
        ICommand StopLiveViewCommand { get; }

        void Dispose();

        Task<bool> SnapImage(IProgress<ApplicationStatus> progress);

        void UpdateDeviceInfo(CameraInfo cameraStatus);
    }
}