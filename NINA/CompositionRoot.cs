using NINA.Profile;
using NINA.Utility;
using NINA.ViewModel;
using NINA.ViewModel.FlatWizard;
using NINA.ViewModel.FramingAssistant;
using NINA.ViewModel.ImageHistory;
using NINA.ViewModel.Interfaces;
using NINA.ViewModel.Sequencer;
using Ninject;
using System;

namespace NINA {
    internal static class CompositionRoot {

        public static IMainWindowVM Compose(IProfileService profileService) {
            try {
                IReadOnlyKernel _kernel = 
                    new KernelConfiguration(
                        new IoCBindings(profileService))
                    .BuildReadonlyKernel();

                return new MainWindowVM {
                    AppVM = _kernel.Get<IApplicationVM>(),
                    ImageSaveController = _kernel.Get<IImageSaveController>(),
                    ImagingVM = _kernel.Get<IImagingVM>(),
                    EquipmentVM = _kernel.Get<IEquipmentVM>(),
                    SkyAtlasVM = _kernel.Get<ISkyAtlasVM>(),
                    SeqVM = _kernel.Get<ISequenceVM>(),
                    FramingAssistantVM = _kernel.Get<IFramingAssistantVM>(),
                    FlatWizardVM = _kernel.Get<IFlatWizardVM>(),
                    DockManagerVM = _kernel.Get<IDockManagerVM>(),

                    OptionsVM = _kernel.Get<IOptionsVM>(),
                    ApplicationDeviceConnectionVM = _kernel.Get<IApplicationDeviceConnectionVM>(),
                    VersionCheckVM = _kernel.Get<IVersionCheckVM>(),
                    ApplicationStatusVM = _kernel.Get<IApplicationStatusVM>(),

                    Sequence2VM = _kernel.Get<ISequence2VM>(),
                    ImageHistoryVM = _kernel.Get<IImageHistoryVM>()
                };
            } catch (Exception ex) {
                Logger.Error(ex);
                throw ex;
            }
        }
        
    }
}