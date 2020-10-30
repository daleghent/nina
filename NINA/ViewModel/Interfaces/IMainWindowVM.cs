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
        ISequenceVM SeqVM { get; }
        IOptionsVM OptionsVM { get; }
        IVersionCheckVM VersionCheckVM { get; }
        IApplicationStatusVM ApplicationStatusVM { get; }
        IApplicationDeviceConnectionVM ApplicationDeviceConnectionVM { get; }
        ISequence2VM Sequence2VM { get; }
        IImageSaveController ImageSaveController { get; }
        IImageHistoryVM ImageHistoryVM { get; }
    }
}
