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
using NINA.Model;
using NINA.Profile;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Enum;
using NINA.Utility.Mediator.Interfaces;
using NUnit.Framework;
using System;

namespace NINATest.Sequencer.SequenceItem.Utility {

    [TestFixture]
    public class WaitForAltitudeTest {
        private Mock<IProfileService> profileServiceMock;
        private WaitForAltitude sut;

        [SetUp]
        public void Setup() {
            profileServiceMock = new Mock<IProfileService>();
            profileServiceMock.Setup(m => m.ActiveProfile.AstrometrySettings).Returns(new AstrometrySettings() {
                Latitude = 47,
                Longitude = 8
            });
            sut = new WaitForAltitude(profileServiceMock.Object);
        }

        [Test]
        public void WaitForAltitude_Clone_GoodClone() {
            sut.Icon = new System.Windows.Media.GeometryGroup();
            sut.Coordinates = new NINA.Model.InputCoordinates(new Coordinates(20, 20, Epoch.J2000, Coordinates.RAType.Degrees));

            var item2 = (WaitForAltitude)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Name.Should().BeSameAs(sut.Name);
            item2.Description.Should().BeSameAs(sut.Description);
            item2.Icon.Should().BeSameAs(sut.Icon);
            item2.Altitude.Should().Be(sut.Altitude);
            item2.AboveOrBelow.Should().Be(sut.AboveOrBelow);
            item2.Coordinates.Should().NotBeSameAs(sut.Coordinates);
            item2.Coordinates.Coordinates.RA.Should().Be(sut.Coordinates.Coordinates.RA);
            item2.Coordinates.Coordinates.Dec.Should().Be(sut.Coordinates.Coordinates.Dec);
        }

        [Test]
        public void WaitForAltitudeTest_GetEstimatedDuration_Test() {
            var estimate = sut.GetEstimatedDuration();

            estimate.Should().Be(TimeSpan.Zero);
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

            sut.AttachNewParent(parentMock.Object);

            sut.Coordinates.Coordinates.RA.Should().Be(coordinates.RA);
            sut.Coordinates.Coordinates.Dec.Should().Be(coordinates.Dec);
        }
    }
}