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
using System.ComponentModel;
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
        private Mock<IDomeFollower> mockDomeFollower;
        private Mock<IDeviceUpdateTimerFactory> mockDeviceUpdateTimerFactory;
        private Mock<IDeviceUpdateTimer> mockDeviceUpdateTimer;
        private Mock<IApplicationResourceDictionary> mockResourceDictionary;
        private Mock<IDome> mockDome;

        private string domeId;
        private bool domeConnected;
        private ShutterState domeShutterState;
        private bool domeDriverCanFollow;
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
        private bool domeDriverFollowing;

        [SetUp]
        public void Init() {
            domeId = "ID";
            domeConnected = false;
            domeShutterState = ShutterState.ShutterOpen;
            domeDriverCanFollow = true;
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
            domeDriverFollowing = false;

            mockProfileService = new Mock<IProfileService>();
            mockDomeDeviceChooserVM = new Mock<IDeviceChooserVM>();
            mockApplicationStatusMediator = new Mock<IApplicationStatusMediator>();
            mockDomeMediator = new Mock<IDomeMediator>();
            mockTelescopeMediator = new Mock<ITelescopeMediator>();
            mockDomeFollower = new Mock<IDomeFollower>();
            mockDomeFollower.Setup(x => x.GetSynchronizedPosition(It.IsAny<TelescopeInfo>())).Returns(() => domeTargetAzimuth);
            mockDeviceUpdateTimer = new Mock<IDeviceUpdateTimer>();
            mockDeviceUpdateTimerFactory = new Mock<IDeviceUpdateTimerFactory>();
            mockDeviceUpdateTimerFactory
                .Setup(x => x.Create(It.IsAny<Func<Dictionary<string, object>>>(), It.IsAny<Action<Dictionary<string, object>>>(), It.IsAny<double>()))
                .Returns(mockDeviceUpdateTimer.Object);
            mockResourceDictionary = new Mock<IApplicationResourceDictionary>();
            mockProfileService.SetupProperty(p => p.ActiveProfile.DomeSettings.Id);
            mockProfileService.SetupGet(p => p.ActiveProfile.ApplicationSettings.DevicePollingInterval).Returns(1);

            mockApplicationStatusMediator.Setup(x => x.StatusUpdate(It.IsAny<ApplicationStatus>()));
        }

        private async Task<DomeVM> CreateSUT() {
            var domeVM = new DomeVM(mockProfileService.Object, mockDomeMediator.Object, mockApplicationStatusMediator.Object, mockTelescopeMediator.Object,
                mockDomeDeviceChooserVM.Object, mockDomeFollower.Object, mockResourceDictionary.Object, mockDeviceUpdateTimerFactory.Object);

            mockDome = new Mock<IDome>();
            mockDome.SetupGet(x => x.Id).Returns(() => domeId);
            mockDome.SetupGet(x => x.Connected).Returns(() => domeConnected);
            mockDome.SetupGet(x => x.ShutterStatus).Returns(() => domeShutterState);
            mockDome.SetupGet(x => x.DriverCanFollow).Returns(() => domeDriverCanFollow);
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
            mockDome.SetupGet(x => x.DriverFollowing).Returns(() => domeDriverFollowing);
            mockDome.SetupSet(x => x.DriverFollowing = It.IsAny<bool>()).Callback<bool>(v => {
                if (!domeDriverCanFollow) {
                    throw new InvalidOperationException("Dome cannot slave");
                }
                domeDriverFollowing = v;
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
        public async Task Test_DomeFollowEnabled_Starts() {
            var sut = await CreateSUT();
            sut.FollowEnabled = true;
            mockDomeFollower.Verify(x => x.Start(), Times.Once);
        }

        [Test]
        public async Task Test_DomeDisconnected_DomeFollowEnabled_NoStart() {
            var sut = await CreateSUT();
            domeConnected = false;
            sut.FollowEnabled = true;
            mockDomeFollower.Verify(x => x.Start(), Times.Never);
        }

        [Test]
        public async Task Test_DomeFollowStops_ToggleSwitchedOff() {
            var sut = await CreateSUT();
            sut.FollowEnabled = true;
            mockDomeFollower.Verify(x => x.Start(), Times.Once);
            mockDomeFollower.SetupGet(f => f.IsFollowing).Returns(false);
            mockDomeFollower.Raise(f => f.PropertyChanged += null, new PropertyChangedEventArgs(nameof(IDomeFollower.IsFollowing)));
            mockDomeFollower.Verify(x => x.Stop(), Times.Once);
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