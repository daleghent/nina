#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

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
using NINA.Utility.Enum;
using NUnit.Framework;

namespace NINATest.Sequencer.Conditions {

    [TestFixture]
    public class MoonAltitudeConditionTest {
        private Mock<IProfileService> profileServiceMock;

        [SetUp]
        public void Setup() {
            profileServiceMock = new Mock<IProfileService>();
            profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Latitude).Returns(0);
            profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Longitude).Returns(0);
        }

        [Test]
        public void MoonAltitudeCondition_Clone_GoodClone() {
            var sut = new MoonAltitudeCondition(profileServiceMock.Object);
            sut.Icon = new System.Windows.Media.GeometryGroup();
            var item2 = (MoonAltitudeCondition)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Icon.Should().BeSameAs(sut.Icon);
            item2.UserMoonAltitude.Should().Be(sut.UserMoonAltitude);
            item2.Comparator.Should().Be(sut.Comparator);
        }

        [Test]
        public void MoonAltitudeCondition_NoProviderInConstructor_NoCrash() {
            var sut = new MoonAltitudeCondition(profileServiceMock.Object);

            sut.UserMoonAltitude.Should().Be(0);
            sut.Comparator.Should().Be(ComparisonOperatorEnum.GREATER_THAN);
        }
    }
}