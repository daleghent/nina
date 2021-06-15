#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.API.SGP;
using NINA.Equipment.Equipment.MyPlanetarium;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Astrometry;
using NINA.ViewModel;
using NINA.ViewModel.FlatWizard;
using NINA.ViewModel.FramingAssistant;
using NINA.ViewModel.ImageHistory;
using NINA.ViewModel.Imaging;
using NINA.ViewModel.Interfaces;
using NINA.ViewModel.Sequencer;
using Ninject;
using Ninject.Modules;
using System;
using System.Windows;
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
using NINA.WPF.Base.ViewModel.AutoFocus;
using NINA.WPF.Base.ViewModel.Equipment.WeatherData;
using NINA.WPF.Base.ViewModel.Equipment.Telescope;
using NINA.WPF.Base.ViewModel.Equipment.Dome;
using NINA.WPF.Base.ViewModel.Equipment.FilterWheel;
using NINA.WPF.Base.ViewModel.Equipment.SafetyMonitor;
using NINA.WPF.Base.ViewModel.Equipment.Focuser;
using NINA.WPF.Base.ViewModel.Equipment.Guider;
using NINA.WPF.Base.ViewModel.Equipment.Rotator;
using NINA.WPF.Base.ViewModel.Imaging;
using NINA.Plugin.Interfaces;
using NINA.Interfaces;
using NINA.ViewModel.Plugins;

namespace NINA.Utility {

    internal class IoCBindings : NinjectModule {
        private readonly IProfileService _profileService;

        public IoCBindings(IProfileService profileService) =>
            _profileService = profileService;

        public override void Load() {
            using (MyStopWatch.Measure()) {
                try {
                    Bind<IProfileService>().ToMethod(f => _profileService);
                    Bind<IProfile>().ToMethod(f => f.Kernel.Get<ProfileService>().ActiveProfile);

                    Bind<IApplicationVM>().To<ApplicationVM>().InSingletonScope();
                    Bind<ICameraVM>().To<CameraVM>().InSingletonScope();
                    Bind<IImagingVM>().To<ImagingVM>().InSingletonScope();
                    Bind<IEquipmentVM>().To<EquipmentVM>().InSingletonScope();
                    Bind<IApplicationDeviceConnectionVM>().To<ApplicationDeviceConnectionVM>().InSingletonScope();
                    Bind<IImageGeometryProvider>().To<ImageGeometryProvider>().InSingletonScope();
                    Bind<ISwitchVM>().To<SwitchVM>().InSingletonScope();
                    Bind<IOptionsVM>().To<OptionsVM>().InSingletonScope();
                    Bind<IFlatDeviceVM>().To<FlatDeviceVM>().InSingletonScope();
                    Bind<IGuiderVM>().To<GuiderVM>().InSingletonScope();
                    Bind<IExposureCalculatorVM>().To<ExposureCalculatorVM>().InSingletonScope();
                    Bind<IPolarAlignmentVM>().To<PolarAlignmentVM>().InSingletonScope();
                    Bind<ISkyAtlasVM>().To<SkyAtlasVM>().InSingletonScope();
                    Bind<IFramingAssistantVM>().To<FramingAssistantVM>().InSingletonScope();
                    Bind<IFocusTargetsVM>().To<FocusTargetsVM>().InSingletonScope();
                    Bind<IAutoFocusToolVM>().To<AutoFocusToolVM>().InSingletonScope();
                    Bind<ITelescopeVM>().To<TelescopeVM>().InSingletonScope();
                    Bind<IWeatherDataVM>().To<WeatherDataVM>().InSingletonScope();
                    Bind<IDomeVM>().To<DomeVM>().InSingletonScope();
                    Bind<IThumbnailVM>().To<ThumbnailVM>().InSingletonScope();
                    Bind<IDockManagerVM>().To<DockManagerVM>().InSingletonScope();
                    Bind<IRotatorVM>().To<RotatorVM>().InSingletonScope();
                    Bind<ISafetyMonitorVM>().To<SafetyMonitorVM>().InSingletonScope();
                    Bind<IFilterWheelVM>().To<FilterWheelVM>().InSingletonScope();
                    Bind<IApplicationStatusVM>().To<ApplicationStatusVM>().InSingletonScope();
                    Bind<IFocuserVM>().To<FocuserVM>().InSingletonScope();
                    Bind<IVersionCheckVM>().To<VersionCheckVM>().InSingletonScope();
                    Bind<IImageControlVM>().To<ImageControlVM>().InSingletonScope();
                    Bind<IImageHistoryVM>().To<ImageHistoryVM>().InSingletonScope();
                    Bind<IImageStatisticsVM>().To<ImageStatisticsVM>().InSingletonScope();
                    Bind<IDeviceChooserVM>().To<DomeChooserVM>().WhenInjectedExactlyInto<DomeVM>().InSingletonScope();
                    Bind<IDeviceChooserVM>().To<CameraChooserVM>().WhenInjectedExactlyInto<CameraVM>().InSingletonScope();
                    Bind<IDeviceChooserVM>().To<CameraChooserVM>().WhenInjectedExactlyInto<SGPServiceBackend>().InSingletonScope();
                    Bind<IDeviceChooserVM>().To<FilterWheelChooserVM>().WhenInjectedExactlyInto<FilterWheelVM>().InSingletonScope();
                    Bind<IDeviceChooserVM>().To<GuiderChooserVM>().WhenInjectedExactlyInto<GuiderVM>().InSingletonScope();
                    Bind<IFlatWizardUserPromptVM>().To<FlatWizardUserPromptVM>().InSingletonScope();
                    Bind<ITwilightCalculator>().To<TwilightCalculator>().InSingletonScope();

                    Bind<ProjectVersion>().ToMethod(f => new ProjectVersion(NINA.Core.Utility.CoreUtil.Version)).InSingletonScope();

                    Bind<IFlatWizardVM>().ToMethod(f => new FlatWizardVM(f.Kernel.Get<IProfileService>(),
                        new ImagingVM(
                            f.Kernel.Get<IProfileService>(), new ImagingMediator(), f.Kernel.Get<ICameraMediator>(),
                            f.Kernel.Get<ITelescopeMediator>(), f.Kernel.Get<IFilterWheelMediator>(), f.Kernel.Get<IFocuserMediator>(),
                            f.Kernel.Get<IRotatorMediator>(), f.Kernel.Get<IGuiderMediator>(), f.Kernel.Get<IWeatherDataMediator>(),
                            f.Kernel.Get<IApplicationStatusMediator>(),
                            new ImageControlVM(f.Kernel.Get<IProfileService>(), f.Kernel.Get<ICameraMediator>(), f.Kernel.Get<ITelescopeMediator>(), f.Kernel.Get<IApplicationStatusMediator>()),
                            new ImageStatisticsVM(f.Kernel.Get<IProfileService>())), f.Kernel.Get<IFlatWizardUserPromptVM>(),
                        f.Kernel.Get<ICameraMediator>(), f.Kernel.Get<IFilterWheelMediator>(), f.Kernel.Get<ITelescopeMediator>(),
                        f.Kernel.Get<IFlatDeviceMediator>(), f.Kernel.Get<IImageGeometryProvider>(), f.Kernel.Get<IApplicationStatusMediator>(), f.Kernel.Get<IMyMessageBoxVM>(),
                        f.Kernel.Get<ITwilightCalculator>())).InSingletonScope();

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
                    Bind<IApplicationMediator>().To<ApplicationMediator>().InSingletonScope();
                    Bind<ISequenceMediator>().To<SequenceMediator>().InSingletonScope();
                    Bind<ISafetyMonitorMediator>().To<SafetyMonitorMediator>().InSingletonScope();

                    Bind<IWindowServiceFactory>().To<WindowServiceFactory>().InSingletonScope();
                    Bind<IPlanetariumFactory>().To<PlanetariumFactory>().InSingletonScope();
                    Bind<IAllDeviceConsumer>().To<AllDeviceConsumer>().InSingletonScope();

                    Bind<ISharpCapSensorAnalysisReader>().To<DefaultSharpCapSensorAnalysisReader>();
                    Bind<IApplicationResourceDictionary>().To<ApplicationResourceDictionary>();
                    Bind<INighttimeCalculator>().To<NighttimeCalculator>().InSingletonScope();
                    Bind<IDeepSkyObjectSearchVM>().To<DeepSkyObjectSearchVM>();
                    Bind<IDomeSynchronization>().To<DomeSynchronization>().InSingletonScope();
                    Bind<IDomeFollower>().To<DomeFollower>().InSingletonScope();
                    Bind<IDeviceUpdateTimerFactory>().To<DefaultDeviceUpateTimerFactory>().InSingletonScope();
                    Bind<IDeviceChooserVM>().To<FlatDeviceChooserVM>().WhenInjectedInto<IFlatDeviceVM>().InSingletonScope();
                    Bind<IDeviceFactory>().To<FlatDeviceFactory>().WhenInjectedInto<FlatDeviceChooserVM>().InSingletonScope();
                    Bind<IDeviceChooserVM>().To<FocuserChooserVM>().WhenInjectedInto<IFocuserVM>().InSingletonScope();
                    Bind<IDeviceFactory>().To<FocuserFactory>().WhenInjectedInto<FocuserChooserVM>().InSingletonScope();

                    Bind<IImageSaveMediator>().To<ImageSaveMediator>().InSingletonScope();
                    Bind<IImageSaveController>().To<ImageSaveController>().InSingletonScope();
                    Bind<ISGPServiceHost>().To<SGPServiceHost>().InSingletonScope();
                    Bind<ISGPService>().To<SGPServiceFrontend>().InSingletonScope();
                    Bind<ISGPServiceBackend>().To<SGPServiceBackend>().InSingletonScope();
                    Bind<ISequenceNavigationVM>().To<SequenceNavigationVM>().InSingletonScope();
                    Bind<IMyMessageBoxVM>().To<MyMessageBoxVM>();

                    Bind<IPluginLoader>().To<PluginLoader>().InSingletonScope();

                    Bind<IPluginsVM>().To<PluginsVM>().InSingletonScope();
                } catch (Exception e) {
                    Logger.Error(e);
                    throw e;
                }
            }
        }
    }
}