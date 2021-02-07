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
using NINA.Model.MyFilterWheel;
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace NINATest.Sequencer.Trigger.Autofocus {

    [TestFixture]
    public class AutofocusAfterTemperatureChangeTriggerTest {
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
            var initial = new AutofocusAfterTemperatureChangeTrigger(profileServiceMock.Object, historyMock.Object, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object, applicationStatusMediatorMock.Object);
            initial.Icon = new System.Windows.Media.GeometryGroup();

            var sut = (AutofocusAfterTemperatureChangeTrigger)initial.Clone();

            sut.Should().NotBeSameAs(initial);
            sut.Icon.Should().BeSameAs(initial.Icon);
        }

        [Test]
        [TestCase(40, 1, 40, false)]
        [TestCase(40, 1, 41, true)]
        [TestCase(40, 1, 39, true)]
        [TestCase(50, 10, 40, true)]
        [TestCase(50, 5, 52, false)]
        [TestCase(50, 5, 48, false)]
        public void ShouldTrigger_LastAFExists(double initialTemp, double tempAmount, double changedTemp, bool shouldTrigger) {
            var afHistory = new NINA.Utility.AsyncObservableCollection<ImageHistoryPoint>();
            var report = new AutoFocusReport() { Temperature = initialTemp };
            var point = new ImageHistoryPoint(0, null, "LIGHT");
            point.PopulateAFPoint(report);
            afHistory.Add(point);
            historyMock.SetupGet(x => x.AutoFocusPoints).Returns(afHistory);

            focuserMediatorMock.Setup(x => x.GetInfo()).Returns(new NINA.Model.MyFocuser.FocuserInfo() { Temperature = changedTemp });

            var sut = new AutofocusAfterTemperatureChangeTrigger(profileServiceMock.Object, historyMock.Object, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object, applicationStatusMediatorMock.Object);
            sut.Amount = tempAmount;

            var trigger = sut.ShouldTrigger(null);

            trigger.Should().Be(shouldTrigger);
        }

        [Test]
        [TestCase(40, 1, 40, false)]
        [TestCase(40, 1, 41, true)]
        [TestCase(40, 1, 39, true)]
        [TestCase(50, 10, 40, true)]
        [TestCase(50, 5, 52, false)]
        [TestCase(50, 5, 48, false)]
        public void ShouldTrigger_LastAFDoesNotExist(double initialTemp, double tempAmount, double changedTemp, bool shouldTrigger) {
            historyMock.SetupGet(x => x.AutoFocusPoints).Returns(new NINA.Utility.AsyncObservableCollection<ImageHistoryPoint>());

            focuserMediatorMock.SetupSequence(x => x.GetInfo())
                .Returns(new NINA.Model.MyFocuser.FocuserInfo() { Connected = true, Temperature = initialTemp })
                .Returns(new NINA.Model.MyFocuser.FocuserInfo() { Temperature = initialTemp })
                .Returns(new NINA.Model.MyFocuser.FocuserInfo() { Temperature = changedTemp });

            var sut = new AutofocusAfterTemperatureChangeTrigger(profileServiceMock.Object, historyMock.Object, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object, applicationStatusMediatorMock.Object);
            sut.Initialize();
            sut.Amount = tempAmount;

            var trigger = sut.ShouldTrigger(null);

            trigger.Should().Be(shouldTrigger);
        }

        [Test]
        public async Task Execute_Successfully_WithAllParametersPassedCorrectly() {
            var report = new AutoFocusReport();

            var filter = new FilterInfo() { Position = 0 };
            filterWheelMediatorMock.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo() { SelectedFilter = filter });

            var sut = new AutofocusAfterTemperatureChangeTrigger(profileServiceMock.Object, historyMock.Object, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object, applicationStatusMediatorMock.Object);

            await sut.Execute(default, default, default);

            // Todo proper assertion
            // historyMock.Verify(h => h.AppendAutoFocusPoint(It.Is<AutoFocusReport>(r => r == report)), Times.Once);
        }

        [Test]
        public void ToString_FilledProperly() {
            var sut = new AutofocusAfterTemperatureChangeTrigger(profileServiceMock.Object, historyMock.Object, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object, applicationStatusMediatorMock.Object);
            var tostring = sut.ToString();
            tostring.Should().Be("Trigger: AutofocusAfterTemperatureChangeTrigger, Amount: 5°");
        }
    }
}