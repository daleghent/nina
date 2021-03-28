using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Utility.Astrometry;
using NINA.ViewModel.Sequencer.SimpleSequence;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.Sequencer.Container {

    public interface ISimpleDSOContainer {
        ISimpleExposure ActiveExposure { get; set; }
        ICommand AddSimpleExposureCommand { get; }
        bool AutoFocusAfterHFRChange { get; set; }
        double AutoFocusAfterHFRChangeAmount { get; set; }
        bool AutoFocusAfterSetExposures { get; set; }
        bool AutoFocusAfterSetTime { get; set; }
        bool AutoFocusAfterTemperatureChange { get; set; }
        double AutoFocusAfterTemperatureChangeAmount { get; set; }
        bool AutoFocusOnFilterChange { get; set; }
        bool AutoFocusOnStart { get; set; }
        int AutoFocusSetExposures { get; set; }
        double AutoFocusSetTime { get; set; }
        CameraInfo CameraInfo { get; }
        bool CenterTarget { get; set; }
        ICommand CoordsFromPlanetariumCommand { get; }
        ICommand CoordsToFramingCommand { get; }
        int Delay { get; set; }
        ICommand DemoteSimpleExposureCommand { get; }
        TimeSpan EstimatedDuration { get; set; }
        DateTime EstimatedEndTime { get; set; }
        DateTime EstimatedStartTime { get; set; }
        string FileName { get; set; }
        bool MeridianFlipEnabled { get; set; }
        SequenceMode Mode { get; set; }
        NighttimeData NighttimeData { get; }
        ICommand PromoteSimpleExposureCommand { get; }
        ICommand RemoveSimpleExposureCommand { get; }
        ICommand ResetSimpleExposureCommand { get; }
        int RotateIterations { get; set; }
        bool RotateTarget { get; set; }
        ISimpleExposure SelectedSimpleExposure { get; set; }
        bool SlewToTarget { get; set; }
        bool StartGuiding { get; set; }
        InputTarget Target { get; set; }

        ISimpleExposure AddSimpleExposure();

        TimeSpan CalculateEstimatedRuntime();

        object Clone();

        Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token);

        void MoveDown();

        void MoveUp();

        IDeepSkyObjectContainer TransformToDSOContainer();

        bool Validate();
    }
}