using System.Collections.ObjectModel;
using System.Windows.Input;
using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Model.MyTelescope;
using NINA.PlateSolving;
using NINA.Profile;
using NINA.Utility;
using NINA.ViewModel.Equipment.Camera;

namespace NINA.ViewModel.Imaging {

    internal interface IAnchorablePlateSolverVM : IDockableVM {
        IProfile ActiveProfile { get; }
        CameraInfo CameraInfo { get; }
        ICommand CancelSolveCommand { get; }
        string ContentId { get; }
        PlateSolveResult PlateSolveResult { get; set; }
        ObservableCollection<PlateSolveResult> PlateSolveResultList { get; set; }
        double RepeatThreshold { get; set; }
        bool SlewToTarget { get; set; }
        BinningMode SnapBin { get; set; }
        double SnapExposureDuration { get; set; }
        FilterInfo SnapFilter { get; set; }
        int SnapGain { get; set; }
        IAsyncCommand SolveCommand { get; }
        ApplicationStatus Status { get; set; }
        bool Sync { get; set; }
        TelescopeInfo TelescopeInfo { get; }

        void Dispose();

        void UpdateDeviceInfo(CameraInfo cameraInfo);

        void UpdateDeviceInfo(TelescopeInfo telescopeInfo);
    }
}