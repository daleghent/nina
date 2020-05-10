#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using Moq;
using NINA.Profile;
using NINA.ViewModel.Equipment.FlatDevice;
using NUnit.Framework;

namespace NINATest.FlatDevice {

    [TestFixture]
    public class FilterTimingTest {
        private Mock<IProfileService> _mockProfileService;
        private Mock<IFlatDeviceSettings> _mockSettings;
        private const double BRIGHTNESS = 1d;
        private const double TIME = 2d;
        private readonly FlatDeviceFilterSettingsKey _key = new FlatDeviceFilterSettingsKey(null, null, 0);

        [OneTimeSetUp]
        public void OneTimeSetup() {
            _mockProfileService = new Mock<IProfileService>();
            _mockSettings = new Mock<IFlatDeviceSettings>();
        }

        [SetUp]
        public void Init() {
            _mockProfileService.Reset();
            _mockSettings.Reset();
        }

        [Test]
        public void TestDoNotStoreIfTimeIsZero() {
            var sut = new FilterTiming(0d, 0d, _mockProfileService.Object, _key, false, true);
            _mockProfileService.Setup(m => m.ActiveProfile.FlatDeviceSettings).Returns(_mockSettings.Object);

            sut.Brightness = BRIGHTNESS;
            _mockSettings.Verify(m =>
                m.AddBrightnessInfo(It.IsAny<FlatDeviceFilterSettingsKey>(), It.IsAny<FlatDeviceFilterSettingsValue>()), Times.Never);
            Assert.That(sut.IsEmpty, Is.True);
        }

        [Test]
        public void TestDoNotStoreIfBrightnessIsZero() {
            var sut = new FilterTiming(0d, 0d, _mockProfileService.Object, _key, false, true);
            _mockProfileService.Setup(m => m.ActiveProfile.FlatDeviceSettings).Returns(_mockSettings.Object);

            sut.Time = TIME;
            _mockSettings.Verify(m =>
                m.AddBrightnessInfo(It.IsAny<FlatDeviceFilterSettingsKey>(), It.IsAny<FlatDeviceFilterSettingsValue>()), Times.Never);
            Assert.That(sut.IsEmpty, Is.True);
        }

        [Test]
        public void TestStoreIfTimeIsNotZero() {
            FlatDeviceFilterSettingsKey keyUsed = null;
            FlatDeviceFilterSettingsValue valueUsed = null;
            var sut = new FilterTiming(0d, TIME, _mockProfileService.Object, _key, false, false);
            _mockProfileService.Setup(m => m.ActiveProfile.FlatDeviceSettings).Returns(_mockSettings.Object);
            _mockSettings.Setup(m =>
                    m.AddBrightnessInfo(It.IsAny<FlatDeviceFilterSettingsKey>(),
                        It.IsAny<FlatDeviceFilterSettingsValue>()))
                .Callback((FlatDeviceFilterSettingsKey key, FlatDeviceFilterSettingsValue value) => {
                    keyUsed = key;
                    valueUsed = value;
                });

            sut.Brightness = BRIGHTNESS;
            _mockSettings.Verify(m =>
                m.AddBrightnessInfo(It.IsAny<FlatDeviceFilterSettingsKey>(), It.IsAny<FlatDeviceFilterSettingsValue>()), Times.Once);
            Assert.That(keyUsed, Is.EqualTo(_key));
            Assert.That(valueUsed.Time, Is.EqualTo(TIME));
            Assert.That(valueUsed.Brightness, Is.EqualTo(BRIGHTNESS));
        }

        [Test]
        public void TestStoreIfBrightnessIsNotZero() {
            FlatDeviceFilterSettingsKey keyUsed = null;
            FlatDeviceFilterSettingsValue valueUsed = null;
            var sut = new FilterTiming(BRIGHTNESS, 0d, _mockProfileService.Object, _key, false, false);
            _mockProfileService.Setup(m => m.ActiveProfile.FlatDeviceSettings).Returns(_mockSettings.Object);
            _mockSettings.Setup(m =>
                    m.AddBrightnessInfo(It.IsAny<FlatDeviceFilterSettingsKey>(),
                        It.IsAny<FlatDeviceFilterSettingsValue>()))
                .Callback((FlatDeviceFilterSettingsKey key, FlatDeviceFilterSettingsValue value) => {
                    keyUsed = key;
                    valueUsed = value;
                });

            sut.Time = TIME;
            _mockSettings.Verify(m =>
                m.AddBrightnessInfo(It.IsAny<FlatDeviceFilterSettingsKey>(), It.IsAny<FlatDeviceFilterSettingsValue>()), Times.Once);
            Assert.That(keyUsed, Is.EqualTo(_key));
            Assert.That(valueUsed.Time, Is.EqualTo(TIME));
            Assert.That(valueUsed.Brightness, Is.EqualTo(BRIGHTNESS));
        }
    }
}