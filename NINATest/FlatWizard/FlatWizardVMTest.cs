#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using Moq;
using NINA.Model;
using NINA.Model.ImageData;
using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Model.MyFlatDevice;
using NINA.MyMessageBox;
using NINA.Profile;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Enum;
using NINA.Utility.Mediator;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.SerialCommunication;
using NINA.Utility.WindowService;
using NINA.ViewModel;
using NINA.ViewModel.FlatWizard;
using NINA.ViewModel.Interfaces;
using Nito.AsyncEx;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using ICommand = System.Windows.Input.ICommand;

namespace NINATest.FlatWizard {

    [TestFixture]
    public class FlatWizardVMTest {
        private IFlatWizardVM sut;

        private Mock<IProfileService> profileServiceMock;
        private Mock<IImagingVM> imagingVMMock;
        private Mock<IFlatWizardUserPromptVM> errorDialogMock;
        private Mock<ICameraMediator> cameraMediatorMock;
        private Mock<IFilterWheelMediator> filterWheelMediatorMock;
        private Mock<ITelescopeMediator> telescopeMediatorMock;
        private Mock<IFlatDeviceMediator> flatDeviceMediatorMock;
        private Mock<IImageGeometryProvider> imageGeometryProviderMock;
        private Mock<IApplicationStatusMediator> applicationStatusMediatorMock;
        private Mock<IProfile> profileMock;
        private Mock<IFlatWizardSettings> flatWizardSettingsMock;
        private Mock<ICameraSettings> cameraSettingsMock;
        private Mock<IFilterWheelSettings> filterWheelSettingsMock;
        private Mock<IExposureData> exposureMock;
        private Mock<IFlatDeviceSettings> flatDeviceSettingsMock;
        private Mock<IWindowService> windowServiceMock;
        private Mock<IMyMessageBoxVM> messageBoxVMMock;
        private Mock<IDispatcherOperationWrapper> dispatcherOperationWrapperMock;
        private Mock<ITwilightCalculator> twilightCalculatorMock;
        private FlatDeviceInfo flatDeviceInfo;

        [OneTimeSetUp]
        public void OneTimeSetup() {
            profileServiceMock = new Mock<IProfileService>();
            imagingVMMock = new Mock<IImagingVM>();
            errorDialogMock = new Mock<IFlatWizardUserPromptVM>();
            cameraMediatorMock = new Mock<ICameraMediator>();
            filterWheelMediatorMock = new Mock<IFilterWheelMediator>();
            telescopeMediatorMock = new Mock<ITelescopeMediator>();
            applicationStatusMediatorMock = new Mock<IApplicationStatusMediator>();
            imageGeometryProviderMock = new Mock<IImageGeometryProvider>();
            profileMock = new Mock<IProfile>();
            flatWizardSettingsMock = new Mock<IFlatWizardSettings>();
            cameraSettingsMock = new Mock<ICameraSettings>();
            filterWheelSettingsMock = new Mock<IFilterWheelSettings>();
            flatDeviceMediatorMock = new Mock<IFlatDeviceMediator>();
            flatDeviceSettingsMock = new Mock<IFlatDeviceSettings>();
            exposureMock = new Mock<IExposureData>();
            windowServiceMock = new Mock<IWindowService>();
            messageBoxVMMock = new Mock<IMyMessageBoxVM>();
            dispatcherOperationWrapperMock = new Mock<IDispatcherOperationWrapper>();
            twilightCalculatorMock = new Mock<ITwilightCalculator>();
        }

        [SetUp]
        public void Init() {
            profileServiceMock.Reset();
            imagingVMMock.Reset();
            errorDialogMock.Reset();
            cameraMediatorMock.Reset();
            filterWheelMediatorMock.Reset();
            telescopeMediatorMock.Reset();
            applicationStatusMediatorMock.Reset();
            imageGeometryProviderMock.Reset();
            profileMock.Reset();
            flatWizardSettingsMock.Reset();
            cameraSettingsMock.Reset();
            filterWheelSettingsMock.Reset();
            flatDeviceMediatorMock.Reset();
            flatDeviceSettingsMock.Reset();
            windowServiceMock.Reset();
            messageBoxVMMock.Reset();
            dispatcherOperationWrapperMock.Reset();
            twilightCalculatorMock.Reset();
            dispatcherOperationWrapperMock.Setup(m => m.GetAwaiter()).Returns(Task.CompletedTask.GetAwaiter);
            profileServiceMock.Setup(m => m.ActiveProfile).Returns(profileMock.Object);
            profileMock.Setup(m => m.FlatWizardSettings).Returns(flatWizardSettingsMock.Object);
            profileMock.Setup(m => m.CameraSettings).Returns(cameraSettingsMock.Object);
            profileMock.Setup(m => m.FilterWheelSettings).Returns(filterWheelSettingsMock.Object);
            profileMock.Setup(m => m.FlatDeviceSettings).Returns(flatDeviceSettingsMock.Object);
            profileServiceMock.Setup(m => m.ActiveProfile.ImageSettings.AutoStretchFactor).Returns(1d);
            profileServiceMock.Setup(m => m.ActiveProfile.ImageSettings.BlackClipping).Returns(0.1);
            profileServiceMock.Setup(m => m.ActiveProfile.ImageFileSettings.FilePath)
                .Returns(Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName);
            profileServiceMock.Setup(m => m.ActiveProfile.AstrometrySettings.Latitude).Returns(33.005699);
            profileServiceMock.Setup(m => m.ActiveProfile.AstrometrySettings.Longitude).Returns(-117.103254);
            filterWheelSettingsMock.Setup(m => m.FilterWheelFilters).Returns(new ObserveAllCollection<FilterInfo>());

            var renderedImageMock = new Mock<IRenderedImage>();
            renderedImageMock.Setup(m => m.Image).Returns(new BitmapImage());
            imagingVMMock.Setup(m => m.PrepareImage(It.IsAny<IImageData>(), It.IsAny<PrepareImageParameters>(),
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(renderedImageMock.Object));
            exposureMock.Reset();
            imagingVMMock.Setup(m => m.CaptureImage(It.IsAny<CaptureSequence>(),
                    It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<String>()))
                .Returns(Task.FromResult(exposureMock.Object));

            flatDeviceInfo = new FlatDeviceInfo {
                Brightness = 1.0,
                Connected = true,
                CoverState = CoverState.Open,
                Description = "Some description",
                DriverInfo = "Some driverInfo",
                LightOn = false,
                DriverVersion = "200",
                MaxBrightness = 255,
                MinBrightness = 0,
                Name = "Some name"
            };
        }

        [TearDown]
        public void Dispose() {
        }

        [Test]
        public void Constructor_WhenInitialized_DoAllNecessaryCallsAndVerifyData() {
            var filters = new List<FilterInfo> {
                new FilterInfo("Filter", 0, 0), new FilterInfo("Filter2", 0, 0)
            };
            filterWheelSettingsMock.SetupGet(m => m.FilterWheelFilters)
                .Returns(new ObserveAllCollection<FilterInfo>(filters));

            // act
            sut = new FlatWizardVM(profileServiceMock.Object, imagingVMMock.Object, errorDialogMock.Object,
                cameraMediatorMock.Object, filterWheelMediatorMock.Object, telescopeMediatorMock.Object,
                flatDeviceMediatorMock.Object, imageGeometryProviderMock.Object,
                applicationStatusMediatorMock.Object, messageBoxVMMock.Object, twilightCalculatorMock.Object);

            // assert
            sut.StartFlatSequenceCommand.Should().NotBeNull();
            sut.CancelFlatExposureSequenceCommand.Should().NotBeNull();
            sut.PauseFlatExposureSequenceCommand.Should().NotBeNull();
            sut.ResumeFlatExposureSequenceCommand.Should().NotBeNull();

            sut.SingleFlatWizardFilterSettings.Should().NotBeNull();
            sut.SingleFlatWizardFilterSettings.Filter.Should().BeNull();

            flatWizardSettingsMock.Verify(m => m.HistogramMeanTarget, Times.Once);
            flatWizardSettingsMock.Verify(m => m.HistogramTolerance, Times.Once);
            flatWizardSettingsMock.Verify(m => m.StepSize, Times.Once);
            cameraSettingsMock.Verify(m => m.MaxFlatExposureTime, Times.Once);
            cameraSettingsMock.Verify(m => m.MinFlatExposureTime, Times.Once);

            flatWizardSettingsMock.Verify(m => m.FlatCount, Times.AtMost(2));
            flatWizardSettingsMock.Verify(m => m.BinningMode, Times.AtMost(2));

            filterWheelSettingsMock.Verify(m => m.FilterWheelFilters, Times.AtMost(2));

            cameraMediatorMock.Verify(m => m.RegisterConsumer(sut), Times.Once);
            flatDeviceMediatorMock.Verify(m => m.RegisterConsumer(sut), Times.Once);

            sut.Filters.Select(f => f.Filter).Should().BeEquivalentTo(filters);
        }

        [Test]
        public void UpdateDeviceInfo_WhenCalled_SetVariablesAndCameraInfoInAllFilters() {
            filterWheelSettingsMock.SetupGet(m => m.FilterWheelFilters)
                .Returns(new ObserveAllCollection<FilterInfo> {
                    new FilterInfo("Filter", 0, 0), new FilterInfo("Filter2", 0, 0)
                });

            // setup
            sut = new FlatWizardVM(profileServiceMock.Object, imagingVMMock.Object, errorDialogMock.Object,
                cameraMediatorMock.Object, filterWheelMediatorMock.Object, telescopeMediatorMock.Object,
                flatDeviceMediatorMock.Object, imageGeometryProviderMock.Object,
                applicationStatusMediatorMock.Object, messageBoxVMMock.Object, twilightCalculatorMock.Object);

            var cameraInfo = new CameraInfo {
                Connected = true,
                BitDepth = 16
            };

            // act
            sut.UpdateDeviceInfo(cameraInfo);

            // assert

            sut.CameraConnected.Should().BeTrue();
            sut.SingleFlatWizardFilterSettings.BitDepth.Should().Be(cameraInfo.BitDepth);
            sut.Filters.Select(m => m.BitDepth).Should().AllBeEquivalentTo(cameraInfo.BitDepth);
        }

        [Test]
        public void UpdateFilterWheelSettings_WhenCalled_UpdateFiltersListAndKeepSelectedFilter() {
            // setup
            var filters = new ObserveAllCollection<FilterInfo> { new FilterInfo("Filter", 0, 0), new FilterInfo("Filter2", 0, 0) };
            filterWheelSettingsMock.SetupGet(m => m.FilterWheelFilters)
                .Returns(filters);

            sut = new FlatWizardVM(profileServiceMock.Object, imagingVMMock.Object, errorDialogMock.Object,
                cameraMediatorMock.Object, filterWheelMediatorMock.Object, telescopeMediatorMock.Object,
                flatDeviceMediatorMock.Object, imageGeometryProviderMock.Object,
                applicationStatusMediatorMock.Object, messageBoxVMMock.Object, twilightCalculatorMock.Object);

            var selectedFilter = filters[0];
            sut.SelectedFilter = selectedFilter;

            var cameraInfo = new CameraInfo {
                Connected = true,
                BitDepth = 16
            };

            // pre-assert
            sut.Filters.Select(f => f.Filter).Should().BeEquivalentTo(filters);
            sut.SelectedFilter.Should().BeEquivalentTo(selectedFilter);

            // remove one filter
            filters.RemoveAt(1);

            // update the camera info on existing filters
            sut.UpdateDeviceInfo(cameraInfo);

            // assert cameraInfo on all filters and unused filters are removed
            sut.Filters.Select(f => f.Filter).Should().BeEquivalentTo(filters);
            sut.Filters.Select(f => f.BitDepth).Should().AllBeEquivalentTo(cameraInfo.BitDepth);
            sut.SelectedFilter.Should().BeEquivalentTo(selectedFilter);

            // add another filter
            filters.Add(new FilterInfo("Filter2", 0, 0));
            filters.Add(new FilterInfo("Filter3", 0, 0));

            // assert cameraInfo still on all filters even on the ones that were added
            sut.Filters.Select(f => f.Filter).Should().BeEquivalentTo(filters);
            sut.Filters.Select(f => f.BitDepth).Should().AllBeEquivalentTo(cameraInfo.BitDepth);
            sut.SelectedFilter.Should().BeEquivalentTo(selectedFilter);
        }

        [Test]
        public void UpdateFlatDeviceSettingsAndCheckFlatMagicWithNullFlatDevice() {
            sut = new FlatWizardVM(profileServiceMock.Object, imagingVMMock.Object, errorDialogMock.Object,
                cameraMediatorMock.Object, filterWheelMediatorMock.Object, telescopeMediatorMock.Object,
                flatDeviceMediatorMock.Object, imageGeometryProviderMock.Object,
                applicationStatusMediatorMock.Object, messageBoxVMMock.Object, twilightCalculatorMock.Object);

            sut.StartFlatSequenceCommand.Execute(new object());
            flatDeviceMediatorMock.Verify(m => m.CloseCover(It.IsAny<CancellationToken>()), Times.Never);
            flatDeviceMediatorMock.Verify(m => m.OpenCover(It.IsAny<CancellationToken>()), Times.Never);
        }

        private static Task<IImageData> GetMeanImage(int mean) {
            var statMock = new Mock<IImageStatistics>();
            statMock.Setup(m => m.Mean).Returns(mean);
            var imageDataMock = new Mock<IImageData>();
            imageDataMock.Setup(m => m.MetaData).Returns(new ImageMetaData());
            imageDataMock.Setup(m => m.Statistics).Returns(new Nito.AsyncEx.AsyncLazy<IImageStatistics>(() => Task.FromResult(statMock.Object)));
            return Task.FromResult(imageDataMock.Object);
        }

        [Test]
        public async Task TestFindFlatExposureTimeCorrectAduRightAway() {
            sut = new FlatWizardVM(profileServiceMock.Object, imagingVMMock.Object, errorDialogMock.Object,
                cameraMediatorMock.Object, filterWheelMediatorMock.Object, telescopeMediatorMock.Object,
                flatDeviceMediatorMock.Object, imageGeometryProviderMock.Object,
                applicationStatusMediatorMock.Object, messageBoxVMMock.Object, twilightCalculatorMock.Object);
            sut.UpdateDeviceInfo(flatDeviceInfo);

            exposureMock.Setup(m => m.ToImageData(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .Returns(GetMeanImage(32768));

            var settings = new FlatWizardFilterSettings {
                FlatWizardMode = FlatWizardMode.DYNAMICEXPOSURE,
                MinFlatExposureTime = 1d,
                MaxFlatDeviceBrightness = 100d
            };
            var filterInfo = new FilterInfo {
                AutoFocusExposureTime = 1d,
                AutoFocusFilter = false,
                FlatWizardFilterSettings = settings,
                Position = 0,
                FocusOffset = 1,
                Name = "Clear"
            };

            var result = await sut.FindFlatExposureTime(new PauseToken(), new FlatWizardFilterSettingsWrapper(filterInfo, settings, 16,
                new CameraInfo(), flatDeviceInfo));

            result.Should().Be(1d);
            flatDeviceMediatorMock.Verify(m => m.SetBrightness(It.IsAny<double>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task TestFindFlatExposureTimeCorrectAduAfterOneStep() {
            sut = new FlatWizardVM(profileServiceMock.Object, imagingVMMock.Object, errorDialogMock.Object,
                cameraMediatorMock.Object, filterWheelMediatorMock.Object, telescopeMediatorMock.Object,
                flatDeviceMediatorMock.Object, imageGeometryProviderMock.Object,
                applicationStatusMediatorMock.Object, messageBoxVMMock.Object, twilightCalculatorMock.Object);
            sut.UpdateDeviceInfo(flatDeviceInfo);

            exposureMock.SetupSequence(m => m.ToImageData(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .Returns(GetMeanImage(5000))
                .Returns(GetMeanImage(32768));

            var wrapper = new FlatWizardFilterSettings {
                FlatWizardMode = FlatWizardMode.DYNAMICEXPOSURE,
                MinFlatExposureTime = 1d,
                StepSize = 1d,
                MaxFlatDeviceBrightness = 100d
            };
            var result = await sut.FindFlatExposureTime(new PauseToken(), new FlatWizardFilterSettingsWrapper(new FilterInfo(), wrapper, 16,
                new CameraInfo(), flatDeviceInfo));

            result.Should().Be(2d);
            flatDeviceMediatorMock.Verify(m => m.SetBrightness(It.IsAny<double>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Test]
        public async Task TestFindFlatExposureTimeAskUserAfterThreeStepsAndCancel() {
            sut = new FlatWizardVM(profileServiceMock.Object, imagingVMMock.Object, errorDialogMock.Object,
                cameraMediatorMock.Object, filterWheelMediatorMock.Object, telescopeMediatorMock.Object,
                flatDeviceMediatorMock.Object, imageGeometryProviderMock.Object,
                applicationStatusMediatorMock.Object, messageBoxVMMock.Object, twilightCalculatorMock.Object) { WindowService = windowServiceMock.Object };
            sut.UpdateDeviceInfo(flatDeviceInfo);

            exposureMock.SetupSequence(m => m.ToImageData(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .Returns(GetMeanImage(5000))
                .Returns(GetMeanImage(5000))
                .Returns(GetMeanImage(5000));

            errorDialogMock.Setup(m => m.Result).Returns(FlatWizardUserPromptResult.Cancel);

            windowServiceMock.Setup(m => m.ShowDialog(It.IsAny<object>(), It.IsAny<string>(),
                                It.IsAny<ResizeMode>(), It.IsAny<WindowStyle>(),
                                It.IsAny<ICommand>()))
                            .Returns(dispatcherOperationWrapperMock.Object);

            var wrapper = new FlatWizardFilterSettings {
                FlatWizardMode = FlatWizardMode.DYNAMICEXPOSURE,
                MinFlatExposureTime = 1d,
                StepSize = 1d,
                MaxFlatDeviceBrightness = 100d
            };

            Func<Task> act = async () => {
                await sut.FindFlatExposureTime(new PauseToken(), new FlatWizardFilterSettingsWrapper(new FilterInfo(),
                    wrapper, 16,
                    new CameraInfo(), flatDeviceInfo));
            };

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Test]
        public async Task TestFindFlatExposureTimeAskUserAfterFiveStepsAndCancel() {
            sut = new FlatWizardVM(profileServiceMock.Object, imagingVMMock.Object, errorDialogMock.Object,
                cameraMediatorMock.Object, filterWheelMediatorMock.Object, telescopeMediatorMock.Object,
                flatDeviceMediatorMock.Object, imageGeometryProviderMock.Object,
                applicationStatusMediatorMock.Object, messageBoxVMMock.Object, twilightCalculatorMock.Object) { WindowService = windowServiceMock.Object };
            sut.UpdateDeviceInfo(flatDeviceInfo);

            exposureMock.SetupSequence(m => m.ToImageData(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .Returns(GetMeanImage(5000))
                .Returns(GetMeanImage(50000))
                .Returns(GetMeanImage(24000))
                .Returns(GetMeanImage(24000))
                .Returns(GetMeanImage(24000))
                .Returns(GetMeanImage(24000));

            errorDialogMock.Setup(m => m.Result).Returns(FlatWizardUserPromptResult.Cancel);

            windowServiceMock.Setup(m => m.ShowDialog(It.IsAny<object>(), It.IsAny<string>(),
                    It.IsAny<ResizeMode>(), It.IsAny<WindowStyle>(),
                    It.IsAny<ICommand>()))
                .Returns(dispatcherOperationWrapperMock.Object);

            var wrapper = new FlatWizardFilterSettings {
                FlatWizardMode = FlatWizardMode.DYNAMICEXPOSURE,
                MinFlatExposureTime = 1d,
                StepSize = 1d,
                MaxFlatDeviceBrightness = 100d
            };

            Func<Task> act = async () => {
                await sut.FindFlatExposureTime(new PauseToken(), new FlatWizardFilterSettingsWrapper(new FilterInfo(),
                    wrapper, 16,
                    new CameraInfo(), flatDeviceInfo));
            };

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Test]
        public async Task TestFindFlatExposureTimeAskUserAfterThreeStepsAndResetAndContinueSuccess() {
            sut = new FlatWizardVM(profileServiceMock.Object, imagingVMMock.Object, errorDialogMock.Object,
                cameraMediatorMock.Object, filterWheelMediatorMock.Object, telescopeMediatorMock.Object,
                flatDeviceMediatorMock.Object, imageGeometryProviderMock.Object,
                applicationStatusMediatorMock.Object, messageBoxVMMock.Object, twilightCalculatorMock.Object) { WindowService = windowServiceMock.Object };
            sut.UpdateDeviceInfo(flatDeviceInfo);

            exposureMock.SetupSequence(m => m.ToImageData(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .Returns(GetMeanImage(5000))
                .Returns(GetMeanImage(5000))
                .Returns(GetMeanImage(32768));

            errorDialogMock.Setup(m => m.Result).Returns(FlatWizardUserPromptResult.ResetAndContinue);

            windowServiceMock.Setup(m => m.ShowDialog(It.IsAny<object>(), It.IsAny<string>(),
                                It.IsAny<ResizeMode>(), It.IsAny<WindowStyle>(),
                                It.IsAny<ICommand>()))
                            .Returns(dispatcherOperationWrapperMock.Object);

            var wrapper = new FlatWizardFilterSettings {
                FlatWizardMode = FlatWizardMode.DYNAMICEXPOSURE,
                MinFlatExposureTime = 1d,
                StepSize = 1d,
                MaxFlatDeviceBrightness = 100d
            };

            var result = await sut.FindFlatExposureTime(new PauseToken(), new FlatWizardFilterSettingsWrapper(new FilterInfo(),
                    wrapper, 16,
                    new CameraInfo(), flatDeviceInfo));

            result.Should().Be(1d);
            flatDeviceMediatorMock.Verify(m => m.SetBrightness(It.IsAny<double>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        }

        [Test]
        public async Task TestFindFlatExposureTimeAskUserAfterFiveStepsAndResetAndContinueSuccess() {
            sut = new FlatWizardVM(profileServiceMock.Object, imagingVMMock.Object, errorDialogMock.Object,
                cameraMediatorMock.Object, filterWheelMediatorMock.Object, telescopeMediatorMock.Object,
                flatDeviceMediatorMock.Object, imageGeometryProviderMock.Object,
                applicationStatusMediatorMock.Object, messageBoxVMMock.Object, twilightCalculatorMock.Object) { WindowService = windowServiceMock.Object };
            sut.UpdateDeviceInfo(flatDeviceInfo);

            exposureMock.SetupSequence(m => m.ToImageData(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .Returns(GetMeanImage(5000))
                .Returns(GetMeanImage(50000))
                .Returns(GetMeanImage(24000))
                .Returns(GetMeanImage(24000))
                .Returns(GetMeanImage(24000))
                .Returns(GetMeanImage(24000))
                .Returns(GetMeanImage(32768));

            errorDialogMock.Setup(m => m.Result).Returns(FlatWizardUserPromptResult.ResetAndContinue);

            windowServiceMock.Setup(m => m.ShowDialog(It.IsAny<object>(), It.IsAny<string>(),
                    It.IsAny<ResizeMode>(), It.IsAny<WindowStyle>(),
                    It.IsAny<ICommand>()))
                .Returns(dispatcherOperationWrapperMock.Object);

            var wrapper = new FlatWizardFilterSettings {
                FlatWizardMode = FlatWizardMode.DYNAMICEXPOSURE,
                MinFlatExposureTime = 1d,
                StepSize = 1d,
                MaxFlatDeviceBrightness = 100d
            };

            var result = await sut.FindFlatExposureTime(new PauseToken(), new FlatWizardFilterSettingsWrapper(new FilterInfo(),
                wrapper, 16,
                new CameraInfo(), flatDeviceInfo));

            result.Should().Be(1d);
            flatDeviceMediatorMock.Verify(m => m.SetBrightness(It.IsAny<double>(), It.IsAny<CancellationToken>()), Times.Exactly(7));
        }

        [Test]
        public async Task TestFindFlatDeviceBrightnessCorrectAduRightAway() {
            sut = new FlatWizardVM(profileServiceMock.Object, imagingVMMock.Object, errorDialogMock.Object,
                cameraMediatorMock.Object, filterWheelMediatorMock.Object, telescopeMediatorMock.Object,
                flatDeviceMediatorMock.Object, imageGeometryProviderMock.Object,
                applicationStatusMediatorMock.Object, messageBoxVMMock.Object, twilightCalculatorMock.Object);
            sut.UpdateDeviceInfo(flatDeviceInfo);

            exposureMock.Setup(m => m.ToImageData(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .Returns(GetMeanImage(32768));

            var settings = new FlatWizardFilterSettings {
                FlatWizardMode = FlatWizardMode.DYNAMICBRIGHTNESS,
                MinFlatExposureTime = 1d,
                MinFlatDeviceBrightness = 10d,
                MaxFlatDeviceBrightness = 100d
            };
            var filterInfo = new FilterInfo {
                AutoFocusExposureTime = 1d,
                AutoFocusFilter = false,
                FlatWizardFilterSettings = settings,
                Position = 0,
                FocusOffset = 1,
                Name = "Clear"
            };

            var result = await sut.FindFlatDeviceBrightness(new PauseToken(), new FlatWizardFilterSettingsWrapper(filterInfo, settings, 16,
                new CameraInfo(), flatDeviceInfo));

            result.Should().Be(10d);
            flatDeviceMediatorMock.Verify(m => m.SetBrightness(It.IsAny<double>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task TestFindFlatDeviceBrightnessCorrectAduAfterOneStep() {
            sut = new FlatWizardVM(profileServiceMock.Object, imagingVMMock.Object, errorDialogMock.Object,
                cameraMediatorMock.Object, filterWheelMediatorMock.Object, telescopeMediatorMock.Object,
                flatDeviceMediatorMock.Object, imageGeometryProviderMock.Object,
                applicationStatusMediatorMock.Object, messageBoxVMMock.Object, twilightCalculatorMock.Object);
            sut.UpdateDeviceInfo(flatDeviceInfo);

            exposureMock.SetupSequence(m => m.ToImageData(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .Returns(GetMeanImage(5000))
                .Returns(GetMeanImage(32768));

            var wrapper = new FlatWizardFilterSettings {
                FlatWizardMode = FlatWizardMode.DYNAMICBRIGHTNESS,
                MinFlatExposureTime = 1d,
                FlatDeviceStepSize = 10d,
                MinFlatDeviceBrightness = 10d,
                MaxFlatDeviceBrightness = 100d
            };
            var result = await sut.FindFlatDeviceBrightness(new PauseToken(), new FlatWizardFilterSettingsWrapper(new FilterInfo(), wrapper, 16,
                new CameraInfo(), flatDeviceInfo));

            result.Should().Be(20d);
            flatDeviceMediatorMock.Verify(m => m.SetBrightness(It.IsAny<double>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Test]
        public async Task TestFindFlatDeviceBrightnessAskUserAfterInitialTooBrightAndCancel() {
            sut = new FlatWizardVM(profileServiceMock.Object, imagingVMMock.Object, errorDialogMock.Object,
                cameraMediatorMock.Object, filterWheelMediatorMock.Object, telescopeMediatorMock.Object,
                flatDeviceMediatorMock.Object, imageGeometryProviderMock.Object,
                applicationStatusMediatorMock.Object, messageBoxVMMock.Object, twilightCalculatorMock.Object) { WindowService = windowServiceMock.Object };
            sut.UpdateDeviceInfo(flatDeviceInfo);

            exposureMock.SetupSequence(m => m.ToImageData(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .Returns(GetMeanImage(50000))
                .Returns(GetMeanImage(50000));

            errorDialogMock.Setup(m => m.Result).Returns(FlatWizardUserPromptResult.Cancel);

            windowServiceMock.Setup(m => m.ShowDialog(It.IsAny<object>(), It.IsAny<string>(),
                    It.IsAny<ResizeMode>(), It.IsAny<WindowStyle>(),
                    It.IsAny<ICommand>())).Returns(dispatcherOperationWrapperMock.Object);

            var wrapper = new FlatWizardFilterSettings {
                FlatWizardMode = FlatWizardMode.DYNAMICBRIGHTNESS,
                MinFlatExposureTime = 1d,
                FlatDeviceStepSize = 10d,
                MinFlatDeviceBrightness = 10d,
                MaxFlatDeviceBrightness = 100d
            };

            Func<Task> act = async () => {
                await sut.FindFlatDeviceBrightness(new PauseToken(), new FlatWizardFilterSettingsWrapper(new FilterInfo(),
                    wrapper, 16,
                    new CameraInfo(), flatDeviceInfo));
            };

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Test]
        public async Task TestFindFlatDeviceBrightnessAskUserAfterInitialTooBrightAndResetAndContinueSuccess() {
            sut = new FlatWizardVM(profileServiceMock.Object, imagingVMMock.Object, errorDialogMock.Object,
                cameraMediatorMock.Object, filterWheelMediatorMock.Object, telescopeMediatorMock.Object,
                flatDeviceMediatorMock.Object, imageGeometryProviderMock.Object,
                applicationStatusMediatorMock.Object, messageBoxVMMock.Object, twilightCalculatorMock.Object) { WindowService = windowServiceMock.Object };
            sut.UpdateDeviceInfo(flatDeviceInfo);

            exposureMock.SetupSequence(m => m.ToImageData(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .Returns(GetMeanImage(50000))
                .Returns(GetMeanImage(32768));

            errorDialogMock.Setup(m => m.Result).Returns(FlatWizardUserPromptResult.ResetAndContinue);

            windowServiceMock.Setup(m => m.ShowDialog(It.IsAny<object>(), It.IsAny<string>(),
                                It.IsAny<ResizeMode>(), It.IsAny<WindowStyle>(),
                                It.IsAny<ICommand>()))
                            .Returns(dispatcherOperationWrapperMock.Object);

            var wrapper = new FlatWizardFilterSettings {
                FlatWizardMode = FlatWizardMode.DYNAMICBRIGHTNESS,
                MinFlatExposureTime = 1d,
                FlatDeviceStepSize = 10d,
                MinFlatDeviceBrightness = 10d,
                MaxFlatDeviceBrightness = 100d
            };

            var result = await sut.FindFlatDeviceBrightness(new PauseToken(), new FlatWizardFilterSettingsWrapper(new FilterInfo(),
                    wrapper, 16,
                    new CameraInfo(), flatDeviceInfo));

            result.Should().Be(10d);
            flatDeviceMediatorMock.Verify(m => m.SetBrightness(It.IsAny<double>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Test]
        public async Task TestFindFlatDeviceBrightnessAskUserAfterFiveStepsBelowLowerBoundAndResetAndContinueSuccess() {
            sut = new FlatWizardVM(profileServiceMock.Object, imagingVMMock.Object, errorDialogMock.Object,
                cameraMediatorMock.Object, filterWheelMediatorMock.Object, telescopeMediatorMock.Object,
                flatDeviceMediatorMock.Object, imageGeometryProviderMock.Object,
                applicationStatusMediatorMock.Object, messageBoxVMMock.Object, twilightCalculatorMock.Object) { WindowService = windowServiceMock.Object };
            sut.UpdateDeviceInfo(flatDeviceInfo);

            exposureMock.SetupSequence(m => m.ToImageData(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .Returns(GetMeanImage(5000))
                .Returns(GetMeanImage(50000))
                .Returns(GetMeanImage(24000))
                .Returns(GetMeanImage(24000))
                .Returns(GetMeanImage(24000))
                .Returns(GetMeanImage(24000))
                .Returns(GetMeanImage(32768));

            errorDialogMock.Setup(m => m.Result).Returns(FlatWizardUserPromptResult.ResetAndContinue);

            windowServiceMock.Setup(m => m.ShowDialog(It.IsAny<object>(), It.IsAny<string>(),
                    It.IsAny<ResizeMode>(), It.IsAny<WindowStyle>(),
                    It.IsAny<ICommand>()))
                .Returns(dispatcherOperationWrapperMock.Object);

            var wrapper = new FlatWizardFilterSettings {
                FlatWizardMode = FlatWizardMode.DYNAMICBRIGHTNESS,
                MinFlatExposureTime = 1d,
                FlatDeviceStepSize = 10d,
                MinFlatDeviceBrightness = 10d,
                MaxFlatDeviceBrightness = 100d
            };

            var result = await sut.FindFlatDeviceBrightness(new PauseToken(), new FlatWizardFilterSettingsWrapper(new FilterInfo(),
                wrapper, 16,
                new CameraInfo(), flatDeviceInfo));

            result.Should().Be(10d);
            flatDeviceMediatorMock.Verify(m => m.SetBrightness(It.IsAny<double>(), It.IsAny<CancellationToken>()), Times.Exactly(7));
        }

        [Test]
        public async Task TestFindFlatDeviceBrightnessAskUserAfterFiveStepsAboveUpperBoundAndResetAndContinueSuccess() {
            sut = new FlatWizardVM(profileServiceMock.Object, imagingVMMock.Object, errorDialogMock.Object,
                cameraMediatorMock.Object, filterWheelMediatorMock.Object, telescopeMediatorMock.Object,
                flatDeviceMediatorMock.Object, imageGeometryProviderMock.Object,
                applicationStatusMediatorMock.Object, messageBoxVMMock.Object, twilightCalculatorMock.Object) { WindowService = windowServiceMock.Object };
            sut.UpdateDeviceInfo(flatDeviceInfo);

            exposureMock.SetupSequence(m => m.ToImageData(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .Returns(GetMeanImage(5000))
                .Returns(GetMeanImage(50000))
                .Returns(GetMeanImage(48000))
                .Returns(GetMeanImage(48000))
                .Returns(GetMeanImage(48000))
                .Returns(GetMeanImage(48000))
                .Returns(GetMeanImage(32768));

            errorDialogMock.Setup(m => m.Result).Returns(FlatWizardUserPromptResult.ResetAndContinue);

            windowServiceMock.Setup(m => m.ShowDialog(It.IsAny<object>(), It.IsAny<string>(),
                    It.IsAny<ResizeMode>(), It.IsAny<WindowStyle>(),
                    It.IsAny<ICommand>()))
                .Returns(dispatcherOperationWrapperMock.Object);

            var wrapper = new FlatWizardFilterSettings {
                FlatWizardMode = FlatWizardMode.DYNAMICBRIGHTNESS,
                MinFlatExposureTime = 1d,
                FlatDeviceStepSize = 10d,
                MinFlatDeviceBrightness = 10d,
                MaxFlatDeviceBrightness = 100d
            };

            var result = await sut.FindFlatDeviceBrightness(new PauseToken(), new FlatWizardFilterSettingsWrapper(new FilterInfo(),
                wrapper, 16,
                new CameraInfo(), flatDeviceInfo));

            result.Should().Be(10d);
            flatDeviceMediatorMock.Verify(m => m.SetBrightness(It.IsAny<double>(), It.IsAny<CancellationToken>()), Times.Exactly(7));
        }

        [Test]
        public async Task TestFindFlatDeviceBrightnessFlatDeviceNotConnected() {
            sut = new FlatWizardVM(profileServiceMock.Object, imagingVMMock.Object, errorDialogMock.Object,
                cameraMediatorMock.Object, filterWheelMediatorMock.Object, telescopeMediatorMock.Object,
                flatDeviceMediatorMock.Object, imageGeometryProviderMock.Object,
                applicationStatusMediatorMock.Object, messageBoxVMMock.Object, twilightCalculatorMock.Object) { WindowService = windowServiceMock.Object };
            flatDeviceInfo.Connected = false;
            sut.UpdateDeviceInfo(flatDeviceInfo);

            var wrapper = new FlatWizardFilterSettings {
                FlatWizardMode = FlatWizardMode.DYNAMICBRIGHTNESS,
                MinFlatExposureTime = 1d,
                FlatDeviceStepSize = 10d,
                MinFlatDeviceBrightness = 10d,
                MaxFlatDeviceBrightness = 100d
            };

            Func<Task> act = async () => {
                await sut.FindFlatDeviceBrightness(new PauseToken(), new FlatWizardFilterSettingsWrapper(
                    new FilterInfo(),
                    wrapper, 16, new CameraInfo(), flatDeviceInfo));
            };
            await act.Should().ThrowAsync<Exception>();

            flatDeviceMediatorMock.Verify(m => m.SetBrightness(It.IsAny<double>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        [TestCase(FlatWizardMode.DYNAMICBRIGHTNESS)]
        [TestCase(FlatWizardMode.DYNAMICEXPOSURE)]
        public async Task TestFlatMagicDynamic(FlatWizardMode mode) {
            var settings = new FlatWizardFilterSettingsWrapper(new FilterInfo(),
                new FlatWizardFilterSettings {
                    FlatWizardMode = mode,
                    MinFlatExposureTime = 1d,
                    HistogramMeanTarget = 32768,
                    HistogramTolerance = 2000,
                    StepSize = 1d,
                    MaxFlatDeviceBrightness = 100d
                }, 16,
                new CameraInfo(), flatDeviceInfo);

            sut = new FlatWizardVM(profileServiceMock.Object, imagingVMMock.Object, errorDialogMock.Object,
                cameraMediatorMock.Object, filterWheelMediatorMock.Object, telescopeMediatorMock.Object,
                flatDeviceMediatorMock.Object, imageGeometryProviderMock.Object,
                applicationStatusMediatorMock.Object, messageBoxVMMock.Object, twilightCalculatorMock.Object) {
                SingleFlatWizardFilterSettings = settings,
                FlatCount = 1,
                DarkFlatCount = 1
            };
            sut.UpdateDeviceInfo(flatDeviceInfo);

            messageBoxVMMock.Setup(m => m.Show(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<MessageBoxButton>(), It.IsAny<MessageBoxResult>())).Returns(MessageBoxResult.OK);
            exposureMock.SetupSequence(m => m.ToImageData(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .Returns(GetMeanImage(32768))
                .Returns(GetMeanImage(32768))
                .Returns(GetMeanImage(32768));

            var result = await sut.StartFlatMagic(new List<FlatWizardFilterSettingsWrapper> { settings }, new PauseToken());

            result.Should().BeTrue();
            flatDeviceMediatorMock.Verify(m => m.SetBrightness(It.IsAny<double>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            profileMock.Verify(m => m.FlatDeviceSettings.AddBrightnessInfo(It.IsAny<FlatDeviceFilterSettingsKey>(), It.IsAny<FlatDeviceFilterSettingsValue>()), Times.Once);
            imagingVMMock.Verify(m => m.CaptureImage(It.IsAny<CaptureSequence>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<String>()),
                Times.Exactly(3));
        }

        [Test]
        [TestCase(FlatWizardMode.DYNAMICBRIGHTNESS)]
        [TestCase(FlatWizardMode.DYNAMICEXPOSURE)]
        public async Task TestFlatMagicDynamicFlatDeviceDiesOnSettingBrightness(FlatWizardMode mode) {
            var settings = new FlatWizardFilterSettingsWrapper(new FilterInfo(),
                new FlatWizardFilterSettings {
                    FlatWizardMode = mode,
                    MinFlatExposureTime = 1d,
                    HistogramMeanTarget = 32768,
                    HistogramTolerance = 2000,
                    StepSize = 1d,
                    MaxFlatDeviceBrightness = 100d
                }, 16,
                new CameraInfo(), flatDeviceInfo);
            flatDeviceMediatorMock.Setup(m => m.SetBrightness(It.IsAny<double>(), It.IsAny<CancellationToken>()))
                .Throws<InvalidDeviceResponseException>();

            sut = new FlatWizardVM(profileServiceMock.Object, imagingVMMock.Object, errorDialogMock.Object,
                cameraMediatorMock.Object, filterWheelMediatorMock.Object, telescopeMediatorMock.Object,
                flatDeviceMediatorMock.Object, imageGeometryProviderMock.Object,
                applicationStatusMediatorMock.Object, messageBoxVMMock.Object, twilightCalculatorMock.Object) {
                SingleFlatWizardFilterSettings = settings,
                FlatCount = 1,
                DarkFlatCount = 1
            };
            sut.UpdateDeviceInfo(flatDeviceInfo);

            var result = await sut.StartFlatMagic(new List<FlatWizardFilterSettingsWrapper> { settings }, new PauseToken());

            result.Should().BeFalse();
            flatDeviceMediatorMock.Verify(m => m.SetBrightness(It.IsAny<double>(), It.IsAny<CancellationToken>()), Times.Once);
            profileMock.Verify(m => m.FlatDeviceSettings.AddBrightnessInfo(It.IsAny<FlatDeviceFilterSettingsKey>(), It.IsAny<FlatDeviceFilterSettingsValue>()), Times.Never);
            imagingVMMock.Verify(m => m.CaptureImage(It.IsAny<CaptureSequence>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<String>()),
                Times.Never);
        }

        [Test]
        public async Task TestFlatMagicSkyflats() {
            var settings = new FlatWizardFilterSettingsWrapper(new FilterInfo(),
                new FlatWizardFilterSettings {
                    FlatWizardMode = FlatWizardMode.SKYFLAT,
                    MinFlatExposureTime = 1d,
                    HistogramMeanTarget = 32768,
                    HistogramTolerance = 2000,
                    StepSize = 1d,
                    MaxFlatDeviceBrightness = 100d
                }, 16,
                new CameraInfo(), null);

            twilightCalculatorMock.SetupSequence(m =>
                    m.GetTwilightDuration(It.IsAny<DateTime>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(TimeSpan.FromMinutes(80))
                .Returns(TimeSpan.FromMinutes(90));

            sut = new FlatWizardVM(profileServiceMock.Object, imagingVMMock.Object, errorDialogMock.Object,
                cameraMediatorMock.Object, filterWheelMediatorMock.Object, telescopeMediatorMock.Object,
                flatDeviceMediatorMock.Object, imageGeometryProviderMock.Object,
                applicationStatusMediatorMock.Object, messageBoxVMMock.Object, twilightCalculatorMock.Object) {
                SingleFlatWizardFilterSettings = settings,
                FlatCount = 1,
                DarkFlatCount = 1
            };

            messageBoxVMMock.Setup(m => m.Show(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<MessageBoxButton>(), It.IsAny<MessageBoxResult>())).Returns(MessageBoxResult.OK);
            exposureMock.SetupSequence(m => m.ToImageData(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .Returns(GetMeanImage(32768))
                .Returns(GetMeanImage(32768))
                .Returns(GetMeanImage(32768));

            var result = await sut.StartFlatMagic(new List<FlatWizardFilterSettingsWrapper> { settings }, new PauseToken());

            result.Should().BeTrue();
            flatDeviceMediatorMock.Verify(m => m.SetBrightness(It.IsAny<double>(), It.IsAny<CancellationToken>()), Times.Never);
            profileMock.Verify(m => m.FlatDeviceSettings.AddBrightnessInfo(It.IsAny<FlatDeviceFilterSettingsKey>(), It.IsAny<FlatDeviceFilterSettingsValue>()), Times.Never);
            imagingVMMock.Verify(m => m.CaptureImage(It.IsAny<CaptureSequence>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<String>()),
                Times.Exactly(3));
        }
    }
}