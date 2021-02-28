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
using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Sequencer;
using NINA.Sequencer.Exceptions;
using NINA.Sequencer.SequenceItem.Camera;
using NINA.Utility.Mediator.Interfaces;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINATest.Sequencer.SequenceItem.Camera {

    [TestFixture]
    internal class CoolCameraTest {
        public Mock<ICameraMediator> cameraMediatorMock;

        [SetUp]
        public void Setup() {
            cameraMediatorMock = new Mock<ICameraMediator>();
        }

        [Test]
        public void Clone_ItemClonedProperly() {
            var sut = new CoolCamera(cameraMediatorMock.Object);
            sut.Name = "SomeName";
            sut.Description = "SomeDescription";
            sut.Icon = new System.Windows.Media.GeometryGroup();
            var item2 = (CoolCamera)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Name.Should().BeSameAs(sut.Name);
            item2.Description.Should().BeSameAs(sut.Description);
            item2.Icon.Should().BeSameAs(sut.Icon);
            item2.Temperature.Should().Be(sut.Temperature);
            item2.Duration.Should().Be(sut.Duration);
        }

        [Test]
        public void Validate_NoIssues() {
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = true, CanSetTemperature = true });

            var sut = new CoolCamera(cameraMediatorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeTrue();

            sut.Issues.Should().BeEmpty();
        }

        [Test]
        public void Validate_NotConnected_OneIssue() {
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = false });

            var sut = new CoolCamera(cameraMediatorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeFalse();

            sut.Issues.Should().HaveCount(1);
        }

        [Test]
        public void Validate_CanNotSetTemperature_OneIssue() {
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = true, CanSetTemperature = false });

            var sut = new CoolCamera(cameraMediatorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeFalse();

            sut.Issues.Should().HaveCount(1);
        }

        [Test]
        [TestCase(-10.5, 10)]
        [TestCase(10.5, 10)]
        [TestCase(-10.5, 0)]
        [TestCase(10.5, 0)]
        [TestCase(0, 0)]
        [TestCase(0, 10)]
        public async Task Execute_NoIssues_LogicCalled(double temperature, int duration) {
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = true, CanSetTemperature = true });

            var sut = new CoolCamera(cameraMediatorMock.Object);
            sut.Temperature = temperature;
            sut.Duration = duration;
            await sut.Execute(default, default);

            cameraMediatorMock.Verify(x => x.CoolCamera(It.Is<double>(temp => temp == temperature), It.Is<TimeSpan>(t => t == TimeSpan.FromMinutes(duration)), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(false, false)]
        public Task Execute_HasIssues_LogicNotCalled(bool connected, bool canTemp) {
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = connected, CanSetTemperature = canTemp });

            var sut = new CoolCamera(cameraMediatorMock.Object);
            sut.Temperature = 10;
            sut.Duration = 10;
            Func<Task> act = () => { return sut.Execute(default, default); };

            cameraMediatorMock.Verify(x => x.CoolCamera(It.IsAny<double>(), It.IsAny<TimeSpan>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Never);
            return act.Should().ThrowAsync<SequenceItemSkippedException>(string.Join(",", sut.Issues));
        }

        [Test]
        [TestCase(0, 1)]
        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(30, 30)]
        public void GetEstimatedDuration_BasedOnParameters_ReturnsCorrectEstimate(int minutes, int expected) {
            var sut = new CoolCamera(cameraMediatorMock.Object);
            sut.Duration = minutes;

            var duration = sut.GetEstimatedDuration();

            duration.Should().Be(TimeSpan.FromMinutes(expected));
        }
    }
}