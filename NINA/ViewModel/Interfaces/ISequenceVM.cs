using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Model.MyFlatDevice;
using NINA.Model.MyFocuser;
using NINA.Model.MyGuider;
using NINA.Model.MyRotator;
using NINA.Model.MyTelescope;
using NINA.Model.MyWeatherData;
using NINA.Utility;
using NINA.Utility.WindowService;

namespace NINA.ViewModel.Interfaces {

    public interface ISequenceVM : IDockableVM {
        ICommand AddSequenceRowCommand { get; }
        ICommand AddTargetCommand { get; }
        ICommand CancelSequenceCommand { get; }
        ICommand CoordsFromPlanetariumCommand { get; set; }
        IDeepSkyObjectSearchVM DeepSkyObjectSearchVM { get; }
        ICommand DemoteSequenceRowCommand { get; }
        ICommand DemoteTargetCommand { get; }
        TimeSpan EstimatedDownloadTime { get; set; }
        bool HasSequenceFileName { get; }
        ObservableCollection<string> ImageTypes { get; set; }
        IImageHistoryVM ImgHistoryVM { get; }
        bool IsPaused { get; set; }
        bool IsRunning { get; set; }
        bool IsUsingSynchronizedGuider { get; set; }
        ICommand LoadSequenceCommand { get; }
        ICommand LoadTargetSetCommand { get; }
        TimeSpan OverallDuration { get; }
        DateTime OverallEndTime { get; }
        DateTime OverallStartTime { get; }
        ICommand PauseSequenceCommand { get; }
        ICommand PromoteSequenceRowCommand { get; }
        ICommand PromoteTargetCommand { get; }
        ICommand RemoveSequenceRowCommand { get; }
        ICommand RemoveTargetCommand { get; }
        ICommand ResetSequenceRowCommand { get; }
        ICommand ResetTargetCommand { get; }
        ICommand ResumeSequenceCommand { get; }
        ICommand SaveAsSequenceCommand { get; }
        ICommand SaveSequenceCommand { get; }
        ICommand SaveTargetSetCommand { get; }
        int SelectedSequenceRowIdx { get; set; }
        CaptureSequenceList Sequence { get; set; }
        TimeSpan SequenceEstimatedDuration { get; }
        DateTime SequenceEstimatedEndTime { get; }
        DateTime SequenceEstimatedStartTime { get; }
        bool SequenceModified { get; }
        bool SequenceSaveable { get; }
        IAsyncCommand StartSequenceCommand { get; }
        ApplicationStatus Status { get; set; }
        AsyncObservableCollection<CaptureSequenceList> Targets { get; set; }
        IWindowServiceFactory WindowServiceFactory { get; set; }

        void AddDownloadTime(TimeSpan t);

        void AddSequenceRow(object o);

        void Dispose();

        bool HasWritePermission(string dir);

        bool OKtoExit();

        Task<bool> SetSequenceCoordiantes(DeepSkyObject dso);

        Task<bool> SetSequenceCoordiantes(ICollection<DeepSkyObject> deepSkyObjects, bool replace = true);

        void UpdateDeviceInfo(CameraInfo cameraInfo);

        void UpdateDeviceInfo(FilterWheelInfo filterWheelInfo);

        void UpdateDeviceInfo(FlatDeviceInfo deviceInfo);

        void UpdateDeviceInfo(FocuserInfo focuserInfo);

        void UpdateDeviceInfo(GuiderInfo deviceInfo);

        void UpdateDeviceInfo(RotatorInfo deviceInfo);

        void UpdateDeviceInfo(TelescopeInfo telescopeInfo);

        void UpdateDeviceInfo(WeatherDataInfo deviceInfo);
    }
}