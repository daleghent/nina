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
using NINA.Image.ImageData;
using NINA.Sequencer.Trigger.Guider;
using NINA.Equipment.Interfaces.Mediator;
using NINA.ViewModel.ImageHistory;
using NINA.ViewModel.Interfaces;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Equipment.Equipment.MyGuider;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.Model;

namespace NINATest.Sequencer.Trigger.Guider {

    [TestFixture]
    public class DitherAfterExposuresTest {
        private Mock<IImageHistoryVM> historyMock;
        private Mock<IGuiderMediator> guiderMediatorMock;

        [SetUp]
        public void Setup() {
            historyMock = new Mock<IImageHistoryVM>();
            guiderMediatorMock = new Mock<IGuiderMediator>();
            guiderMediatorMock.Setup(x => x.GetInfo()).Returns(new GuiderInfo() { Connected = true });
        }

        [Test]
        public void CloneTest() {
            var initial = new DitherAfterExposures(guiderMediatorMock.Object, historyMock.Object);
            initial.Icon = new System.Windows.Media.GeometryGroup();

            var sut = (DitherAfterExposures)initial.Clone();

            sut.Should().NotBeSameAs(initial);
            sut.Icon.Should().BeSameAs(initial.Icon);
        }

        [Test]
        public async Task ExecuteTest() {
            historyMock.SetupGet(x => x.ImageHistory).Returns(new List<ImageHistoryPoint>());

            var sut = new DitherAfterExposures(guiderMediatorMock.Object, historyMock.Object);
            await sut.Execute(default, default, default);

            guiderMediatorMock.Verify(x => x.Dither(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void InitializeTest() {
            var sut = new DitherAfterExposures(guiderMediatorMock.Object, historyMock.Object);
            sut.Initialize();

            Assert.Pass();
        }

        [Test]
        [TestCase(0, 1, false)]
        [TestCase(1, 1, true)]
        [TestCase(0, 3, false)]
        [TestCase(1, 3, false)]
        [TestCase(2, 3, false)]
        [TestCase(3, 3, true)]
        [TestCase(4, 3, false)]
        [TestCase(5, 3, false)]
        [TestCase(6, 3, true)]
        [TestCase(7, 3, false)]
        [TestCase(8, 3, false)]
        [TestCase(9, 3, true)]
        [TestCase(0, 10, false)]
        [TestCase(10, 10, true)]
        [TestCase(15, 10, false)]
        [TestCase(20, 10, true)]
        [TestCase(22, 10, false)]
        [TestCase(27, 10, false)]
        [TestCase(30, 10, true)]
        public void ShouldTrigger_HistoryExists_NoPreviousAFs_True(int historyItems, int afterExpsoures, bool shouldTrigger) {
            var history = new List<ImageHistoryPoint>();
            for (int i = 0; i < historyItems; i++) {
                history.Add(new ImageHistoryPoint(i, null, "LIGHT"));
            }
            historyMock.SetupGet(x => x.ImageHistory).Returns(history);

            var sut = new DitherAfterExposures(guiderMediatorMock.Object, historyMock.Object);
            sut.AfterExposures = afterExpsoures;

            var trigger = sut.ShouldTrigger(null);

            trigger.Should().Be(shouldTrigger);
        }

        [Test]
        public async Task ShouldTrigger_WithExecute_FlowTest() {
            var history = new List<ImageHistoryPoint>();
            for (int i = 0; i < 10; i++) {
                history.Add(new ImageHistoryPoint(i, null, "LIGHT"));
            }
            historyMock.SetupGet(x => x.ImageHistory).Returns(history);

            var sut = new DitherAfterExposures(guiderMediatorMock.Object, historyMock.Object);
            sut.AfterExposures = 1;

            var test1 = sut.ShouldTrigger(null);
            await sut.Execute(default, default, default);
            var test2 = sut.ShouldTrigger(null);

            history.Add(new ImageHistoryPoint(100, null, "LIGHT"));
            var test3 = sut.ShouldTrigger(null);

            test1.Should().BeTrue();
            test2.Should().BeFalse();
            test3.Should().BeTrue();
        }
    }
}