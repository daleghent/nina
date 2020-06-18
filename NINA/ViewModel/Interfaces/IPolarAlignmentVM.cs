using System.Threading.Tasks;
using System.Windows.Input;
using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Model.MyTelescope;
using NINA.PlateSolving;
using NINA.Utility;

namespace NINA.ViewModel.Interfaces {

    internal interface IPolarAlignmentVM : IDockableVM {
        double AltitudeDeclination { get; set; }
        double AltitudeMeridianOffset { get; set; }
        ApplicationStatus AltitudePolarErrorStatus { get; set; }
        double AzimuthDeclination { get; set; }
        double AzimuthMeridianOffset { get; set; }
        ApplicationStatus AzimuthPolarErrorStatus { get; set; }
        CameraInfo CameraInfo { get; }
        ICommand CancelDARVSlewCommand { get; }
        ICommand CancelMeasureAltitudeErrorCommand { get; }
        ICommand CancelMeasureAzimuthErrorCommand { get; }
        IAsyncCommand DARVSlewCommand { get; }
        double DARVSlewDuration { get; set; }
        double DARVSlewRate { get; set; }
        string DarvStatus { get; set; }
        string HourAngleTime { get; set; }
        IAsyncCommand MeasureAltitudeErrorCommand { get; }
        IAsyncCommand MeasureAzimuthErrorCommand { get; }
        PlateSolveResult PlateSolveResult { get; set; }
        double Rotation { get; set; }
        IAsyncCommand SlewToAltitudeMeridianOffsetCommand { get; }
        IAsyncCommand SlewToAzimuthMeridianOffsetCommand { get; }
        BinningMode SnapBin { get; set; }
        double SnapExposureDuration { get; set; }
        FilterInfo SnapFilter { get; set; }
        int SnapGain { get; set; }
        ApplicationStatus Status { get; set; }
        TelescopeInfo TelescopeInfo { get; }

        void Dispose();

        void Hide(object o);

        Task<bool> SlewToMeridianOffset(double meridianOffset, double declination);

        void UpdateDeviceInfo(CameraInfo cameraInfo);

        void UpdateDeviceInfo(TelescopeInfo telescopeInfo);
    }
}