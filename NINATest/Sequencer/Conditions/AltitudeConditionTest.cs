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
using NINA.Profile;
using NINA.Sequencer.Conditions;
using NINA.Utility.Astrometry;
using NUnit.Framework;

namespace NINATest.Sequencer.Conditions {

    [TestFixture]
    public class AltitudeConditionTest {
        private Mock<IProfileService> profileServiceMock;

        [SetUp]
        public void Setup() {
            profileServiceMock = new Mock<IProfileService>();
            profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Latitude).Returns(0);
            profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Longitude).Returns(0);
        }

        [Test]
        public void AltitudeConditionTest_Clone_GoodClone() {
            var sut = new AltitudeCondition(profileServiceMock.Object);
            sut.Icon = new System.Windows.Media.GeometryGroup();
            var item2 = (AltitudeCondition)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Icon.Should().BeSameAs(sut.Icon);
            item2.Altitude.Should().Be(sut.Altitude);
        }

        [Test]
        [TestCase(30, 30)]
        [TestCase(40, 35)]
        [TestCase(35, 40)]
        public void Check_When_Scope_Pointing_East_Returns_True(double currentAltitude, double targetAltitude) {
            var altaz = new TopocentricCoordinates(Angle.ByDegree(90), Angle.ByDegree(currentAltitude), Angle.Zero, Angle.Zero);
            var coords = altaz.Transform(Epoch.J2000);

            var sut = new AltitudeCondition(profileServiceMock.Object);
            sut.Coordinates.Coordinates = coords;
            sut.Altitude = targetAltitude;

            Assert.IsTrue(
                sut.Check(null));
        }


        [Test]
        [TestCase(30, 30)]
        [TestCase(40, 35)]
        [TestCase(0, 0)]
        [TestCase(10, 0)]
        [TestCase(-10, -11)]
        [TestCase(-10, -10)]
        public void Check_When_Scope_Is_Pointing_West_Above_Target_Alt_Returns_True(double currentAltitude, double targetAltitude) {
            var altaz = new TopocentricCoordinates(Angle.ByDegree(270), Angle.ByDegree(currentAltitude), Angle.Zero, Angle.Zero);
            var coords = altaz.Transform(Epoch.J2000);

            var sut = new AltitudeCondition(profileServiceMock.Object);
            sut.Coordinates.Coordinates = coords;
            sut.Altitude = targetAltitude;

            Assert.IsTrue(
                sut.Check(null));
        }


        [Test]
        [TestCase(29, 30)]
        [TestCase(-10, 0)]
        [TestCase(-20, -10)]
        public void Check_When_Scope_Is_Pointing_West_Below_Target_Alt_Returns_False(double currentAltitude, double targetAltitude) {
            var altaz = new TopocentricCoordinates(Angle.ByDegree(270), Angle.ByDegree(currentAltitude), Angle.Zero, Angle.Zero);
            var coords = altaz.Transform(Epoch.J2000);

            var sut = new AltitudeCondition(profileServiceMock.Object);
            sut.Coordinates.Coordinates = coords;
            sut.Altitude = targetAltitude;
            Assert.IsFalse(
                sut.Check(null));
        }
    }
}