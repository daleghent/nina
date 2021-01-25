using Moq;
using NINA.Model.MyDome;
using NINA.Model.MyTelescope;
using NINA.Profile;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel.Equipment.Dome;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace NINATest.Dome {

    [TestFixture]
    internal class DomeFollowerTest {
        private Mock<IProfileService> mockProfileService;
        private Mock<ITelescopeMediator> mockTelescopeMediator;
        private Mock<IDomeMediator> mockDomeMediator;
        private Mock<IDomeSynchronization> mockDomeSynchronization;

        private Angle siteLatitude;
        private Angle siteLongitude;
        private Angle domeTargetAzimuth;
        private double domeAzimuth;
        private bool synchronizeDuringMountSlew;
        private double domeAzimuthToleranceDegrees;

        [SetUp]
        public void Init() {
            mockProfileService = new Mock<IProfileService>();
            mockTelescopeMediator = new Mock<ITelescopeMediator>();
            mockDomeMediator = new Mock<IDomeMediator>();
            mockDomeSynchronization = new Mock<IDomeSynchronization>();

            synchronizeDuringMountSlew = false;
            domeAzimuthToleranceDegrees = 1.0;
            domeTargetAzimuth = Angle.ByDegree(0.0);
            siteLatitude = Angle.ByDegree(41.5);
            siteLongitude = Angle.ByDegree(-23.2);

            mockProfileService.SetupGet(p => p.ActiveProfile.DomeSettings.AzimuthTolerance_degrees).Returns(() => domeAzimuthToleranceDegrees);
            mockProfileService.SetupGet(p => p.ActiveProfile.DomeSettings.SynchronizeDuringMountSlew).Returns(() => synchronizeDuringMountSlew);
            mockDomeSynchronization
                .Setup(x => x.TargetDomeAzimuth(It.IsAny<Coordinates>(), It.IsAny<double>(), It.IsAny<Angle>(), It.IsAny<Angle>(), It.IsAny<PierSide>()))
                .Returns(() => domeTargetAzimuth);
        }

        private DomeFollower CreateSUT() {
            return new DomeFollower(mockProfileService.Object, mockTelescopeMediator.Object, mockDomeMediator.Object, mockDomeSynchronization.Object);
        }

        [Test]
        public void Test_DomeSynchronize_ReceivesCorrectParameters() {
            mockProfileService.SetupGet(p => p.ActiveProfile.MeridianFlipSettings.UseSideOfPier).Returns(() => false);
            var sut = CreateSUT();
            var t1 = new TelescopeInfo() {
                Connected = true,
                SiteLatitude = siteLatitude.Degree,
                SiteLongitude = siteLongitude.Degree,
                SiderealTime = 11.2,
                SideOfPier = PierSide.pierEast,
                Coordinates = new Coordinates(1.0, 2.0, Epoch.J2000, Coordinates.RAType.Degrees)
            };
            var d1 = new DomeInfo() {
                Connected = true,
                Azimuth = 10.0
            };
            sut.UpdateDeviceInfo(t1);
            sut.UpdateDeviceInfo(d1);
            sut.TriggerTelescopeSync();
            mockDomeSynchronization.Verify(x => x.TargetDomeAzimuth(t1.Coordinates, t1.SiderealTime, siteLatitude, siteLongitude, t1.SideOfPier), Times.Once);
        }

        [Test]
        public async Task Test_SlewIfExceedsTolerance() {
            mockProfileService.SetupGet(p => p.ActiveProfile.MeridianFlipSettings.UseSideOfPier).Returns(() => false);
            var sut = CreateSUT();
            domeAzimuth = 0.0;
            domeTargetAzimuth = Angle.ByDegree(2);
            var t1 = new TelescopeInfo() {
                Connected = true,
                SiteLatitude = siteLatitude.Degree,
                SiteLongitude = siteLongitude.Degree,
                SiderealTime = 11.2,
                SideOfPier = PierSide.pierEast,
                Coordinates = new Coordinates(1.0, 2.0, Epoch.J2000, Coordinates.RAType.Degrees)
            };
            var d1 = new DomeInfo() { Connected = true, Azimuth = domeAzimuth };
            mockDomeMediator
                .Setup(x => x.SlewToAzimuth(domeTargetAzimuth.Degree, It.IsAny<CancellationToken>()))
                .Callback<double, CancellationToken>((x, y) => {
                    domeAzimuth = x;
                }).Returns(Task.FromResult(true))
                .Verifiable();

            sut.UpdateDeviceInfo(t1);
            sut.UpdateDeviceInfo(d1);
            sut.TriggerTelescopeSync();
            await sut.WaitForDomeSynchronization(CancellationToken.None);
            mockDomeMediator.Verify();
            Assert.AreEqual(domeAzimuth, domeTargetAzimuth.Degree);
        }
    }
}