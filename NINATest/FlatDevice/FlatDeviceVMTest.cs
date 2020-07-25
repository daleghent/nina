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
using NINA.Locale;
using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Model.MyFlatDevice;
using NINA.Profile;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel;
using NINA.ViewModel.Equipment;
using NINA.ViewModel.Equipment.FlatDevice;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace NINATest.FlatDevice {

    [TestFixture]
    public class FlatDeviceVMTest {
        private FlatDeviceVM sut;
        private Mock<IProfileService> mockProfileService;
        private Mock<IFlatDeviceMediator> mockFlatDeviceMediator;
        private Mock<IApplicationStatusMediator> mockApplicationStatusMediator;
        private Mock<IFlatDevice> mockFlatDevice;
        private Mock<IDeviceChooserVM> mockFlatDeviceChooserVM;
        private Mock<IFlatDeviceSettings> mockFlatDeviceSettings;
        private Mock<IFilterWheelSettings> mockFilterWheelSettings;
        private Mock<ICameraMediator> mockCameraMediator;
        private Mock<IImageGeometryProvider> mockImageGeometryProvider;

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            mockProfileService = new Mock<IProfileService>();
            mockFlatDeviceMediator = new Mock<IFlatDeviceMediator>();
            mockApplicationStatusMediator = new Mock<IApplicationStatusMediator>();
            mockFlatDevice = new Mock<IFlatDevice>();
            mockFlatDeviceChooserVM = new Mock<IDeviceChooserVM>();
            mockFlatDeviceSettings = new Mock<IFlatDeviceSettings>();
            mockFilterWheelSettings = new Mock<IFilterWheelSettings>();
            mockCameraMediator = new Mock<ICameraMediator>();
            mockImageGeometryProvider = new Mock<IImageGeometryProvider>();
        }

        [SetUp]
        public void Init() {
            mockProfileService.Reset();
            mockFlatDeviceMediator.Reset();
            mockApplicationStatusMediator.Reset();
            mockFlatDevice.Reset();
            mockFlatDeviceChooserVM.Reset();
            mockFlatDeviceSettings.Reset();
            mockFilterWheelSettings.Reset();
            mockCameraMediator.Reset();
            mockImageGeometryProvider.Reset();

            mockFlatDeviceSettings.SetupProperty(m => m.Id, "mockDevice");

            mockProfileService.SetupProperty(m => m.ActiveProfile.FlatDeviceSettings, mockFlatDeviceSettings.Object);
            mockFilterWheelSettings.Setup(m => m.FilterWheelFilters).Returns(new ObserveAllCollection<FilterInfo>());
            mockProfileService.SetupProperty(m => m.ActiveProfile.FilterWheelSettings, mockFilterWheelSettings.Object);
            mockProfileService.Setup(m => m.ActiveProfile.ApplicationSettings.DevicePollingInterval).Returns(200);
            sut = new FlatDeviceVM(mockProfileService.Object, mockFlatDeviceMediator.Object,
                mockApplicationStatusMediator.Object, mockCameraMediator.Object, mockFlatDeviceChooserVM.Object, mockImageGeometryProvider.Object);
        }

        [Test]
        public async Task TestOpenCoverNullFlatDevice() {
            mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, null);
            (await sut.OpenCover()).Should().BeFalse();
        }

        [Test]
        public async Task TestOpenCoverNotConnectedFlatDevice() {
            mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, mockFlatDevice.Object);
            mockFlatDevice.Setup(m => m.Connected).Returns(false);
            (await sut.OpenCover()).Should().BeFalse();
        }

        [Test]
        public async Task TestOpenCoverOpenCloseNotSupported() {
            mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, mockFlatDevice.Object);
            mockFlatDevice.Setup(m => m.Connected).Returns(true);
            mockFlatDevice.Setup(m => m.SupportsOpenClose).Returns(false);
            (await sut.OpenCover()).Should().BeFalse();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task TestOpenCoverSuccess(bool expected) {
            mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, mockFlatDevice.Object);
            mockFlatDevice.Setup(m => m.Id).Returns("Something");
            mockFlatDevice.Setup(m => m.Connected).Returns(true);
            mockFlatDevice.Setup(m => m.SupportsOpenClose).Returns(true);
            mockFlatDevice.Setup(m => m.Open(It.IsAny<CancellationToken>(), It.IsAny<int>())).Returns(Task.FromResult(expected));
            (await sut.OpenCover()).Should().Be(expected);
        }

        [Test]
        public async Task TestOpenCoverCancelled() {
            mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, mockFlatDevice.Object);
            mockFlatDevice.Setup(m => m.Id).Returns("Something");
            mockFlatDevice.Setup(m => m.Connected).Returns(true);
            mockFlatDevice.Setup(m => m.SupportsOpenClose).Returns(true);
            mockFlatDevice.Setup(m => m.Open(It.IsAny<CancellationToken>(), It.IsAny<int>()))
                .Callback((CancellationToken ct, int delay) => throw new OperationCanceledException());
            (await sut.OpenCover()).Should().BeFalse();
        }

        [Test]
        public async Task TestCloseCoverNullFlatDevice() {
            mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, null);
            (await sut.CloseCover()).Should().BeFalse();
        }

        [Test]
        public async Task TestCloseCoverNotConnectedFlatDevice() {
            mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, mockFlatDevice.Object);
            mockFlatDevice.Setup(m => m.Connected).Returns(false);
            (await sut.CloseCover()).Should().BeFalse();
        }

        [Test]
        public async Task TestCloseCoverOpenCloseNotSupported() {
            mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, mockFlatDevice.Object);
            mockFlatDevice.Setup(m => m.Connected).Returns(true);
            mockFlatDevice.Setup(m => m.SupportsOpenClose).Returns(false);
            (await sut.CloseCover()).Should().BeFalse();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task TestCloseCoverSuccess(bool expected) {
            mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, mockFlatDevice.Object);
            mockFlatDevice.Setup(m => m.Id).Returns("Something");
            mockFlatDevice.Setup(m => m.Connected).Returns(true);
            mockFlatDevice.Setup(m => m.SupportsOpenClose).Returns(true);
            mockFlatDevice.Setup(m => m.Close(It.IsAny<CancellationToken>(), It.IsAny<int>())).Returns(Task.FromResult(expected));
            (await sut.CloseCover()).Should().Be(expected);
        }

        [Test]
        public async Task TestCloseCoverCancelled() {
            mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, mockFlatDevice.Object);
            mockFlatDevice.Setup(m => m.Id).Returns("Something");
            mockFlatDevice.Setup(m => m.Connected).Returns(true);
            mockFlatDevice.Setup(m => m.SupportsOpenClose).Returns(true);
            mockFlatDevice.Setup(m => m.Close(It.IsAny<CancellationToken>(), It.IsAny<int>()))
                .Callback((CancellationToken ct, int delay) => throw new OperationCanceledException());
            (await sut.CloseCover()).Should().BeFalse();
        }

        [Test]
        public async Task TestConnectNullDevice() {
            mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, null);
            (await sut.Connect()).Should().BeFalse();
        }

        [Test]
        public async Task TestConnectDummyDevice() {
            mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, mockFlatDevice.Object);
            mockFlatDevice.Setup(m => m.Id).Returns("No_Device");
            (await sut.Connect()).Should().BeFalse();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task TestConnectSuccess(bool expected) {
            mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, mockFlatDevice.Object);
            mockFlatDevice.Setup(m => m.Id).Returns("Something");
            mockFlatDevice.Setup(m => m.Connect(It.IsAny<CancellationToken>())).Returns(Task.FromResult(expected));
            (await sut.Connect()).Should().Be(expected);
        }

        [Test]
        public async Task TestConnectCancelled() {
            mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, mockFlatDevice.Object);
            mockFlatDevice.Setup(m => m.Id).Returns("Something");
            mockFlatDevice.Setup(m => m.Connect(It.IsAny<CancellationToken>()))
                .Callback((CancellationToken ct) => throw new OperationCanceledException());
            (await sut.Connect()).Should().BeFalse();
        }

        [Test]
        public void TestWizardGridWithoutData() {
            var result = sut.WizardGrid;
            result.Should().NotBeNull();
            result.Blocks.Count.Should().Be(0);
        }

        [Test]
        public void TestWizardGridWithoutFilterWheel() {
            const int gain = 30;
            var binningMode = new BinningMode(1, 1);
            var settingsValue = new FlatDeviceFilterSettingsValue(0.7, 0.5);
            var settingsKey = new FlatDeviceFilterSettingsKey(null, binningMode, gain);
            mockProfileService
                        .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoBinnings())
                        .Returns(new List<BinningMode> { binningMode });
            mockProfileService
                    .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoGains())
                    .Returns(new List<int> { gain });
            mockProfileService
                    .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfo(
                        It.IsAny<FlatDeviceFilterSettingsKey>())).Returns(settingsValue);
            mockFilterWheelSettings.Raise(m => m.PropertyChanged += null,
                    new PropertyChangedEventArgs("FilterWheelFilters"));
            var result = sut.WizardGrid;
            result.Should().NotBeNull();
            result.Blocks.Count.Should().Be(1);
            result.Blocks[0].Columns.Count.Should().Be(2);
            result.Blocks[0].Binning.Should().Be(binningMode);
            result.Blocks[0].Columns[0].Header.Should().Be(Loc.Instance["LblFilter"]);
            result.Blocks[0].Columns[0].Settings[0].ShowFilterNameOnly.Should().BeTrue();
            result.Blocks[0].Columns[0].Settings[0].Key.Position.Should().BeNull();
            result.Blocks[0].Columns[1].Header.Should().BeNull();
            result.Blocks[0].Columns[1].Settings[0].ShowFilterNameOnly.Should().BeFalse();
            result.Blocks[0].Columns[1].Settings[0].Key.Should().Be(settingsKey);
            result.Blocks[0].Columns[1].Settings[0].Brightness.Should().Be(settingsValue.Brightness);
            result.Blocks[0].Columns[1].Settings[0].Time.Should().Be(settingsValue.Time);
        }

        [Test]
        public void TestWizardGridWithFilters() {
            const int gain = 30;
            const short position = 2;
            var binningMode = new BinningMode(1, 1);
            var settingsValue = new FlatDeviceFilterSettingsValue(0.7, 0.5);
            var settingsKey = new FlatDeviceFilterSettingsKey(position, binningMode, gain);

            mockProfileService
                .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfo(
                    It.IsAny<FlatDeviceFilterSettingsKey>())).Returns(settingsValue);
            mockProfileService
                .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoBinnings())
                .Returns(new List<BinningMode> { binningMode });
            mockProfileService
                .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoGains())
                                .Returns(new List<int> { gain });
            mockFilterWheelSettings.Setup(m => m.FilterWheelFilters)
                    .Returns(new ObserveAllCollection<FilterInfo> { new FilterInfo { Position = position } });
            mockFilterWheelSettings.Raise(m => m.PropertyChanged += null,
                    new PropertyChangedEventArgs("FilterWheelFilters"));
            var result = sut.WizardGrid;
            result.Should().NotBeNull();
            result.Blocks.Count.Should().Be(1);
            result.Blocks[0].Columns.Count.Should().Be(2);
            result.Blocks[0].Binning.Should().Be(binningMode);
            result.Blocks[0].Columns[0].Header.Should().Be(Loc.Instance["LblFilter"]);
            result.Blocks[0].Columns[0].Settings[0].ShowFilterNameOnly.Should().BeTrue();
            result.Blocks[0].Columns[0].Settings[0].Key.Position.Should().Be(position);
            result.Blocks[0].Columns[1].Header.Should().BeNull();
            result.Blocks[0].Columns[1].Settings[0].ShowFilterNameOnly.Should().BeFalse();
            result.Blocks[0].Columns[1].Settings[0].Key.Should().Be(settingsKey);
            result.Blocks[0].Columns[1].Settings[0].Brightness.Should().Be(settingsValue.Brightness);
            result.Blocks[0].Columns[1].Settings[0].Time.Should().Be(settingsValue.Time);
        }

        [Test]
        public void TestWizardGridMustChangeWithNewProfile() {
            const int gain = 30;
            const short position = 2;
            var binningMode = new BinningMode(1, 1);
            var settingsValue = new FlatDeviceFilterSettingsValue(0.7, 0.5);

            mockProfileService
                    .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfo(
                        It.IsAny<FlatDeviceFilterSettingsKey>())).Returns(settingsValue);
            mockProfileService
                    .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoBinnings())
                    .Returns(new List<BinningMode> { binningMode });
            mockProfileService
                    .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoGains())
                    .Returns(new List<int> { gain });
            mockFilterWheelSettings.Setup(m => m.FilterWheelFilters)
                    .Returns(new ObserveAllCollection<FilterInfo> { new FilterInfo { Position = position } });
            mockFilterWheelSettings.Raise(m => m.PropertyChanged += null,
                    new PropertyChangedEventArgs("FilterWheelFilters"));

            var result1 = sut.WizardGrid;
            result1.Should().NotBeNull();
            result1.Blocks.Count.Should().Be(1);
            result1.Blocks[0].Columns[0].Settings[0].Key.Position.Should().Be(position);

            mockFilterWheelSettings.Setup(m => m.FilterWheelFilters)
            .Returns(new ObserveAllCollection<FilterInfo> { null });
            mockFilterWheelSettings.Raise(m => m.PropertyChanged += null,
                    new PropertyChangedEventArgs("FilterWheelFilters"));

            var result2 = sut.WizardGrid;
            result2.Should().NotBeNull();
            result2.Blocks.Count.Should().Be(1);
            result2.Blocks[0].Columns[0].Settings[0].Key.Position.Should().BeNull();
        }

        [Test]
        public void TestWizardGridForCamerasWithoutBinning() {
            const int gain = 30;
            const short position = 2;
            var settingsValue = new FlatDeviceFilterSettingsValue(0.7, 0.5);
            var settingsKey = new FlatDeviceFilterSettingsKey(position, null, gain);

            mockProfileService
                    .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfo(
                        It.IsAny<FlatDeviceFilterSettingsKey>())).Returns(settingsValue);
            mockProfileService
                    .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoBinnings())
                    .Returns(new List<BinningMode> { null });
            mockProfileService
                    .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoGains())
                    .Returns(new List<int> { gain });
            mockFilterWheelSettings.Setup(m => m.FilterWheelFilters)
                    .Returns(new ObserveAllCollection<FilterInfo> { new FilterInfo { Position = position } });
            mockFilterWheelSettings.Raise(m => m.PropertyChanged += null,
                    new PropertyChangedEventArgs("FilterWheelFilters"));

            var result = sut.WizardGrid;

            result.Should().NotBeNull();
            result.Blocks.Count.Should().Be(1);
            result.Blocks[0].Columns.Count.Should().Be(2);
            result.Blocks[0].Binning.Should().BeNull();
            result.Blocks[0].Columns[0].Header.Should().Be(Loc.Instance["LblFilter"]);
            result.Blocks[0].Columns[0].Settings[0].ShowFilterNameOnly.Should().BeTrue();
            result.Blocks[0].Columns[0].Settings[0].Key.Position.Should().Be(position);
            result.Blocks[0].Columns[1].Header.Should().BeNull();
            result.Blocks[0].Columns[1].Settings[0].ShowFilterNameOnly.Should().BeFalse();
            result.Blocks[0].Columns[1].Settings[0].Key.Should().Be(settingsKey);
            result.Blocks[0].Columns[1].Settings[0].Brightness.Should().Be(settingsValue.Brightness);
            result.Blocks[0].Columns[1].Settings[0].Time.Should().Be(settingsValue.Time);
        }

        [Test]
        public void TestSetBrightnessNullFlatDevice() {
            mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, null);
            sut.SetBrightness(1.0);
            sut.Brightness.Should().Be(0d);
            mockFlatDevice.Verify(m => m.Brightness, Times.Never);
        }

        [Test]
        public async Task TestSetBrightnessConnectedFlatDeviceAsync() {
            mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, mockFlatDevice.Object);
            mockFlatDevice.Setup(m => m.Id).Returns("Something");
            mockFlatDevice.Setup(m => m.Connected).Returns(true);
            mockFlatDevice.Setup(m => m.Connect(It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            await sut.Connect();
            sut.SetBrightness(1.0);
            sut.Brightness.Should().Be(0d);
            mockFlatDevice.VerifySet(m => m.Brightness = 1d, Times.Once);
        }

        [Test]
        public void TestToggleLightNullFlatDevice() {
            mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, null);
            sut.ToggleLight(true);
            sut.LightOn.Should().BeFalse();
            mockFlatDevice.Verify(m => m.LightOn, Times.Never);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task TestToggleLightConnected(bool expected) {
            mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, mockFlatDevice.Object);
            mockFlatDevice.Setup(m => m.Id).Returns("Something");
            mockFlatDevice.Setup(m => m.Connected).Returns(true);
            mockFlatDevice.Setup(m => m.Connect(It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            await sut.Connect();
            sut.ToggleLight(expected);
            mockFlatDevice.VerifySet(m => m.LightOn = expected, Times.Once);
        }

        [Test]
        public void TestBinningModesReturnsCorrectName() {
            var binningMode = new BinningMode(1, 1);

            mockProfileService
            .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoBinnings())
            .Returns(new List<BinningMode> { binningMode });
            var result = sut.BinningModes;

            result.Count.Should().Be(1);
            result.Should().BeEquivalentTo(new List<string> { binningMode.Name });
        }

        [Test]
        public void TestBinningModesReturnsCorrectNameForNullBinningMode() {
            mockProfileService
                    .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoBinnings())
                    .Returns(new List<BinningMode> { null });
            var result = sut.BinningModes;

            result.Count.Should().Be(1);
            result.Should().BeEquivalentTo(new List<string> { Loc.Instance["LblNone"] });
        }

        [Test]
        public void TestAddGain() {
            var result = string.Empty;
            mockFlatDeviceSettings.Setup(m =>
                            m.AddBrightnessInfo(It.IsAny<FlatDeviceFilterSettingsKey>(), It.IsAny<FlatDeviceFilterSettingsValue>()))
                    .Callback((FlatDeviceFilterSettingsKey key, FlatDeviceFilterSettingsValue value) => { result = key.Gain.ToString(); });
            mockProfileService.Setup(m => m.ActiveProfile.FlatDeviceSettings).Returns(mockFlatDeviceSettings.Object);
            sut.AddGainCommand.Execute("25");

            mockFlatDeviceSettings.Verify(m =>
                    m.AddBrightnessInfo(It.IsAny<FlatDeviceFilterSettingsKey>(), It.IsAny<FlatDeviceFilterSettingsValue>()), Times.Once);
            result.Should().Be("25");
        }

        [Test]
        public void TestAddBinning() {
            BinningMode result = null;
            var binning = new BinningMode(1, 1);
            mockFlatDeviceSettings.Setup(m =>
                            m.AddBrightnessInfo(It.IsAny<FlatDeviceFilterSettingsKey>(), It.IsAny<FlatDeviceFilterSettingsValue>()))
                    .Callback((FlatDeviceFilterSettingsKey key, FlatDeviceFilterSettingsValue value) => { result = key.Binning; });
            mockProfileService.Setup(m => m.ActiveProfile.FlatDeviceSettings).Returns(mockFlatDeviceSettings.Object);
            sut.AddBinningCommand.Execute(binning);

            mockFlatDeviceSettings.Verify(m =>
                    m.AddBrightnessInfo(It.IsAny<FlatDeviceFilterSettingsKey>(), It.IsAny<FlatDeviceFilterSettingsValue>()), Times.Once);
            result.Should().Be(binning);
        }
    }
}