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
using NINA.Astrometry;
using NINA.Sequencer.Container;
using NINA.Sequencer.Utility;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINATest.Sequencer.Utility {

    [TestFixture]
    public class ItemUtilityTest {

        [Test]
        public void RetrieveContextCoordinates_IsNull_NoParent_ReturnNull() {
            var coordinates = ItemUtility.RetrieveContextCoordinates(null);

            coordinates.Item1.Should().BeNull();
            coordinates.Item2.Should().Be(0);
        }

        [Test]
        public void RetrieveContextCoordinates_NoDSOContainer_NoParent_ReturnNull() {
            var containerMock = new Mock<ISequenceContainer>();

            var coordinates = ItemUtility.RetrieveContextCoordinates(containerMock.Object);

            coordinates.Item1.Should().BeNull();
            coordinates.Item2.Should().Be(0);
        }

        [Test]
        public void RetrieveContextCoordinates_NoDSOContainer_HasParent_ReturnNull() {
            var parentMock = new Mock<ISequenceContainer>();
            var containerMock = new Mock<ISequenceContainer>();
            containerMock.SetupGet(x => x.Parent).Returns(parentMock.Object);

            var coordinates = ItemUtility.RetrieveContextCoordinates(containerMock.Object);

            coordinates.Item1.Should().BeNull();
            coordinates.Item2.Should().Be(0);
        }

        [Test]
        public void RetrieveContextCoordinates_IsDSOContainer_NoParent_ReturnCoordinates() {
            var containerMock = new Mock<IDeepSkyObjectContainer>();

            var target = new InputTarget(Angle.ByDegree(10), Angle.ByDegree(10), null);
            var coords = new Coordinates(Angle.ByDegree(5), Angle.ByDegree(20), Epoch.J2000);
            var inputCoords = new InputCoordinates(coords);
            target.InputCoordinates = inputCoords;
            target.Rotation = 100;
            containerMock.SetupGet(x => x.Target).Returns(target);

            var coordinates = ItemUtility.RetrieveContextCoordinates(containerMock.Object);

            coordinates.Item1.Should().NotBeNull();
            coordinates.Item1.RA.Should().Be(coords.RA);
            coordinates.Item1.Dec.Should().Be(coords.Dec);
            coordinates.Item2.Should().Be(100);
        }

        [Test]
        public void RetrieveContextCoordinates_IsNotDSOContainer_HasDSOParent_ReturnCoordinates() {
            var parentMock = new Mock<IDeepSkyObjectContainer>();
            var containerMock = new Mock<ISequenceContainer>();
            containerMock.SetupGet(x => x.Parent).Returns(parentMock.Object);

            var target = new InputTarget(Angle.ByDegree(10), Angle.ByDegree(10), null);
            var coords = new Coordinates(Angle.ByDegree(5), Angle.ByDegree(20), Epoch.J2000);
            var inputCoords = new InputCoordinates(coords);
            target.InputCoordinates = inputCoords;
            target.Rotation = 100;
            parentMock.SetupGet(x => x.Target).Returns(target);

            var coordinates = ItemUtility.RetrieveContextCoordinates(containerMock.Object);

            coordinates.Item1.Should().NotBeNull();
            coordinates.Item1.RA.Should().Be(coords.RA);
            coordinates.Item1.Dec.Should().Be(coords.Dec);
            coordinates.Item2.Should().Be(100);
        }

        [Test]
        public void IsInRootContainer_IsNull_ReturnFalse() {
            var isIn = ItemUtility.IsInRootContainer(null);

            isIn.Should().BeFalse();
        }

        [Test]
        public void IsInRootContainer_IsNoRootContainer_NoParent_ReturnFalse() {
            var containerMock = new Mock<ISequenceContainer>();

            var isIn = ItemUtility.IsInRootContainer(containerMock.Object);

            isIn.Should().BeFalse();
        }

        [Test]
        public void IsInRootContainer_IsNoRootContainer_HasParent_ReturnFalse() {
            var parentMock = new Mock<ISequenceContainer>();
            var containerMock = new Mock<ISequenceContainer>();
            containerMock.SetupGet(x => x.Parent).Returns(parentMock.Object);

            var isIn = ItemUtility.IsInRootContainer(containerMock.Object);

            isIn.Should().BeFalse();
        }

        [Test]
        public void IsInRootContainer_IsRootContainer_NoParent_ReturnTrue() {
            var containerMock = new Mock<ISequenceRootContainer>();

            var isIn = ItemUtility.IsInRootContainer(containerMock.Object);

            isIn.Should().BeTrue();
        }

        [Test]
        public void IsInRootContainer_IsNoRootContainer_HasRootParent_ReturnTrue() {
            var parentMock = new Mock<ISequenceRootContainer>();
            var containerMock = new Mock<ISequenceContainer>();
            containerMock.SetupGet(x => x.Parent).Returns(parentMock.Object);

            var isIn = ItemUtility.IsInRootContainer(containerMock.Object);

            isIn.Should().BeTrue();
        }
    }
}