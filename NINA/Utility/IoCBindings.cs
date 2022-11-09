#region "copyright"
/*
    Copyright © 2016 - 2022 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using NINA.API.SGP;
using NINA.Equipment.Equipment.MyPlanetarium;
using NINA.Profile.Interfaces;
using NINA.Astrometry;
using NINA.ViewModel;
using NINA.ViewModel.FlatWizard;
using NINA.ViewModel.FramingAssistant;
using NINA.ViewModel.ImageHistory;
using NINA.ViewModel.Imaging;
using NINA.ViewModel.Sequencer;
using Ninject;
using Ninject.Modules;
using System;
using NINA.Plugin;
using NINA.Core.Utility;
using NINA.Core.Model;
using NINA.Equipment.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Core.Interfaces;
using NINA.WPF.Base.Mediator;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.Core.Utility.WindowService;
using NINA.Image.ImageAnalysis;
using NINA.WPF.Base.Interfaces.Utility;
using NINA.Equipment.Interfaces;
using NINA.WPF.Base.Utility;
using NINA.Sequencer.Mediator;
using NINA.Equipment.Equipment.MyDome;
using NINA.Core.Interfaces.API.SGP;
using NINA.Core.MyMessageBox;
using NINA.Profile;
using NINA.Astrometry.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.WPF.Base.ViewModel.Equipment.FlatDevice;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.ViewModel.Equipment.Switch;
using NINA.WPF.Base.ViewModel.Equipment.Camera;
using NINA.WPF.Base.ViewModel.Equipment.WeatherData;
using NINA.WPF.Base.ViewModel.Equipment.Telescope;
using NINA.WPF.Base.ViewModel.Equipment.Dome;
using NINA.WPF.Base.ViewModel.Equipment.FilterWheel;
using NINA.WPF.Base.ViewModel.Equipment.SafetyMonitor;
using NINA.WPF.Base.ViewModel.Equipment.Focuser;
using NINA.WPF.Base.ViewModel.Equipment.Guider;
using NINA.WPF.Base.ViewModel.Equipment.Rotator;
using NINA.Plugin.Interfaces;
using NINA.Interfaces;
using NINA.ViewModel.Plugins;
using NINA.PlateSolving.Interfaces;
using NINA.PlateSolving;
using NINA.Equipment.SDK.CameraSDKs.SBIGSDK;
using NINA.Core.Interfaces.Utility;
using NINA.Image.Interfaces;
using NINA.Image.ImageData;
using NINA.WPF.Base.ViewModel;
using NINA.WPF.Base.Interfaces;
using NINA.Equipment.Equipment;
using NINA.Imaging.ViewModel.Imaging;

namespace NINA.Utility {

    internal class IoCBindings : NinjectModule {
        private readonly IProfileService _profileService;

        public IoCBindings(IProfileService profileService) =>
            _profileService = profileService;

        private void BindPluggable<InterfaceT, DefaultT>()
            where DefaultT : InterfaceT
            where InterfaceT : class, IPluggableBehavior {
            Bind<IPluggableBehaviorSelector, IPluggableBehaviorSelector<InterfaceT>>().To<PluggableBehaviorSelector<InterfaceT, DefaultT>>().InSingletonScope();
        }

        public override void Load() {
            using (MyStopWatch.Measure()) {
                try {
                    Bind<IProfileService>().ToMethod(f => _profileService);
                    Bind<IProfile>().ToMethod(f => f.Kernel.Get<ProfileService>().ActiveProfile);

                    Bind<IDeviceDispatcher>().To<DeviceDispatcher>().InSingletonScope();
                    Bind<IApplicationVM>().To<ApplicationVM>().InSingletonScope();

                    Bind<IEquipmentProviders, IEquipmentProviders<ICamera>>().To<PluginEquipmentProviders<ICamera>>().InSingletonScope();
                    Bind<IEquipmentProviders, IEquipmentProviders<IFilterWheel>>().To<PluginEquipmentProviders<IFilterWheel>>().InSingletonScope();
                    Bind<IEquipmentProviders, IEquipmentProviders<IFocuser>>().To<PluginEquipmentProviders<IFocuser>>().InSingletonScope();
                    Bind<IEquipmentProviders, IEquipmentProviders<IRotator>>().To<PluginEquipmentProviders<IRotator>>().InSingletonScope();
                    Bind<IEquipmentProviders, IEquipmentProviders<ITelescope>>().To<PluginEquipmentProviders<ITelescope>>().InSingletonScope();
                    Bind<IEquipmentProviders, IEquipmentProviders<IGuider>>().To<PluginEquipmentProviders<IGuider>>().InSingletonScope();
                    Bind<IEquipmentProviders, IEquipmentProviders<ISwitchHub>>().To<PluginEquipmentProviders<ISwitchHub>>().InSingletonScope();
                    Bind<IEquipmentProviders, IEquipmentProviders<IFlatDevice>>().To<PluginEquipmentProviders<IFlatDevice>>().InSingletonScope();
                    Bind<IEquipmentProviders, IEquipmentProviders<IWeatherData>>().To<PluginEquipmentProviders<IWeatherData>>().InSingletonScope();
                    Bind<IEquipmentProviders, IEquipmentProviders<IDome>>().To<PluginEquipmentProviders<IDome>>().InSingletonScope();
                    Bind<IEquipmentProviders, IEquipmentProviders<ISafetyMonitor>>().To<PluginEquipmentProviders<ISafetyMonitor>>().InSingletonScope();
                    Bind<IPluginEquipmentProviderManager>().To<PluginEquipmentProviderManager>().InSingletonScope();

                    Bind<IDeviceChooserVM>().To<CameraChooserVM>().WhenInjectedExactlyInto<CameraVM>().InSingletonScope();
                    Bind<IDeviceChooserVM>().To<CameraChooserVM>().WhenInjectedExactlyInto<SGPServiceBackend>().InSingletonScope();
                    Bind<IDeviceChooserVM>().To<FilterWheelChooserVM>().WhenInjectedExactlyInto<FilterWheelVM>().InSingletonScope();
                    Bind<IDeviceChooserVM>().To<FocuserChooserVM>().WhenInjectedInto<IFocuserVM>().InSingletonScope();
                    Bind<IDeviceChooserVM>().To<RotatorChooserVM>().WhenInjectedExactlyInto<RotatorVM>().InSingletonScope();                    
                    Bind<IDeviceChooserVM>().To<TelescopeChooserVM>().WhenInjectedExactlyInto<TelescopeVM>().InSingletonScope();
                    Bind<IDeviceChooserVM>().To<GuiderChooserVM>().WhenInjectedExactlyInto<GuiderVM>().InSingletonScope();
                    Bind<IDeviceChooserVM>().To<SwitchChooserVM>().WhenInjectedExactlyInto<SwitchVM>().InSingletonScope();
                    Bind<IDeviceChooserVM>().To<FlatDeviceChooserVM>().WhenInjectedInto<IFlatDeviceVM>().InSingletonScope();
                    Bind<IDeviceChooserVM>().To<WeatherDataChooserVM>().WhenInjectedExactlyInto<WeatherDataVM>().InSingletonScope();
                    Bind<IDeviceChooserVM>().To<DomeChooserVM>().WhenInjectedExactlyInto<DomeVM>().InSingletonScope();
                    Bind<IDeviceChooserVM>().To<SafetyMonitorChooserVM>().WhenInjectedExactlyInto<SafetyMonitorVM>().InSingletonScope();

                    Bind<ICameraVM>().To<CameraVM>().InSingletonScope();
                    Bind<IFilterWheelVM>().To<FilterWheelVM>().InSingletonScope();
                    Bind<IFocuserVM>().To<FocuserVM>().InSingletonScope();
                    Bind<IRotatorVM>().To<RotatorVM>().InSingletonScope();
                    Bind<ITelescopeVM>().To<TelescopeVM>().InSingletonScope();
                    Bind<IGuiderVM>().To<GuiderVM>().InSingletonScope();
                    Bind<ISwitchVM>().To<SwitchVM>().InSingletonScope();
                    Bind<IFlatDeviceVM>().To<FlatDeviceVM>().InSingletonScope();
                    Bind<IWeatherDataVM>().To<WeatherDataVM>().InSingletonScope();
                    Bind<IDomeVM>().To<DomeVM>().InSingletonScope();
                    Bind<ISafetyMonitorVM>().To<SafetyMonitorVM>().InSingletonScope();

                    Bind<ICameraMediator>().To<CameraMediator>().InSingletonScope();
                    Bind<IFilterWheelMediator>().To<FilterWheelMediator>().InSingletonScope();
                    Bind<IFocuserMediator>().To<FocuserMediator>().InSingletonScope();
                    Bind<IRotatorMediator>().To<RotatorMediator>().InSingletonScope();
                    Bind<ITelescopeMediator>().To<TelescopeMediator>().InSingletonScope();
                    Bind<IGuiderMediator>().To<GuiderMediator>().InSingletonScope();
                    Bind<ISwitchMediator>().To<SwitchMediator>().InSingletonScope();
                    Bind<IFlatDeviceMediator>().To<FlatDeviceMediator>().InSingletonScope();
                    Bind<IWeatherDataMediator>().To<WeatherDataMediator>().InSingletonScope();
                    Bind<IDomeMediator>().To<DomeMediator>().InSingletonScope();
                    Bind<ISafetyMonitorMediator>().To<SafetyMonitorMediator>().InSingletonScope();

                    Bind<IImagingVM>().To<ImagingVM>().InSingletonScope();
                    Bind<IEquipmentVM>().To<EquipmentVM>().InSingletonScope();
                    Bind<IApplicationDeviceConnectionVM>().To<ApplicationDeviceConnectionVM>().InSingletonScope();
                    Bind<IImageGeometryProvider>().To<ImageGeometryProvider>().InSingletonScope();
                    Bind<IOptionsVM>().To<OptionsVM>().InSingletonScope();
                    Bind<ISkyAtlasVM>().To<SkyAtlasVM>().InSingletonScope();
                    Bind<IFramingAssistantVM>().To<FramingAssistantVM>().InSingletonScope();
                    Bind<IFocusTargetsVM>().To<FocusTargetsVM>().InSingletonScope();
                    Bind<IAutoFocusToolVM>().To<AutoFocusToolVM>().InSingletonScope();
                    Bind<IThumbnailVM>().To<ThumbnailVM>().InSingletonScope();
                    Bind<IDockManagerVM>().To<DockManagerVM>().InSingletonScope();
                    Bind<IApplicationStatusVM>().To<ApplicationStatusVM>().InSingletonScope();
                    Bind<IVersionCheckVM>().To<VersionCheckVM>().InSingletonScope();
                    Bind<IImageControlVM>().To<ImageControlVM>().InSingletonScope();
                    Bind<IImageHistoryVM>().To<ImageHistoryVM>().InSingletonScope();
                    Bind<IImageStatisticsVM>().To<ImageStatisticsVM>().InSingletonScope();                    

                    Bind<IFlatWizardUserPromptVM>().To<FlatWizardUserPromptVM>().InSingletonScope();
                    Bind<ITwilightCalculator>().To<TwilightCalculator>().InSingletonScope();
                    Bind<IMicroCacheFactory>().To<DefaultMicroCacheFactory>().InSingletonScope();
                    Bind<ISbigSdk>().To<SbigSdk>().InSingletonScope();
                    Bind<ProjectVersion>().ToMethod(f => new ProjectVersion(NINA.Core.Utility.CoreUtil.Version)).InSingletonScope();
                    BindPluggable<IStarDetection, StarDetection>();
                    BindPluggable<IStarAnnotator, StarAnnotator>();
                    BindPluggable<IAutoFocusVMFactory, BuiltInAutoFocusVMFactory>();
                    Bind<IImageDataFactory>().To<ImageDataFactory>().InSingletonScope();
                    Bind<IExposureDataFactory>().To<ExposureDataFactory>().InSingletonScope();
                    Bind<IAutoFocusVMFactory>().To<PluggableAutoFocusVMFactory>().InSingletonScope();
                    Bind<IMeridianFlipVMFactory>().To<MeridianFlipVMFactory>().InSingletonScope();
                    Bind<IPluggableBehaviorManager>().To<PluggableBehaviorManager>().InSingletonScope();

                    Bind<IAnchorablePlateSolverVM>().To<AnchorablePlateSolverVM>().InSingletonScope();
                    Bind<IAnchorableSnapshotVM>().To<AnchorableSnapshotVM>().InSingletonScope();

                    Bind<IImagingMediator>().To<ImagingMediator>().InSingletonScope();
                    Bind<IApplicationStatusMediator>().To<ApplicationStatusMediator>().InSingletonScope();
                    Bind<IApplicationMediator>().To<ApplicationMediator>().InSingletonScope();
                    Bind<ISequenceMediator>().To<SequenceMediator>().InSingletonScope();

                    Bind<IWindowServiceFactory>().To<WindowServiceFactory>().InSingletonScope();
                    Bind<IPlanetariumFactory>().To<PlanetariumFactory>().InSingletonScope();
                    Bind<IAllDeviceConsumer>().To<AllDeviceConsumer>().InSingletonScope();

                    Bind<IApplicationResourceDictionary>().To<ApplicationResourceDictionary>();
                    Bind<INighttimeCalculator>().To<NighttimeCalculator>().InSingletonScope();
                    Bind<IDeepSkyObjectSearchVM>().To<DeepSkyObjectSearchVM>();
                    Bind<IDomeSynchronization>().To<DomeSynchronization>().InSingletonScope();
                    Bind<IDomeFollower>().To<DomeFollower>().InSingletonScope();
                    Bind<IDeviceUpdateTimerFactory>().To<DefaultDeviceUpateTimerFactory>().InSingletonScope();

                    if (DllLoader.IsX86()) {
                        Bind<IImageSaveMediator>().To<ImageSaveMediatorX86>().InSingletonScope();
                    } else {
                        Bind<IImageSaveMediator>().To<ImageSaveMediator>().InSingletonScope();
                    }

                    Bind<IFlatWizardVM>().ToMethod(f => new FlatWizardVM(f.Kernel.Get<IProfileService>(),
                        new ImagingVM(
                            f.Kernel.Get<IProfileService>(), new ImagingMediator(), f.Kernel.Get<ICameraMediator>(),
                            f.Kernel.Get<ITelescopeMediator>(), f.Kernel.Get<IFilterWheelMediator>(), f.Kernel.Get<IFocuserMediator>(),
                            f.Kernel.Get<IRotatorMediator>(), f.Kernel.Get<IGuiderMediator>(), f.Kernel.Get<IWeatherDataMediator>(),
                            f.Kernel.Get<IApplicationStatusMediator>(),
                            new ImageControlVM(f.Kernel.Get<IProfileService>(), f.Kernel.Get<ICameraMediator>(), f.Kernel.Get<ITelescopeMediator>(), f.Kernel.Get<IImagingMediator>(), f.Kernel.Get<IApplicationStatusMediator>()),
                            new ImageStatisticsVM(f.Kernel.Get<IProfileService>()),
                            f.Kernel.Get<IImageHistoryVM>()), f.Kernel.Get<IFlatWizardUserPromptVM>(),
                        f.Kernel.Get<ICameraMediator>(), f.Kernel.Get<IFilterWheelMediator>(), f.Kernel.Get<ITelescopeMediator>(),
                        f.Kernel.Get<IFlatDeviceMediator>(), f.Kernel.Get<IImageGeometryProvider>(), f.Kernel.Get<IApplicationStatusMediator>(), f.Kernel.Get<IMyMessageBoxVM>(),
                        f.Kernel.Get<ITwilightCalculator>(),
                        f.Kernel.Get<IImageSaveMediator>())).InSingletonScope();

                    Bind<IImageSaveController>().To<ImageSaveController>().InSingletonScope();
                    Bind<ISGPServiceHost>().To<SGPServiceHost>().InSingletonScope();                    
                    Bind<ISGPServiceBackend>().To<SGPServiceBackend>().InSingletonScope();
                    Bind<ISequenceNavigationVM>().To<SequenceNavigationVM>().InSingletonScope();
                    Bind<IMyMessageBoxVM>().To<MyMessageBoxVM>();

                    Bind<IPlateSolverFactory>().To<PlateSolverFactoryProxy>().InSingletonScope();

                    Bind<IPluginLoader>().To<PluginLoader>().InSingletonScope();

                    Bind<IPluginsVM>().To<PluginsVM>().InSingletonScope();
                } catch (Exception e) {
                    Logger.Error(e);
                    throw;
                }
            }
        }
    }
}