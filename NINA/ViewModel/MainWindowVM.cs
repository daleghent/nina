using NINA.ViewModel.FlatWizard;
using NINA.ViewModel.FramingAssistant;
using NINA.ViewModel.ImageHistory;
using NINA.ViewModel.Interfaces;
using NINA.ViewModel.Sequencer;

namespace NINA.ViewModel {
    internal class MainWindowVM : IMainWindowVM {
        public IImagingVM ImagingVM { get; set; }
        public IApplicationVM AppVM { get; set; }
        public IEquipmentVM EquipmentVM { get; set; }
        public ISkyAtlasVM SkyAtlasVM { get; set; }
        public IFramingAssistantVM FramingAssistantVM { get; set; }
        public IFlatWizardVM FlatWizardVM { get; set; }
        public IDockManagerVM DockManagerVM { get; set; }
        public ISequenceVM SeqVM { get; set; }
        public IOptionsVM OptionsVM { get; set; }
        public IVersionCheckVM VersionCheckVM { get; set; }
        public IApplicationStatusVM ApplicationStatusVM { get; set; }
        public IApplicationDeviceConnectionVM ApplicationDeviceConnectionVM { get; set; }
        public ISequence2VM Sequence2VM { get; set; }
        public IImageSaveController ImageSaveController { get; set; }
        public IImageHistoryVM ImageHistoryVM { get; set; }
    }
}
