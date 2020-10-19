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
using NINA.Model.ImageData;
using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Model.MyFocuser;
using NINA.Profile;
using NINA.Sequencer.Trigger.Autofocus;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.WindowService;
using NINA.ViewModel;
using NINA.ViewModel.AutoFocus;
using NINA.ViewModel.ImageHistory;
using NINA.ViewModel.Interfaces;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace NINATest.Sequencer.Trigger.Autofocus {

    [TestFixture]
    public class AutofocusAfterHFRIncreaseTriggerTest {
        private Mock<IProfileService> profileServiceMock;
        private Mock<IImageHistoryVM> historyMock;
        private Mock<ICameraMediator> cameraMediatorMock;
        private Mock<IFilterWheelMediator> filterWheelMediatorMock;
        private Mock<IFocuserMediator> focuserMediatorMock;
        private Mock<IGuiderMediator> guiderMediatorMock;
        private Mock<IImagingMediator> imagingMediatorMock;
        private Mock<IApplicationStatusMediator> applicationStatusMediatorMock;

        [SetUp]
        public void Setup() {
            profileServiceMock = new Mock<IProfileService>();
            historyMock = new Mock<IImageHistoryVM>();
            cameraMediatorMock = new Mock<ICameraMediator>();
            filterWheelMediatorMock = new Mock<IFilterWheelMediator>();
            focuserMediatorMock = new Mock<IFocuserMediator>();
            guiderMediatorMock = new Mock<IGuiderMediator>();
            imagingMediatorMock = new Mock<IImagingMediator>();
            applicationStatusMediatorMock = new Mock<IApplicationStatusMediator>();
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new NINA.Model.MyCamera.CameraInfo { Connected = true });
            focuserMediatorMock.Setup(x => x.GetInfo()).Returns(new NINA.Model.MyFocuser.FocuserInfo { Connected = true });
        }

        [Test]
        public void CloneTest() {
            var initial = new AutofocusAfterHFRIncreaseTrigger(profileServiceMock.Object, historyMock.Object, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object, applicationStatusMediatorMock.Object);
            initial.Icon = new System.Windows.Media.GeometryGroup();

            var sut = (AutofocusAfterHFRIncreaseTrigger)initial.Clone();

            sut.Should().NotBeSameAs(initial);
            sut.Icon.Should().BeSameAs(initial.Icon);
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void ShouldTrigger_HistoryNotLargeEnough_False(int sampleSize) {
            var history = new List<ImageHistoryPoint>();
            for (int i = 0; i < sampleSize; i++) {
                history.Add(new ImageHistoryPoint(0, null));
            }
            historyMock.SetupGet(x => x.ImageHistory).Returns(history);

            var sut = new AutofocusAfterHFRIncreaseTrigger(profileServiceMock.Object, historyMock.Object, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object, applicationStatusMediatorMock.Object);
            sut.Amount = 1;

            var trigger = sut.ShouldTrigger(null);

            trigger.Should().BeFalse();
        }

        [Test]
        [TestCase(new double[] { 3, 3, 3, 10 }, 1, true)]
        [TestCase(new double[] { 3, 3, 3, 3 }, 1, false)]
        [TestCase(new double[] { 3, 3.1, 2.9, 3 }, 1, false)]
        [TestCase(new double[] { 3, 3.1, 3.2, 3.3 }, 1, true)]
        [TestCase(new double[] { 3, 3.1, 3.2, 3.3 }, 50, false)]
        [TestCase(new double[] { 3, 2.9, 2.8, 2.7 }, 1, false)]
        public void ShouldTrigger_HistoryExists_NoPreviousAFs_True(double[] hfrs, double changeAmount, bool shouldTrigger) {
            var history = new List<ImageHistoryPoint>();
            for (int i = 0; i < hfrs.Length; i++) {
                var p = new ImageHistoryPoint(i, null);
                p.PopulateSDPoint(new StarDetectionAnalysis() { DetectedStars = i, HFR = hfrs[i] });
                history.Add(p);
            }
            historyMock.SetupGet(x => x.ImageHistory).Returns(history);
            historyMock.SetupGet(x => x.AutoFocusPoints).Returns(new NINA.Utility.AsyncObservableCollection<ImageHistoryPoint>());

            var sut = new AutofocusAfterHFRIncreaseTrigger(profileServiceMock.Object, historyMock.Object, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object, applicationStatusMediatorMock.Object);
            sut.Amount = changeAmount;

            var trigger = sut.ShouldTrigger(null);

            trigger.Should().Be(shouldTrigger);
        }

        [Test]
        [TestCase(new double[] { 3, 3, 3, 10 }, 1, true)]
        [TestCase(new double[] { 3, 3, 3, 3 }, 1, false)]
        [TestCase(new double[] { 3, 3.1, 2.9, 3 }, 1, false)]
        [TestCase(new double[] { 3, 3.1, 3.2, 3.3 }, 1, true)]
        [TestCase(new double[] { 3, 3.1, 3.2, 3.3 }, 50, false)]
        [TestCase(new double[] { 3, 2.9, 2.8, 2.7 }, 1, false)]
        public void ShouldTrigger_HistoryExists_PreviousAFsExists_True(double[] hfrs, double changeAmount, bool shouldTrigger) {
            var history = new List<ImageHistoryPoint>();
            history.Add(new ImageHistoryPoint(0, null));
            history.Add(new ImageHistoryPoint(1, null));
            history.Add(new ImageHistoryPoint(2, null));
            history.Add(new ImageHistoryPoint(3, null));

            var afPoint = new ImageHistoryPoint(4, null);
            afPoint.PopulateAFPoint(new NINA.ViewModel.AutoFocus.AutoFocusReport() {
                InitialFocusPoint = new NINA.ViewModel.AutoFocus.FocusPoint() { Position = 1000 },
                CalculatedFocusPoint = new NINA.ViewModel.AutoFocus.FocusPoint() { Position = 1200 },
                Temperature = 10,
                Timestamp = DateTime.Now
            });
            history.Add(afPoint);
            for (int i = 0; i < hfrs.Length; i++) {
                var p = new ImageHistoryPoint(i, null);
                p.PopulateSDPoint(new StarDetectionAnalysis() { DetectedStars = i + 5, HFR = hfrs[i] });
                history.Add(p);
            }
            historyMock.SetupGet(x => x.ImageHistory).Returns(history);
            historyMock.SetupGet(x => x.AutoFocusPoints).Returns(new NINA.Utility.AsyncObservableCollection<ImageHistoryPoint>() { afPoint });

            var sut = new AutofocusAfterHFRIncreaseTrigger(profileServiceMock.Object, historyMock.Object, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object, applicationStatusMediatorMock.Object);
            sut.Amount = changeAmount;

            var trigger = sut.ShouldTrigger(null);

            trigger.Should().Be(shouldTrigger);
        }

        [Test]
        public async Task Execute_Successfully_WithAllParametersPassedCorrectly() {
            var report = new AutoFocusReport();

            var filter = new FilterInfo() { Position = 0 };
            filterWheelMediatorMock.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo() { SelectedFilter = filter });

            var sut = new AutofocusAfterHFRIncreaseTrigger(profileServiceMock.Object, historyMock.Object, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object, applicationStatusMediatorMock.Object);

            await sut.Execute(default, default, default);

            // Todo proper assertion
            // historyMock.Verify(h => h.AppendAutoFocusPoint(It.Is<AutoFocusReport>(r => r == report)), Times.Once);
        }

        [Test]
        public void ToString_FilledProperly() {
            var sut = new AutofocusAfterHFRIncreaseTrigger(profileServiceMock.Object, historyMock.Object, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object, applicationStatusMediatorMock.Object);
            var tostring = sut.ToString();
            tostring.Should().Be("Trigger: AutofocusAfterHFRIncreaseTrigger, Amount: 5");
        }
    }
}