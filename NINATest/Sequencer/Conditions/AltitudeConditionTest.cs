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
using NINA.Sequencer.SequenceItem.Telescope;
using NINA.Utility.Astrometry;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        [TestCase(30, 30, true)]
        [TestCase(40, 35, true)]
        [TestCase(29, 30, false)]
        [TestCase(0, 0, true)]
        [TestCase(10, 0, true)]
        [TestCase(-10, 0, false)]
        [TestCase(-10, -11, true)]
        [TestCase(-10, -10, true)]
        [TestCase(-20, -10, false)]
        public void Check_Altitude_MustBeAboveToBeTrue(double currentAltitude, double targetAltitude, bool isValid) {
            var altaz = new TopocentricCoordinates(Angle.Zero, Angle.ByDegree(currentAltitude), Angle.Zero, Angle.Zero);
            var coords = altaz.Transform(Epoch.J2000);

            var sut = new AltitudeCondition(profileServiceMock.Object);
            sut.Coordinates.Coordinates = coords;
            sut.Altitude = targetAltitude;

            sut.Check(null).Should().Be(isValid);
        }
    }
}