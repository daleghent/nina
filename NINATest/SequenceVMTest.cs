#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Moq;
using NINA;
using NINA.Model;
using NINA.Model.MyFlatDevice;
using NINA.Model.MyGuider;
using NINA.Model.MyPlanetarium;
using NINA.Model.MyTelescope;
using NINA.Profile;
using NINA.Utility.Astrometry;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel;
using NINA.ViewModel.Equipment.FlatDevice;
using NINA.ViewModel.ImageHistory;
using NINA.ViewModel.FramingAssistant;
using NINA.ViewModel.Interfaces;
using NINA.ViewModel.Sequencer;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace NINATest {

    [TestFixture]
    public class SequenceVMTest {
        public TestContext TestContext { get; set; }

        private Mock<IProfileService> profileServiceMock;
        private Mock<ICameraMediator> cameraMediatorMock;
        private Mock<ITelescopeMediator> telescopeMediatorMock;
        private Mock<IFocuserMediator> focuserMediatorMock;
        private Mock<IFilterWheelMediator> filterWheelMediatorMock;
        private Mock<IGuiderMediator> guiderMediatorMock;
        private Mock<IRotatorMediator> rotatorMediatorMock;
        private Mock<IWeatherDataMediator> weatherDataMediatorMock;
        private Mock<IImagingMediator> imagingMediatorMock;
        private Mock<IApplicationStatusMediator> applicationStatusMediatorMock;
        private Mock<IFlatDeviceMediator> flatDeviceMediatorMock;
        private Mock<INighttimeCalculator> nighttimeCalculatorMock;
        private Mock<IPlanetariumFactory> planetariumFactoryMock;
        private Mock<IImageHistoryVM> imageHistoryVMMock;
        private Mock<IDeepSkyObjectSearchVM> deepSkyObjectSearchVMMock;
        private Mock<IDomeMediator> domeMediatorMock;
        private Mock<IImageSaveMediator> imageSaveMediatorMock;
        private Mock<IApplicationMediator> applicationMediatorMock;
        private Mock<IFramingAssistantVM> framingAssistantVMMock;
        private FlatDeviceInfo _flatDevice;
        private CaptureSequenceList _dummyList;
        private Mock<ISequenceMediator> sequenceMediatorMock;
        private SequenceVM _sut;

        [SetUp]
        public void SequenceVM_TestInit() {
            profileServiceMock = new Mock<IProfileService>();
            profileServiceMock.SetupProperty(m => m.ActiveProfile.ImageFileSettings.FilePath, TestContext.CurrentContext.TestDirectory);
            profileServiceMock.SetupProperty(m => m.ActiveProfile.SequenceSettings.TemplatePath, TestContext.CurrentContext.TestDirectory);
            profileServiceMock.SetupProperty(m => m.ActiveProfile.AstrometrySettings.Longitude, 0);
            profileServiceMock.SetupProperty(m => m.ActiveProfile.AstrometrySettings.Latitude, 0);
            profileServiceMock.SetupProperty(m => m.ActiveProfile.FocuserSettings.AutoFocusDisableGuiding, true);

            cameraMediatorMock = new Mock<ICameraMediator>();
            telescopeMediatorMock = new Mock<ITelescopeMediator>();
            focuserMediatorMock = new Mock<IFocuserMediator>();
            filterWheelMediatorMock = new Mock<IFilterWheelMediator>();
            guiderMediatorMock = new Mock<IGuiderMediator>();
            rotatorMediatorMock = new Mock<IRotatorMediator>();
            flatDeviceMediatorMock = new Mock<IFlatDeviceMediator>();
            weatherDataMediatorMock = new Mock<IWeatherDataMediator>();
            imagingMediatorMock = new Mock<IImagingMediator>();
            applicationStatusMediatorMock = new Mock<IApplicationStatusMediator>();
            nighttimeCalculatorMock = new Mock<INighttimeCalculator>();
            planetariumFactoryMock = new Mock<IPlanetariumFactory>();
            imageHistoryVMMock = new Mock<IImageHistoryVM>();
            deepSkyObjectSearchVMMock = new Mock<IDeepSkyObjectSearchVM>();
            domeMediatorMock = new Mock<IDomeMediator>();
            sequenceMediatorMock = new Mock<ISequenceMediator>();
            applicationMediatorMock = new Mock<IApplicationMediator>();
            framingAssistantVMMock = new Mock<IFramingAssistantVM>();

            _dummyList = new CaptureSequenceList();
            _dummyList.Add(new CaptureSequence() { TotalExposureCount = 10 });
            _dummyList.Add(new CaptureSequence() { TotalExposureCount = 20 });
            _dummyList.Add(new CaptureSequence() { TotalExposureCount = 5 });

            _flatDevice = new FlatDeviceInfo() {
                Brightness = 1.0,
                Connected = true,
                CoverState = CoverState.Open,
                Description = "Some description",
                DriverInfo = "Some driverInfo",
                LightOn = false,
                DriverVersion = "200",
                MaxBrightness = 255,
                MinBrightness = 0,
                Name = "Some name",
                SupportsOpenClose = true
            };

            _sut = new SequenceVM(profileServiceMock.Object, sequenceMediatorMock.Object, cameraMediatorMock.Object, rotatorMediatorMock.Object, applicationStatusMediatorMock.Object, nighttimeCalculatorMock.Object,
                planetariumFactoryMock.Object, deepSkyObjectSearchVMMock.Object, framingAssistantVMMock.Object, applicationMediatorMock.Object);
        }

        [TearDown]
        public void Cleanup() {
        }

        [Test]
        public void Sequence_ConsumerRegistered() {
            cameraMediatorMock.Verify(m => m.RegisterConsumer(_sut));
            rotatorMediatorMock.Verify(m => m.RegisterConsumer(_sut));
        }

        [Test]
        public void ProcessSequence_AddSequenceCommand() {
            //Act
            _sut.AddTarget(new DeepSkyObject("", ""));
            _sut.AddSequenceRowCommand.Execute(null);

            //Assert
            Assert.That(_sut.SelectedSequenceRowIdx, Is.EqualTo(1));
            Assert.That(_sut.Sequence.Count, Is.EqualTo(2));
        }

        [Test]
        public void ProcessSequence_OnEmptySequence_AddSequenceCommand() {
            //Arrange
            _sut.Sequence = new CaptureSequenceList();

            //Act
            _sut.AddSequenceRowCommand.Execute(null);

            //Assert
            Assert.That(_sut.SelectedSequenceRowIdx, Is.EqualTo(0));
            Assert.That(_sut.Sequence.Count, Is.EqualTo(1));
        }

        [Test]
        public void ProcessSequence_RemoveSequenceCommand() {
            //Act
            _sut.AddTarget(new DeepSkyObject("", ""));
            _sut.RemoveSequenceRowCommand.Execute(null);

            //Assert
            Assert.That(_sut.SelectedSequenceRowIdx, Is.EqualTo(-1));
            Assert.That(_sut.Sequence.Count, Is.EqualTo(0));
        }

        [Test]
        public void ProcessSequence_OnEmptySequence_RemoveSequenceCommand() {
            //Act
            _sut.AddTarget(new DeepSkyObject("", ""));
            _sut.RemoveSequenceRowCommand.Execute(null);
            _sut.RemoveSequenceRowCommand.Execute(null);

            //Assert
            Assert.That(_sut.SelectedSequenceRowIdx, Is.EqualTo(-1));
            Assert.That(_sut.Sequence.Count, Is.EqualTo(0));
        }
    }
}