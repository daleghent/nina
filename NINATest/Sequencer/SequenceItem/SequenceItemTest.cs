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
using NINA.Core.Enum;
using NINA.Model;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.Exceptions;
using NINA.Sequencer.SequenceItem;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINATest.Sequencer.SequenceItem {

    [TestFixture]
    public class SequenceItemTest {

        [Test]
        public void AttachNewParent_PreviousWasNull_NewParentAttached() {
            //Arrange
            var mock = new Mock<NINA.Sequencer.SequenceItem.SequenceItem>();
            var testContainerMock = new Mock<ISequenceContainer>();

            var newParent = testContainerMock.Object;

            //Act
            var sut = mock.Object;
            sut.AttachNewParent(newParent);

            //Assert
            sut.Parent.Should().Be(newParent);
            mock.Verify(x => x.AfterParentChanged(), Times.Once);
        }

        [Test]
        public async Task Run_Successfully_StatusToComplete() {
            //Arrange
            var mock = new Mock<NINA.Sequencer.SequenceItem.SequenceItem>();

            //Act
            var sut = mock.Object;
            await sut.Run(default, default);

            //Assert
            sut.Status.Should().Be(SequenceEntityStatus.FINISHED);
        }

        [Test]
        public async Task Run_Unsuccessfully_StatusToFailed() {
            //Arrange
            var mock = new Mock<NINA.Sequencer.SequenceItem.SequenceItem>();

            //Act
            mock.Setup(x => x.Execute(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).Throws(new Exception("Failed to run"));
            var sut = mock.Object;

            await sut.Run(default, default);

            //Assert
            sut.Status.Should().Be(SequenceEntityStatus.FAILED);
        }

        [Test]
        public async Task Run_Cancelled_StatusToCreated() {
            //Arrange
            var mock = new Mock<NINA.Sequencer.SequenceItem.SequenceItem>();

            //Act
            mock
                .Setup(x => x.Execute(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());
            var sut = mock.Object;

            try {
                await sut.Run(default, default);
                Assert.Fail(); // When no exception is thrown
            } catch (OperationCanceledException) {
            }

            //Assert
            sut.Status.Should().Be(SequenceEntityStatus.CREATED);
        }

        [Test]
        public async Task Run_Skipped_StatusToCreated() {
            //Arrange
            var mock = new Mock<NINA.Sequencer.SequenceItem.SequenceItem>();

            //Act
            mock
                .Setup(x => x.Execute(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .Throws(new SequenceItemSkippedException());
            var sut = mock.Object;

            await sut.Run(default, default);

            //Assert
            sut.Status.Should().Be(SequenceEntityStatus.SKIPPED);
        }

        [Test]
        public async Task ResetProgress() {
            //Arrange
            var mock = new Mock<NINA.Sequencer.SequenceItem.SequenceItem>();
            mock.CallBase = true;

            //Act
            var sut = mock.Object;
            await sut.Run(default, default);
            sut.ResetProgress();

            //Assert
            sut.Status.Should().Be(SequenceEntityStatus.CREATED);
        }

        [Test]
        public void Skip() {
            //Arrange
            var mock = new Mock<NINA.Sequencer.SequenceItem.SequenceItem>();

            //Act
            var sut = mock.Object;
            sut.Skip();

            //Assert
            sut.Status.Should().Be(SequenceEntityStatus.SKIPPED);
        }

        [Test]
        public void GetEstimatedDuration() {
            //Arrange
            var mock = new Mock<NINA.Sequencer.SequenceItem.SequenceItem>();
            mock.CallBase = true;

            //Act
            var sut = mock.Object;
            sut.GetEstimatedDuration().Should().Be(TimeSpan.Zero);
        }

        [Test]
        public void MoveUp() {
            var parent = new Mock<ISequenceContainer>();
            //Arrange
            var mock = new Mock<NINA.Sequencer.SequenceItem.SequenceItem>();
            mock.CallBase = true;

            //Act
            var sut = mock.Object;
            sut.AttachNewParent(parent.Object);
            sut.MoveUp();

            parent.Verify(x => x.MoveUp(It.Is<ISequenceItem>(s => s == sut)), Times.Once);
        }

        [Test]
        public void MoveDown() {
            var parent = new Mock<ISequenceContainer>();
            //Arrange
            var mock = new Mock<NINA.Sequencer.SequenceItem.SequenceItem>();
            mock.CallBase = true;

            //Act
            var sut = mock.Object;
            sut.AttachNewParent(parent.Object);
            sut.MoveDown();

            parent.Verify(x => x.MoveDown(It.Is<ISequenceItem>(s => s == sut)), Times.Once);
        }

        [Test]
        public void Detach() {
            var parent = new Mock<ISequenceContainer>();
            //Arrange
            var mock = new Mock<NINA.Sequencer.SequenceItem.SequenceItem>();
            mock.CallBase = true;

            //Act
            var sut = mock.Object;
            sut.AttachNewParent(parent.Object);
            sut.Detach();

            parent.Verify(x => x.Remove(It.Is<ISequenceItem>(s => s == sut)), Times.Once);
        }
    }
}