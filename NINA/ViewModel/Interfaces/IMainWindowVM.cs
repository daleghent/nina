using NINA.ViewModel.FlatWizard;
using NINA.ViewModel.FramingAssistant;
using NINA.ViewModel.ImageHistory;
using NINA.ViewModel.Sequencer;

namespace NINA.ViewModel.Interfaces {

    internal interface IMainWindowVM {
        IImagingVM ImagingVM { get; }
        IApplicationVM AppVM { get; }
        IEquipmentVM EquipmentVM { get; }
        ISkyAtlasVM SkyAtlasVM { get; }
        IFramingAssistantVM FramingAssistantVM { get; }
        IFlatWizardVM FlatWizardVM { get; }
        IDockManagerVM DockManagerVM { get; }
        ISequenceNavigationVM SequenceNavigationVM { get; }
        IOptionsVM OptionsVM { get; }
        IVersionCheckVM VersionCheckVM { get; }
        IApplicationStatusVM ApplicationStatusVM { get; }
        IApplicationDeviceConnectionVM ApplicationDeviceConnectionVM { get; }
        IImageSaveController ImageSaveController { get; }
        IImageHistoryVM ImageHistoryVM { get; }
    }
}