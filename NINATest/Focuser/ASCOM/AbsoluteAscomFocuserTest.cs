using ASCOM.DeviceInterface;
using Moq;
using NINA.Model.MyFocuser;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace NINATest.Focuser.ASCOM {

    [TestFixture]
    internal class AbsoluteAscomFocuserTest {
        private AbsoluteAscomFocuser sut;
        private Mock<IFocuserV3> mockFocuser;

        [OneTimeSetUp]
        public void OneTimeSetup() {
            mockFocuser = new Mock<IFocuserV3>();
        }

        [SetUp]
        public void Init() {
            mockFocuser.Reset();
            mockFocuser.SetupProperty(m => m.Connected, false);
            sut = new AbsoluteAscomFocuser(mockFocuser.Object);
        }

        [Test]
        public async Task TestMoveConnectedWithTempCompOn() {
            mockFocuser.Setup(m => m.Connected).Returns(true);
            mockFocuser.Setup(m => m.TempCompAvailable).Returns(true);
            mockFocuser.Setup(m => m.TempComp).Returns(true);
            mockFocuser.SetupSequence(m => m.Position).Returns(0).Returns(10);
            mockFocuser.SetupSequence(m => m.IsMoving).Returns(true).Returns(false);

            await sut.MoveAsync(10, new CancellationToken(), 0);

            mockFocuser.Verify(m => m.Move(10), Times.Once);
            mockFocuser.VerifySet(m => m.TempComp = true, Times.Once);
            mockFocuser.VerifySet(m => m.TempComp = false, Times.Once);
        }

        [Test]
        public async Task TestMoveConnectedWithTempCompOff() {
            mockFocuser.Setup(m => m.Connected).Returns(true);
            mockFocuser.Setup(m => m.TempCompAvailable).Returns(true);
            mockFocuser.Setup(m => m.TempComp).Returns(false);
            mockFocuser.SetupSequence(m => m.Position).Returns(0).Returns(10);
            mockFocuser.SetupSequence(m => m.IsMoving).Returns(true).Returns(false);

            await sut.MoveAsync(10, new CancellationToken(), 0);

            mockFocuser.Verify(m => m.Move(10), Times.Once);
            mockFocuser.VerifySet(m => m.TempComp = true, Times.Never);
            mockFocuser.VerifySet(m => m.TempComp = false, Times.Never);
        }

        [Test]
        public async Task TestMoveNotConnected() {
            mockFocuser.Setup(m => m.Connected).Returns(false);
            mockFocuser.Setup(m => m.TempCompAvailable).Returns(true);

            await sut.MoveAsync(10, new CancellationToken(), 0);

            mockFocuser.Verify(m => m.Move(It.IsAny<int>()), Times.Never);
            mockFocuser.VerifySet(m => m.TempComp = true, Times.Never);
            mockFocuser.VerifySet(m => m.TempComp = false, Times.Never);
        }
    }
}