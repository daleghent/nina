using NINA.Core.Interfaces;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Interfaces;
using NINA.Profile.Interfaces;
using NINA.Utility;
using NINA.ViewModel;
using NINA.ViewModel.FlatWizard;
using NINA.ViewModel.FramingAssistant;
using NINA.ViewModel.ImageHistory;
using NINA.ViewModel.Interfaces;
using NINA.ViewModel.Sequencer;
using NINA.WPF.Base.Interfaces.ViewModel;
using Ninject;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace NINA {

    internal static class CompositionRoot {

        public static IMainWindowVM Compose(IProfileService profileService) {
            try {
                IReadOnlyKernel _kernel =
                new KernelConfiguration(
                    new IoCBindings(profileService))
                .BuildReadonlyKernel();

                Stopwatch sw;

                sw = Stopwatch.StartNew();
                var appvm = _kernel.Get<IApplicationVM>();
                Debug.Print($"Time to create IApplicationVM {sw.Elapsed}");

                sw = Stopwatch.StartNew();
                try {
                    EDSDKLib.EDSDKLocal.Initialize();
                } catch (Exception ex) {
                    Logger.Error(ex);
                }
                Debug.Print($"Time to initialize EDSDK {sw.Elapsed}");

                sw = Stopwatch.StartNew();
                var imageSaveController = _kernel.Get<IImageSaveController>();
                Debug.Print($"Time to create IImageSaveController {sw.Elapsed}");

                sw = Stopwatch.StartNew();
                var imagingVM = _kernel.Get<IImagingVM>();
                Debug.Print($"Time to create IImagingVM {sw.Elapsed}");

                sw = Stopwatch.StartNew();
                var equipmentVM = _kernel.Get<IEquipmentVM>();
                Debug.Print($"Time to create IEquipmentVM {sw.Elapsed}");

                sw = Stopwatch.StartNew();
                var skyAtlasVM = _kernel.Get<ISkyAtlasVM>();
                Debug.Print($"Time to create ISkyAtlasVM {sw.Elapsed}");

                sw = Stopwatch.StartNew();
                var sequenceNavigationVM = _kernel.Get<ISequenceNavigationVM>();
                Debug.Print($"Time to create ISequenceNavigationVM {sw.Elapsed}");

                sw = Stopwatch.StartNew();
                var framingAssistantVM = _kernel.Get<IFramingAssistantVM>();
                Debug.Print($"Time to create IFramingAssistantVM {sw.Elapsed}");

                sw = Stopwatch.StartNew();
                var flatWizardVM = _kernel.Get<IFlatWizardVM>();
                Debug.Print($"Time to create IFlatWizardVM {sw.Elapsed}");

                sw = Stopwatch.StartNew();
                var dockManagerVM = _kernel.Get<IDockManagerVM>();
                Debug.Print($"Time to create IDockManagerVM {sw.Elapsed}");

                sw = Stopwatch.StartNew();
                var optionsVM = _kernel.Get<IOptionsVM>();
                Debug.Print($"Time to create IOptionsVM {sw.Elapsed}");

                sw = Stopwatch.StartNew();
                var applicationDeviceConnectionVM = _kernel.Get<IApplicationDeviceConnectionVM>();
                Debug.Print($"Time to create IApplicationDeviceConnectionVM {sw.Elapsed}");

                sw = Stopwatch.StartNew();
                var versionCheckVM = _kernel.Get<IVersionCheckVM>();
                Debug.Print($"Time to create IVersionCheckVM {sw.Elapsed}");

                sw = Stopwatch.StartNew();
                var applicationStatusVM = _kernel.Get<IApplicationStatusVM>();
                Debug.Print($"Time to create IApplicationStatusVM {sw.Elapsed}");

                sw = Stopwatch.StartNew();
                var imageHistoryVM = _kernel.Get<IImageHistoryVM>();
                Debug.Print($"Time to create IImageHistoryVM {sw.Elapsed}");

                sw = Stopwatch.StartNew();
                var pluginsVM = _kernel.Get<IPluginsVM>();
                Debug.Print($"Time to create IPluginsVM {sw.Elapsed}");

                sw = Stopwatch.StartNew();
                var globalObjects = _kernel.Get<GlobalObjects>();
                Debug.Print($"Time to create GlobalObjects {sw.Elapsed}");

                sw = Stopwatch.StartNew();
                var mainWindowVM = new MainWindowVM {
                    AppVM = appvm,
                    ImageSaveController = imageSaveController,
                    ImagingVM = imagingVM,
                    EquipmentVM = equipmentVM,
                    SkyAtlasVM = skyAtlasVM,
                    SequenceNavigationVM = sequenceNavigationVM,
                    FramingAssistantVM = framingAssistantVM,
                    FlatWizardVM = flatWizardVM,
                    DockManagerVM = dockManagerVM,

                    OptionsVM = optionsVM,
                    ApplicationDeviceConnectionVM = applicationDeviceConnectionVM,
                    VersionCheckVM = versionCheckVM,
                    ApplicationStatusVM = applicationStatusVM,

                    ImageHistoryVM = imageHistoryVM,
                    PluginsVM = pluginsVM,
                    GlobalObjects = globalObjects
                };
                Debug.Print($"Time to create MainWindowVM {sw.Elapsed}");

                return mainWindowVM;
            } catch (Exception ex) {
                Logger.Error(ex);
                throw;
            }
        }
    }
}