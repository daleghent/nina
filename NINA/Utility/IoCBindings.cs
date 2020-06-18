using NINA.Model.MyPlanetarium;
using NINA.Profile;
using NINA.Utility.Astrometry;
using NINA.Utility.ImageAnalysis;
using NINA.Utility.Mediator;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel;
using NINA.ViewModel.Equipment.Camera;
using NINA.ViewModel.Equipment.Dome;
using NINA.ViewModel.Equipment.FilterWheel;
using NINA.ViewModel.Equipment.FlatDevice;
using NINA.ViewModel.Equipment.Focuser;
using NINA.ViewModel.Equipment.Guider;
using NINA.ViewModel.Equipment.Rotator;
using NINA.ViewModel.Equipment.Switch;
using NINA.ViewModel.Equipment.Telescope;
using NINA.ViewModel.Equipment.WeatherData;
using NINA.ViewModel.FlatWizard;
using NINA.ViewModel.FramingAssistant;
using NINA.ViewModel.Imaging;
using NINA.ViewModel.Interfaces;
using Ninject;
using Ninject.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NINA.Utility {

    internal class IoCBindings : NinjectModule {

        public override void Load() {
            Bind<IProfileService>().ToMethod(f => (ProfileService)Application.Current.Resources["ProfileService"]);
            Bind<IProfile>().ToMethod(f => f.Kernel.Get<ProfileService>().ActiveProfile);

            Bind<IApplicationVM>().To<ApplicationVM>().InSingletonScope();
            Bind<ICameraVM>().To<CameraVM>().InSingletonScope();
            Bind<IImagingVM>().To<ImagingVM>().InSingletonScope();
            Bind<IEquipmentVM>().To<EquipmentVM>().InSingletonScope();
            Bind<IApplicationDeviceConnectionVM>().To<ApplicationDeviceConnectionVM>().InSingletonScope();

            Bind<ISwitchVM>().To<SwitchVM>().InSingletonScope();
            Bind<IOptionsVM>().To<OptionsVM>().InSingletonScope();
            Bind<IFlatDeviceVM>().To<FlatDeviceVM>().InSingletonScope();
            Bind<IGuiderVM>().To<GuiderVM>().InSingletonScope();
            Bind<IExposureCalculatorVM>().To<ExposureCalculatorVM>().InSingletonScope();
            Bind<ISequenceVM>().To<SequenceVM>().InSingletonScope();
            Bind<IPolarAlignmentVM>().To<PolarAlignmentVM>().InSingletonScope();
            Bind<ISkyAtlasVM>().To<SkyAtlasVM>().InSingletonScope();
            Bind<IFramingAssistantVM>().To<FramingAssistantVM>().InSingletonScope();
            Bind<IFocusTargetsVM>().To<FocusTargetsVM>().InSingletonScope();
            Bind<IAutoFocusVM>().To<AutoFocusVM>().InSingletonScope();
            Bind<ITelescopeVM>().To<TelescopeVM>().InSingletonScope();
            Bind<IWeatherDataVM>().To<WeatherDataVM>().InSingletonScope();
            Bind<IDomeVM>().To<DomeVM>().InSingletonScope();
            Bind<IThumbnailVM>().To<ThumbnailVM>().InSingletonScope();
            Bind<IDockManagerVM>().To<DockManagerVM>().InSingletonScope();
            Bind<IRotatorVM>().To<RotatorVM>().InSingletonScope();
            Bind<IFilterWheelVM>().To<FilterWheelVM>().InSingletonScope();
            Bind<IApplicationStatusVM>().To<ApplicationStatusVM>().InSingletonScope();
            Bind<IFocuserVM>().To<FocuserVM>().InSingletonScope();
            Bind<IVersionCheckVM>().To<VersionCheckVM>().InSingletonScope();
            Bind<IImageControlVM>().To<ImageControlVM>().InSingletonScope();
            Bind<IImageHistoryVM>().To<ImageHistoryVM>().InSingletonScope();
            Bind<IImageStatisticsVM>().To<ImageStatisticsVM>().InSingletonScope();

            Bind<ProjectVersion>().ToMethod(f => new ProjectVersion(Utility.Version)).InSingletonScope();

            Bind<IFlatWizardVM>().ToMethod(f => {
                return new FlatWizardVM(f.Kernel.Get<IProfileService>(),
                    new ImagingVM(
                        f.Kernel.Get<IProfileService>(), new ImagingMediator(), f.Kernel.Get<ICameraMediator>(),
                        f.Kernel.Get<ITelescopeMediator>(), f.Kernel.Get<IFilterWheelMediator>(), f.Kernel.Get<IFocuserMediator>(),
                        f.Kernel.Get<IRotatorMediator>(), f.Kernel.Get<IGuiderMediator>(), f.Kernel.Get<IWeatherDataMediator>(),
                        f.Kernel.Get<IApplicationStatusMediator>(),
                        new ImageControlVM(f.Kernel.Get<IProfileService>(), f.Kernel.Get<ICameraMediator>(), f.Kernel.Get<ITelescopeMediator>(), f.Kernel.Get<IApplicationStatusMediator>()),
                        new ImageStatisticsVM(f.Kernel.Get<IProfileService>())),
                    f.Kernel.Get<ICameraMediator>(), f.Kernel.Get<IFilterWheelMediator>(), f.Kernel.Get<ITelescopeMediator>(),
                    f.Kernel.Get<IFlatDeviceMediator>(), f.Kernel.Get<IApplicationResourceDictionary>(), f.Kernel.Get<IApplicationStatusMediator>());
            }).InSingletonScope();

            Bind<IAnchorablePlateSolverVM>().To<AnchorablePlateSolverVM>().InSingletonScope();
            Bind<IAnchorableSnapshotVM>().To<AnchorableSnapshotVM>().InSingletonScope();

            Bind<ICameraMediator>().To<CameraMediator>().InSingletonScope();
            Bind<ITelescopeMediator>().To<TelescopeMediator>().InSingletonScope();
            Bind<IFocuserMediator>().To<FocuserMediator>().InSingletonScope();
            Bind<IFilterWheelMediator>().To<FilterWheelMediator>().InSingletonScope();
            Bind<IRotatorMediator>().To<RotatorMediator>().InSingletonScope();
            Bind<IFlatDeviceMediator>().To<FlatDeviceMediator>().InSingletonScope();
            Bind<IImagingMediator>().To<ImagingMediator>().InSingletonScope();
            Bind<IGuiderMediator>().To<GuiderMediator>().InSingletonScope();
            Bind<IApplicationStatusMediator>().To<ApplicationStatusMediator>().InSingletonScope();
            Bind<ISwitchMediator>().To<SwitchMediator>().InSingletonScope();
            Bind<IDomeMediator>().To<DomeMediator>().InSingletonScope();
            Bind<IWeatherDataMediator>().To<WeatherDataMediator>().InSingletonScope();

            Bind<IPlanetariumFactory>().To<PlanetariumFactory>().InSingletonScope();
            Bind<IAllDeviceConsumer>().To<AllDeviceConsumer>().InSingletonScope();

            Bind<ISharpCapSensorAnalysisReader>().To<DefaultSharpCapSensorAnalysisReader>();
            Bind<IApplicationResourceDictionary>().To<ApplicationResourceDictionary>();
            Bind<INighttimeCalculator>().To<NighttimeCalculator>();
        }
    }
}