using Moq;
using NINA.Model;
using NINA.Model.MyDome;
using NINA.Model.MyTelescope;
using NINA.Profile;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel.Equipment;
using NINA.ViewModel.Equipment.Dome;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NINATest.Dome {

    [TestFixture]
    public class DomeVMTest {
        private Mock<IProfileService> mockProfileService;
        private Mock<IDeviceChooserVM> mockDomeDeviceChooserVM;
        private Mock<IApplicationStatusMediator> mockApplicationStatusMediator;
        private Mock<IDomeMediator> mockDomeMediator;
        private Mock<ITelescopeMediator> mockTelescopeMediator;
        private Mock<IDomeSynchronization> mockDomeSynchronization;
        private Mock<IDeviceUpdateTimerFactory> mockDeviceUpdateTimerFactory;
        private Mock<IDeviceUpdateTimer> mockDeviceUpdateTimer;
        private Mock<IApplicationResourceDictionary> mockResourceDictionary;
        private Mock<IDome> mockDome;

        private string domeId;
        private bool domeConnected;
        private ShutterState domeShutterState;
        private bool domeDriverCanSlave;
        private bool domeCanSetShutter;
        private bool domeCanSetPark;
        private bool domeCanSetAzimuth;
        private bool domeCanSyncAzimuth;
        private bool domeCanPark;
        private bool domeCanFindHome;
        private double domeAzimuth;
        private Angle domeTargetAzimuth;
        private bool domeAtPark;
        private bool domeAtHome;
        private bool domeSlewing;
        private bool domeDriverSlaved;
        private double domeAzimuthToleranceDegrees;
        private Angle siteLatitude;
        private Angle siteLongitude;

        [SetUp]
        public void Init() {
            domeId = "ID";
            domeConnected = false;
            domeShutterState = ShutterState.ShutterOpen;
            domeDriverCanSlave = true;
            domeCanSetShutter = true;
            domeCanSetPark = true;
            domeCanSetAzimuth = true;
            domeCanSyncAzimuth = true;
            domeCanPark = true;
            domeCanFindHome = true;
            domeAzimuth = 0.0;
            domeTargetAzimuth = Angle.ByDegree(0.0);
            domeAtPark = true;
            domeAtHome = false;
            domeSlewing = false;
            domeDriverSlaved = false;
            domeAzimuthToleranceDegrees = 1.0;
            siteLatitude = Angle.ByDegree(41.5);
            siteLongitude = Angle.ByDegree(-23.2);

            mockProfileService = new Mock<IProfileService>();
            mockDomeDeviceChooserVM = new Mock<IDeviceChooserVM>();
            mockApplicationStatusMediator = new Mock<IApplicationStatusMediator>();
            mockDomeMediator = new Mock<IDomeMediator>();
            mockTelescopeMediator = new Mock<ITelescopeMediator>();
            mockDomeSynchronization = new Mock<IDomeSynchronization>();
            mockDomeSynchronization.Setup(x => x.TargetDomeAzimuth(
                It.IsAny<Coordinates>(),
                It.IsAny<double>(),
                It.IsAny<Angle>(),
                It.IsAny<Angle>(),
                It.IsAny<PierSide>())).Returns(() => domeTargetAzimuth);
            mockDeviceUpdateTimer = new Mock<IDeviceUpdateTimer>();
            mockDeviceUpdateTimerFactory = new Mock<IDeviceUpdateTimerFactory>();
            mockDeviceUpdateTimerFactory
                .Setup(x => x.Create(It.IsAny<Func<Dictionary<string, object>>>(), It.IsAny<Action<Dictionary<string, object>>>(), It.IsAny<double>()))
                .Returns(mockDeviceUpdateTimer.Object);
            mockResourceDictionary = new Mock<IApplicationResourceDictionary>();
            mockProfileService.SetupGet(p => p.ActiveProfile.ApplicationSettings.DevicePollingInterval).Returns(1);
            mockProfileService.SetupGet(p => p.ActiveProfile.DomeSettings.UseDirectFollowing).Returns(false);
            mockProfileService.SetupGet(p => p.ActiveProfile.DomeSettings.AzimuthTolerance_degrees).Returns(() => domeAzimuthToleranceDegrees);

            mockApplicationStatusMediator.Setup(x => x.StatusUpdate(It.IsAny<ApplicationStatus>()));
        }

        private async Task<DomeVM> CreateSUT() {
            var domeVM = new DomeVM(mockProfileService.Object, mockDomeMediator.Object, mockApplicationStatusMediator.Object, mockTelescopeMediator.Object,
                mockDomeDeviceChooserVM.Object, mockDomeSynchronization.Object, mockResourceDictionary.Object, mockDeviceUpdateTimerFactory.Object);

            mockDome = new Mock<IDome>();
            mockDome.SetupGet(x => x.Id).Returns(() => domeId);
            mockDome.SetupGet(x => x.Connected).Returns(() => domeConnected);
            mockDome.SetupGet(x => x.ShutterStatus).Returns(() => domeShutterState);
            mockDome.SetupGet(x => x.DriverCanFollow).Returns(() => domeDriverCanSlave);
            mockDome.SetupGet(x => x.CanSetShutter).Returns(() => domeCanSetShutter);
            mockDome.SetupGet(x => x.CanSetPark).Returns(() => domeCanSetPark);
            mockDome.SetupGet(x => x.CanSetAzimuth).Returns(() => domeCanSetAzimuth);
            mockDome.SetupGet(x => x.CanSyncAzimuth).Returns(() => domeCanSyncAzimuth);
            mockDome.SetupGet(x => x.CanPark).Returns(() => domeCanPark);
            mockDome.SetupGet(x => x.CanFindHome).Returns(() => domeCanFindHome);
            mockDome.SetupGet(x => x.Azimuth).Returns(() => domeAzimuth);
            mockDome.SetupGet(x => x.AtPark).Returns(() => domeAtPark);
            mockDome.SetupGet(x => x.AtHome).Returns(() => domeAtHome);
            mockDome.SetupGet(x => x.Slewing).Returns(() => domeSlewing);
            mockDome.SetupGet(x => x.DriverFollowing).Returns(() => domeDriverSlaved);
            mockDome.SetupSet(x => x.DriverFollowing = It.IsAny<bool>()).Callback<bool>(v => {
                if (!domeDriverCanSlave) {
                    throw new InvalidOperationException("Dome cannot slave");
                }
                domeDriverSlaved = v;
            });
            mockDome.Setup(x => x.Connect(It.IsAny<CancellationToken>())).Callback<CancellationToken>(ct => {
                domeConnected = true;
            }).ReturnsAsync(true);
            mockDomeDeviceChooserVM.SetupGet(x => x.SelectedDevice).Returns(mockDome.Object);

            var connectionResult = await domeVM.Connect();
            Assert.IsTrue(connectionResult);
            return domeVM;
        }

        [Test]
        public async Task Test_DomeSynchronize_ReceivesCorrectParameters() {
            var sut = await CreateSUT();
            sut.FollowEnabled = true;
            sut.DirectFollowToggled = true;
            var t1 = new TelescopeInfo() {
                Connected = true,
                SiteLatitude = siteLatitude.Degree,
                SiteLongitude = siteLongitude.Degree,
                SiderealTime = 11.2,
                SideOfPier = PierSide.pierEast,
                Coordinates = new Coordinates(1.0, 2.0, Epoch.J2000, Coordinates.RAType.Degrees)
            };
            sut.UpdateDeviceInfo(t1);
            mockDomeSynchronization.Verify(x => x.TargetDomeAzimuth(t1.Coordinates, t1.SiderealTime, siteLatitude, siteLongitude, t1.SideOfPier), Times.Once);
        }

        [Test]
        public async Task Test_SlavingDisabled_NoDomeSynchronization() {
            var sut = await CreateSUT();
            sut.FollowEnabled = false;
            sut.DirectFollowToggled = true;
            var t1 = new TelescopeInfo() { Connected = true };
            sut.UpdateDeviceInfo(t1);
            mockDomeSynchronization.Verify(x => x.TargetDomeAzimuth(It.IsAny<Coordinates>(), It.IsAny<double>(), It.IsAny<Angle>(), It.IsAny<Angle>(), It.IsAny<PierSide>()), Times.Never);
        }

        [Test]
        public async Task Test_DirectSlavingDisabled_NoDomeSynchronization() {
            var sut = await CreateSUT();
            sut.DirectFollowToggled = false;
            sut.FollowEnabled = true;
            var t1 = new TelescopeInfo() { Connected = true };
            sut.UpdateDeviceInfo(t1);
            mockDomeSynchronization.Verify(x => x.TargetDomeAzimuth(It.IsAny<Coordinates>(), It.IsAny<double>(), It.IsAny<Angle>(), It.IsAny<Angle>(), It.IsAny<PierSide>()), Times.Never);
        }

        [Test]
        public async Task Test_DomeSynchronize_TargetsOverrideCurrent() {
            var sut = await CreateSUT();
            sut.FollowEnabled = true;
            sut.DirectFollowToggled = true;
            var t1 = new TelescopeInfo() {
                Connected = true,
                SiteLatitude = siteLatitude.Degree,
                SiteLongitude = siteLongitude.Degree,
                SiderealTime = 11.2,
                SideOfPier = PierSide.pierWest,
                TargetSideOfPier = PierSide.pierEast,
                Coordinates = new Coordinates(1.0, 2.0, Epoch.J2000, Coordinates.RAType.Degrees),
                TargetCoordinates = new Coordinates(2.0, 3.0, Epoch.J2000, Coordinates.RAType.Degrees),
            };
            sut.UpdateDeviceInfo(t1);
            mockDomeSynchronization.Verify(x => x.TargetDomeAzimuth(t1.TargetCoordinates, t1.SiderealTime, siteLatitude, siteLongitude, t1.TargetSideOfPier.Value), Times.Once);

            var t2 = new TelescopeInfo() {
                Connected = true,
                SiteLatitude = siteLatitude.Degree,
                SiteLongitude = siteLongitude.Degree,
                SiderealTime = 11.2,
                TargetSideOfPier = PierSide.pierWest,
                SideOfPier = PierSide.pierEast,
                TargetCoordinates = new Coordinates(1.0, 2.0, Epoch.J2000, Coordinates.RAType.Degrees),
                Coordinates = new Coordinates(2.0, 3.0, Epoch.J2000, Coordinates.RAType.Degrees),
            };
            sut.UpdateDeviceInfo(t2);
            mockDomeSynchronization.Verify(x => x.TargetDomeAzimuth(t2.TargetCoordinates, t2.SiderealTime, siteLatitude, siteLongitude, t2.TargetSideOfPier.Value), Times.Once);
        }

        [Test]
        public async Task Test_DriverSlaved_Toggling() {
            var sut = await CreateSUT();
            Assert.AreEqual(false, mockDome.Object.DriverFollowing);

            sut.DirectFollowToggled = true;
            mockDome.VerifySet(x => x.DriverFollowing = false, Times.Once);

            sut.FollowEnabled = true;
            mockDome.VerifySet(x => x.DriverFollowing = false, Times.Exactly(2));

            sut.DirectFollowToggled = false;
            mockDome.VerifySet(x => x.DriverFollowing = true, Times.Once);

            sut.FollowEnabled = false;
            mockDome.VerifySet(x => x.DriverFollowing = false, Times.Exactly(3));
        }

        [Test]
        public async Task Test_DriverCanSlaveDisabled_NoUpdates() {
            domeDriverCanSlave = false;
            var sut = await CreateSUT();
            Assert.AreEqual(false, mockDome.Object.DriverFollowing);

            sut.DirectFollowToggled = true;
            mockDome.VerifySet(x => x.DriverFollowing = It.IsAny<bool>(), Times.Never);

            sut.FollowEnabled = true;
            mockDome.VerifySet(x => x.DriverFollowing = It.IsAny<bool>(), Times.Never);

            sut.DirectFollowToggled = false;
            mockDome.VerifySet(x => x.DriverFollowing = It.IsAny<bool>(), Times.Never);

            sut.FollowEnabled = false;
            mockDome.VerifySet(x => x.DriverFollowing = It.IsAny<bool>(), Times.Never);
        }

        [Test]
        public async Task Test_DomeSynchronizeThrows_DisablesDirectSlaving() {
            var sut = await CreateSUT();
            sut.FollowEnabled = true;
            sut.DirectFollowToggled = true;
            var t1 = new TelescopeInfo() { Connected = true };
            mockDomeSynchronization.Setup(x => x.TargetDomeAzimuth(It.IsAny<Coordinates>(), It.IsAny<double>(), It.IsAny<Angle>(), It.IsAny<Angle>(), It.IsAny<PierSide>())).Throws(new InvalidOperationException("Error"));
            sut.UpdateDeviceInfo(t1);

            // Error getting the TargetDomeAzimuth disables slaving
            Assert.AreEqual(false, sut.FollowEnabled);
        }

        [Test]
        public async Task Test_DomeSynchronize_SlewIfExceedsTolerance() {
            var sut = await CreateSUT();
            sut.FollowEnabled = true;
            sut.DirectFollowToggled = true;
            var t1 = new TelescopeInfo() { Connected = true };
            domeTargetAzimuth = Angle.ByDegree(2);
            mockDome
                .Setup(x => x.SlewToAzimuth(domeTargetAzimuth.Degree, It.IsAny<CancellationToken>()))
                .Callback<double, CancellationToken>((x, y) => {
                    domeAzimuth = domeTargetAzimuth.Degree;
                }).Returns(Task.CompletedTask)
                .Verifiable();

            sut.UpdateDeviceInfo(t1);
            await sut.WaitForDomeSynchronization(CancellationToken.None);
            mockDome.Verify();
        }

        [Test]
        public async Task Test_OpenShutter_IfEnabled() {
            domeCanSetShutter = true;
            var sut = await CreateSUT();
            mockDome.Setup(x => x.OpenShutter(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();
            var result = await sut.OpenShutter(CancellationToken.None);
            Assert.IsTrue(result);
            mockDome.Verify();
        }

        [Test]
        public async Task Test_OpenShutter_NotIfDisabled() {
            domeCanSetShutter = false;
            var sut = await CreateSUT();
            var result = await sut.OpenShutter(CancellationToken.None);
            Assert.IsFalse(result);
            mockDome.Verify(x => x.OpenShutter(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Test_CloseShutter_IfEnabled() {
            domeCanSetShutter = true;
            var sut = await CreateSUT();
            mockDome.Setup(x => x.CloseShutter(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();
            var result = await sut.CloseShutter(CancellationToken.None);
            Assert.IsTrue(result);
            mockDome.Verify();
        }

        [Test]
        public async Task Test_CloseShutter_NotIfDisabled() {
            domeCanSetShutter = false;
            var sut = await CreateSUT();
            var result = await sut.CloseShutter(CancellationToken.None);
            Assert.IsFalse(result);
            mockDome.Verify(x => x.CloseShutter(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Test_Park_IfEnabled() {
            domeCanPark = true;
            var sut = await CreateSUT();
            mockDome.Setup(x => x.Park(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();
            var result = await sut.Park(CancellationToken.None);
            Assert.IsTrue(result);
            mockDome.Verify();
        }

        [Test]
        public async Task Test_Park_NotIfDisabled() {
            domeCanPark = false;
            var sut = await CreateSUT();
            var result = await sut.Park(CancellationToken.None);
            Assert.IsFalse(result);
            mockDome.Verify(x => x.Park(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}