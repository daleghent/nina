using ASCOM.DeviceInterface;
using Moq;
using NINA.Model.MyFocuser;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;

namespace NINATest.Focuser.ASCOM {
    [TestFixture]
    internal class RelativeAscomFocuserTest {
        private RelativeAscomFocuser sut;
        private Mock<IFocuserV3> mockFocuser;
        private static readonly int INITIAL_POSITION = 5000;

        [OneTimeSetUp]
        public void OneTimeSetup() {
            mockFocuser = new Mock<IFocuserV3>();
        }

        [SetUp]
        public void Init() {
            mockFocuser.Reset();
            mockFocuser.SetupProperty(m => m.Connected, false);
            sut = new RelativeAscomFocuser(mockFocuser.Object);
        }

        [Test]
        public async Task TestPositiveMoveConnectedWithTempCompOn() {
            mockFocuser.Setup(m => m.Connected).Returns(true);
            mockFocuser.Setup(m => m.TempCompAvailable).Returns(true);
            mockFocuser.Setup(m => m.TempComp).Returns(true);
            mockFocuser.Setup(m => m.MaxStep).Returns(5);
            mockFocuser.SetupSequence(m => m.IsMoving).Returns(true).Returns(false);

            const int moveAmount = 12;
            await sut.MoveAsync(INITIAL_POSITION + moveAmount, new CancellationToken(), 0);

            mockFocuser.Verify(m => m.Move(5), Times.Exactly(2));
            mockFocuser.Verify(m => m.Move(2), Times.Once);
            mockFocuser.Verify(m => m.IsMoving, Times.Exactly(4));
            sut.Position.Should().Be(INITIAL_POSITION + moveAmount);
            mockFocuser.VerifySet(m => m.TempComp = true, Times.Once);
            mockFocuser.VerifySet(m => m.TempComp = false, Times.Once);
        }

        [Test]
        public async Task TestPositiveMoveConnectedWithTempCompOff() {
            mockFocuser.Setup(m => m.Connected).Returns(true);
            mockFocuser.Setup(m => m.TempCompAvailable).Returns(true);
            mockFocuser.Setup(m => m.TempComp).Returns(false);
            mockFocuser.Setup(m => m.MaxStep).Returns(5);
            mockFocuser.SetupSequence(m => m.IsMoving).Returns(true).Returns(false);

            const int moveAmount = 12;
            await sut.MoveAsync(INITIAL_POSITION + moveAmount, new CancellationToken(), 0);

            mockFocuser.Verify(m => m.Move(5), Times.Exactly(2));
            mockFocuser.Verify(m => m.Move(2), Times.Once);
            mockFocuser.Verify(m => m.IsMoving, Times.Exactly(4));
            sut.Position.Should().Be(INITIAL_POSITION + moveAmount);
            mockFocuser.VerifySet(m => m.TempComp = true, Times.Never);
            mockFocuser.VerifySet(m => m.TempComp = false, Times.Never);
        }

        [Test]
        public async Task TestPositiveMoveNotConnected() {
            mockFocuser.Setup(m => m.Connected).Returns(false);

            const int moveAmount = 12;
            await sut.MoveAsync(INITIAL_POSITION + moveAmount, new CancellationToken(), 0);

            mockFocuser.Verify(m => m.Move(It.IsAny<int>()), Times.Never);
            sut.Position.Should().Be(INITIAL_POSITION);
            mockFocuser.VerifySet(m => m.TempComp = true, Times.Never);
            mockFocuser.VerifySet(m => m.TempComp = false, Times.Never);
        }

        [Test]
        public async Task TestNegativeMoveConnectedWithTempCompOn() {
            mockFocuser.Setup(m => m.Connected).Returns(true);
            mockFocuser.Setup(m => m.TempCompAvailable).Returns(true);
            mockFocuser.Setup(m => m.TempComp).Returns(true);
            mockFocuser.Setup(m => m.MaxStep).Returns(5);
            mockFocuser.SetupSequence(m => m.IsMoving).Returns(true).Returns(false);

            const int moveAmount = -12;
            await sut.MoveAsync(INITIAL_POSITION + moveAmount, new CancellationToken(), 0);

            mockFocuser.Verify(m => m.Move(-5), Times.Exactly(2));
            mockFocuser.Verify(m => m.Move(-2), Times.Once);
            mockFocuser.Verify(m => m.IsMoving, Times.Exactly(4));
            sut.Position.Should().Be(INITIAL_POSITION + moveAmount);
            mockFocuser.VerifySet(m => m.TempComp = true, Times.Once);
            mockFocuser.VerifySet(m => m.TempComp = false, Times.Once);
        }

        [Test]
        public async Task TestNegativeMoveConnectedWithTempCompOff() {
            mockFocuser.Setup(m => m.Connected).Returns(true);
            mockFocuser.Setup(m => m.TempCompAvailable).Returns(true);
            mockFocuser.Setup(m => m.TempComp).Returns(false);
            mockFocuser.Setup(m => m.MaxStep).Returns(5);
            mockFocuser.SetupSequence(m => m.IsMoving).Returns(true).Returns(false);

            const int moveAmount = -12;
            await sut.MoveAsync(INITIAL_POSITION + moveAmount, new CancellationToken(), 0);

            mockFocuser.Verify(m => m.Move(-5), Times.Exactly(2));
            mockFocuser.Verify(m => m.Move(-2), Times.Once);
            mockFocuser.Verify(m => m.IsMoving, Times.Exactly(4));
            sut.Position.Should().Be(INITIAL_POSITION + moveAmount);
            mockFocuser.VerifySet(m => m.TempComp = true, Times.Never);
            mockFocuser.VerifySet(m => m.TempComp = false, Times.Never);
        }

        [Test]
        public async Task TestNegativeMoveNotConnected() {
            mockFocuser.Setup(m => m.Connected).Returns(false);

            const int moveAmount = -12;
            await sut.MoveAsync(INITIAL_POSITION + moveAmount, new CancellationToken(), 0);

            mockFocuser.Verify(m => m.Move(It.IsAny<int>()), Times.Never);
            sut.Position.Should().Be(INITIAL_POSITION);
            mockFocuser.VerifySet(m => m.TempComp = true, Times.Never);
            mockFocuser.VerifySet(m => m.TempComp = false, Times.Never);
        }
    }
}