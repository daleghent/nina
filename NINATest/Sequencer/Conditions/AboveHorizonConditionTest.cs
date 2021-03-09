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
using NINA.Sequencer.Container;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NUnit.Framework;
using System;
using System.IO;

namespace NINATest.Sequencer.Conditions {

    [TestFixture]
    public class AboveHorizonConditionTest {
        private Mock<IProfileService> profileServiceMock;

        [SetUp]
        public void Setup() {
            profileServiceMock = new Mock<IProfileService>();
            profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Latitude).Returns(0);
            profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Longitude).Returns(0);
        }

        [Test]
        public void AboveHorizonConditionTest_Clone_GoodClone() {
            var sut = new AboveHorizonCondition(profileServiceMock.Object);
            sut.Icon = new System.Windows.Media.GeometryGroup();
            sut.Coordinates = new NINA.Model.InputCoordinates(new Coordinates(20, 20, Epoch.J2000, Coordinates.RAType.Degrees));
            var item2 = (AboveHorizonCondition)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Icon.Should().BeSameAs(sut.Icon);
            item2.Coordinates.Should().NotBeSameAs(sut.Coordinates);
            item2.Coordinates.Coordinates.RA.Should().Be(sut.Coordinates.Coordinates.RA);
            item2.Coordinates.Coordinates.Dec.Should().Be(sut.Coordinates.Coordinates.Dec);
        }

        [Test]
        public void StandardHorizon_AboveHorizon_CheckTrue() {
            var sut = new AboveHorizonCondition(profileServiceMock.Object);
            var mockDateProvider = new Mock<ICustomDateTime>();
            mockDateProvider.SetupGet(x => x.Now).Returns(DateTime.ParseExact("20191231T23:00:00Z", "yyyyMMddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture));
            var coordinates = new Coordinates(Angle.ByDegree(1), Angle.ByDegree(2), Epoch.J2000, mockDateProvider.Object);

            sut.Coordinates.Coordinates = coordinates;

            sut.Check(default).Should().BeTrue();
        }

        [Test]
        public void StandardHorizon_AboveHorizon_CheckFalse() {
            var sut = new AboveHorizonCondition(profileServiceMock.Object);
            var mockDateProvider = new Mock<ICustomDateTime>();
            mockDateProvider.SetupGet(x => x.Now).Returns(new DateTime(2020, 1, 1, 1, 0, 0));
            var coordinates = new Coordinates(Angle.ByDegree(1), Angle.ByDegree(2), Epoch.J2000, mockDateProvider.Object);

            sut.Coordinates.Coordinates = coordinates;

            sut.Check(default).Should().BeFalse();
        }

        [Test]
        public void CustomHorizon_AboveHorizon_CheckTrue() {
            var horizonDefinition = $"20 20" + Environment.NewLine + "100 20";
            using (var sr = new StringReader(horizonDefinition)) {
                var horizon = CustomHorizon.FromReader(sr);
                profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Horizon).Returns(horizon);
            }

            var sut = new AboveHorizonCondition(profileServiceMock.Object);
            var mockDateProvider = new Mock<ICustomDateTime>();
            mockDateProvider.SetupGet(x => x.Now).Returns(DateTime.ParseExact("20200101T22:00:00Z", "yyyyMMddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture));
            var coordinates = new Coordinates(Angle.ByDegree(1), Angle.ByDegree(2), Epoch.J2000, mockDateProvider.Object);

            sut.Coordinates.Coordinates = coordinates;

            sut.Check(default).Should().BeTrue();
        }

        [Test]
        public void CustomHorizon_AboveHorizon_CheckFalse() {
            var horizonDefinition = $"20 20" + Environment.NewLine + "100 20";
            using (var sr = new StringReader(horizonDefinition)) {
                var horizon = CustomHorizon.FromReader(sr);
                profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Horizon).Returns(horizon);
            }

            var sut = new AboveHorizonCondition(profileServiceMock.Object);
            var mockDateProvider = new Mock<ICustomDateTime>();
            mockDateProvider.SetupGet(x => x.Now).Returns(new DateTime(2020, 1, 1, 0, 0, 0));
            var coordinates = new Coordinates(Angle.ByDegree(1), Angle.ByDegree(2), Epoch.J2000, mockDateProvider.Object);

            sut.Coordinates.Coordinates = coordinates;

            sut.Check(default).Should().BeFalse();
        }

        [Test]
        public void AttachNewParent_HasDSOContainerParent_RetrieveParentCoordinates() {
            var coordinates = new Coordinates(Angle.ByDegree(1), Angle.ByDegree(2), Epoch.J2000);
            var parentMock = new Mock<IDeepSkyObjectContainer>();
            parentMock
                .SetupGet(x => x.Target)
                .Returns(
                new NINA.Model.InputTarget(Angle.ByDegree(1), Angle.ByDegree(2), null) {
                    InputCoordinates = new NINA.Model.InputCoordinates() {
                        Coordinates = coordinates
                    }
                }
            );

            var sut = new AboveHorizonCondition(profileServiceMock.Object);
            sut.AttachNewParent(parentMock.Object);

            sut.Coordinates.Coordinates.RA.Should().Be(coordinates.RA);
            sut.Coordinates.Coordinates.Dec.Should().Be(coordinates.Dec);
        }
    }
}