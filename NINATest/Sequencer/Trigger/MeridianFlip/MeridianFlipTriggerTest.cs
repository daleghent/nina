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
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger.MeridianFlip;
using NINA.Utility.Mediator.Interfaces;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINATest.Sequencer.Trigger.MeridianFlip {

    [TestFixture]
    public class MeridianFlipTriggerTest {
        private Mock<IProfileService> profileServiceMock;
        private Mock<ITelescopeMediator> telescopeMediatorMock;
        private Mock<IGuiderMediator> guiderMediatorMock;
        private Mock<IImagingMediator> imagingMediatorMock;
        private Mock<IApplicationStatusMediator> applicationStatusMediatorMock;
        private Mock<IFilterWheelMediator> filterMediatorMock;
        private Mock<IFocuserMediator> focuserMediatorMock;
        private Mock<ICameraMediator> cameraMediatorMock;

        [SetUp]
        public void Setup() {
            profileServiceMock = new Mock<IProfileService>();
            telescopeMediatorMock = new Mock<ITelescopeMediator>();
            guiderMediatorMock = new Mock<IGuiderMediator>();
            imagingMediatorMock = new Mock<IImagingMediator>();
            applicationStatusMediatorMock = new Mock<IApplicationStatusMediator>();
            filterMediatorMock = new Mock<IFilterWheelMediator>();
            cameraMediatorMock = new Mock<ICameraMediator>();
            focuserMediatorMock = new Mock<IFocuserMediator>();
        }

        [Test]
        public void CloneTest() {
            var initial = new MeridianFlipTrigger(profileServiceMock.Object, cameraMediatorMock.Object, telescopeMediatorMock.Object, guiderMediatorMock.Object, focuserMediatorMock.Object, imagingMediatorMock.Object, applicationStatusMediatorMock.Object, filterMediatorMock.Object);
            initial.Icon = new System.Windows.Media.GeometryGroup();

            var sut = (MeridianFlipTrigger)initial.Clone();

            sut.Should().NotBeSameAs(initial);
            sut.Icon.Should().BeSameAs(initial.Icon);
        }

        [Test]
        public void ShouldTrigger_TimeToMeridianZero_True() {
            var sut = new MeridianFlipTrigger(profileServiceMock.Object, cameraMediatorMock.Object, telescopeMediatorMock.Object, guiderMediatorMock.Object, focuserMediatorMock.Object, imagingMediatorMock.Object, applicationStatusMediatorMock.Object, filterMediatorMock.Object);

            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new NINA.Model.MyTelescope.TelescopeInfo() {
                Connected = true,
                TimeToMeridianFlip = 0
            });

            profileServiceMock.SetupGet(x => x.ActiveProfile.MeridianFlipSettings.Enabled).Returns(true);
            profileServiceMock.SetupGet(x => x.ActiveProfile.MeridianFlipSettings.UseSideOfPier).Returns(false);

            var itemMock = new Mock<ISequenceItem>();
            itemMock.Setup(x => x.GetEstimatedDuration()).Returns(TimeSpan.Zero);

            var should = sut.ShouldTrigger(itemMock.Object);

            should.Should().BeTrue();
        }

        [Test]
        public void ShouldTrigger_TimeToMeridianLarge_False() {
            var sut = new MeridianFlipTrigger(profileServiceMock.Object, cameraMediatorMock.Object, telescopeMediatorMock.Object, guiderMediatorMock.Object, focuserMediatorMock.Object, imagingMediatorMock.Object, applicationStatusMediatorMock.Object, filterMediatorMock.Object);

            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new NINA.Model.MyTelescope.TelescopeInfo() {
                Connected = true,
                TimeToMeridianFlip = 1000
            });

            profileServiceMock.SetupGet(x => x.ActiveProfile.MeridianFlipSettings.Enabled).Returns(true);
            profileServiceMock.SetupGet(x => x.ActiveProfile.MeridianFlipSettings.UseSideOfPier).Returns(false);

            var itemMock = new Mock<ISequenceItem>();
            itemMock.Setup(x => x.GetEstimatedDuration()).Returns(TimeSpan.Zero);

            var should = sut.ShouldTrigger(itemMock.Object);

            should.Should().BeFalse();
        }

        [Test]
        public void ShouldTrigger_TimeToMeridianLarge_ButSequenceItemDurationLarger_True() {
            var sut = new MeridianFlipTrigger(profileServiceMock.Object, cameraMediatorMock.Object, telescopeMediatorMock.Object, guiderMediatorMock.Object, focuserMediatorMock.Object, imagingMediatorMock.Object, applicationStatusMediatorMock.Object, filterMediatorMock.Object);

            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new NINA.Model.MyTelescope.TelescopeInfo() {
                Connected = true,
                TimeToMeridianFlip = 5
            });

            profileServiceMock.SetupGet(x => x.ActiveProfile.MeridianFlipSettings.Enabled).Returns(true);
            profileServiceMock.SetupGet(x => x.ActiveProfile.MeridianFlipSettings.UseSideOfPier).Returns(false);

            var itemMock = new Mock<ISequenceItem>();
            itemMock.Setup(x => x.GetEstimatedDuration()).Returns(TimeSpan.FromHours(10));

            var should = sut.ShouldTrigger(itemMock.Object);

            should.Should().BeTrue();
        }
    }
}