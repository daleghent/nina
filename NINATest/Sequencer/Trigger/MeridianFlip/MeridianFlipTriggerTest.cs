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
using NINA.Model.MyTelescope;
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

            profileServiceMock.SetupGet(x => x.ActiveProfile.MeridianFlipSettings.UseSideOfPier).Returns(false);

            var itemMock = new Mock<ISequenceItem>();
            itemMock.Setup(x => x.GetEstimatedDuration()).Returns(TimeSpan.Zero);

            var should = sut.ShouldTrigger(itemMock.Object);

            should.Should().BeTrue();
        }

        [Test]
        public void ShouldTrigger_TimeToMeridianLarge_ButSequenceItemDurationLarger_True() {
            var sut = new MeridianFlipTrigger(profileServiceMock.Object, cameraMediatorMock.Object, telescopeMediatorMock.Object, guiderMediatorMock.Object, focuserMediatorMock.Object, imagingMediatorMock.Object, applicationStatusMediatorMock.Object, filterMediatorMock.Object);

            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new NINA.Model.MyTelescope.TelescopeInfo() {
                Connected = true,
                TimeToMeridianFlip = 5
            });

            profileServiceMock.SetupGet(x => x.ActiveProfile.MeridianFlipSettings.UseSideOfPier).Returns(false);

            var itemMock = new Mock<ISequenceItem>();
            itemMock.Setup(x => x.GetEstimatedDuration()).Returns(TimeSpan.FromHours(10));

            var should = sut.ShouldTrigger(itemMock.Object);

            should.Should().BeTrue();
        }

        [Test]
        public void ShouldFlip_NoTelescopeConnected_UnableToFlip() {
            var sut = new MeridianFlipTrigger(profileServiceMock.Object, cameraMediatorMock.Object, telescopeMediatorMock.Object, guiderMediatorMock.Object, focuserMediatorMock.Object, imagingMediatorMock.Object, applicationStatusMediatorMock.Object, filterMediatorMock.Object);
            profileServiceMock.SetupGet(m => m.ActiveProfile.MeridianFlipSettings).Returns(new Mock<IMeridianFlipSettings>().Object);

            var telescopeInfo = new TelescopeInfo() {
                TimeToMeridianFlip = 10,
                Connected = false
            };
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(telescopeInfo);

            var nextItemMock = new Mock<ISequenceItem>();
            nextItemMock.Setup(x => x.GetEstimatedDuration()).Returns(TimeSpan.FromMinutes(6000));

            sut.ShouldTrigger(nextItemMock.Object).Should().BeFalse();
        }

        [Test]
        public void ShouldFlip_TelescopeConnectedButNaNTime_UnableToFlip() {
            var sut = new MeridianFlipTrigger(profileServiceMock.Object, cameraMediatorMock.Object, telescopeMediatorMock.Object, guiderMediatorMock.Object, focuserMediatorMock.Object, imagingMediatorMock.Object, applicationStatusMediatorMock.Object, filterMediatorMock.Object);
            profileServiceMock.SetupGet(m => m.ActiveProfile.MeridianFlipSettings).Returns(new Mock<IMeridianFlipSettings>().Object);

            var telescopeInfo = new TelescopeInfo() {
                TimeToMeridianFlip = double.NaN,
                Connected = true
            };
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(telescopeInfo);

            var nextItemMock = new Mock<ISequenceItem>();
            nextItemMock.Setup(x => x.GetEstimatedDuration()).Returns(TimeSpan.FromMinutes(6000));

            sut.ShouldTrigger(nextItemMock.Object).Should().BeFalse();
        }

        [Test]
        public void ShouldFlip_LastFlipHappenedAlready_NoFlip() {
            //todo
        }

        [Test]
        [TestCase(5, 5, -1, true)]
        [TestCase(5, 5, 0, true)]
        [TestCase(5, 5, 2, true)]
        [TestCase(5, 5, 4, true)]
        [TestCase(5, 5, 5, true)]
        [TestCase(5, 10, 8, false)]
        [TestCase(5, 10, 10, false)]
        [TestCase(5, 10, 11, false)]
        public void ShouldFlip_BetweenMinimumAndMaximumTime_NoPause_NoPierSide_FlipWhenExpected(double minTimeToFlip, double maxTimeToFlip, double remainingTimeToFlip, bool expectToFlip) {
            var sut = new MeridianFlipTrigger(profileServiceMock.Object, cameraMediatorMock.Object, telescopeMediatorMock.Object, guiderMediatorMock.Object, focuserMediatorMock.Object, imagingMediatorMock.Object, applicationStatusMediatorMock.Object, filterMediatorMock.Object);

            var settings = new Mock<IMeridianFlipSettings>();
            settings.SetupGet(m => m.MinutesAfterMeridian).Returns(minTimeToFlip);
            settings.SetupGet(m => m.MaxMinutesAfterMeridian).Returns(maxTimeToFlip);
            settings.SetupGet(m => m.PauseTimeBeforeMeridian).Returns(0);
            settings.SetupGet(m => m.UseSideOfPier).Returns(false);

            profileServiceMock.SetupGet(m => m.ActiveProfile.MeridianFlipSettings).Returns(settings.Object);

            var telescopeInfo = new TelescopeInfo() {
                TimeToMeridianFlip = TimeSpan.FromMinutes(remainingTimeToFlip).TotalHours,
                Connected = true
            };
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(telescopeInfo);

            var nextItemMock = new Mock<ISequenceItem>();
            nextItemMock.Setup(x => x.GetEstimatedDuration()).Returns(TimeSpan.FromMinutes(minTimeToFlip));

            sut.ShouldTrigger(nextItemMock.Object).Should().Be(expectToFlip);
        }

        [Test]
        [TestCase(5, 5, -1, PierSide.pierWest, true)]
        [TestCase(5, 5, 0, PierSide.pierWest, true)]
        [TestCase(5, 5, 2, PierSide.pierWest, true)]
        [TestCase(5, 5, 4, PierSide.pierWest, true)]
        [TestCase(5, 5, 5, PierSide.pierWest, true)]
        [TestCase(5, 10, 8, PierSide.pierWest, false)]
        [TestCase(5, 10, 10, PierSide.pierWest, false)]
        [TestCase(5, 10, 11, PierSide.pierWest, false)]
        /* Same tests as before, but with pier side East, therefore no flip is expected in each case */
        [TestCase(5, 5, -1, PierSide.pierEast, false)]
        [TestCase(5, 5, 0, PierSide.pierEast, false)]
        [TestCase(5, 5, 2, PierSide.pierEast, false)]
        [TestCase(5, 5, 4, PierSide.pierEast, false)]
        [TestCase(5, 5, 5, PierSide.pierEast, false)]
        [TestCase(5, 10, 8, PierSide.pierEast, false)]
        [TestCase(5, 10, 10, PierSide.pierEast, false)]
        [TestCase(5, 10, 11, PierSide.pierEast, false)]
        public void ShouldFlip_BetweenMinimumAndMaximumTime_NoPause_UsePierSide_FlipWhenExpected(double minTimeToFlip, double maxTimeToFlip, double remainingTimeToFlip, PierSide pierSide, bool expectToFlip) {
            var sut = new MeridianFlipTrigger(profileServiceMock.Object, cameraMediatorMock.Object, telescopeMediatorMock.Object, guiderMediatorMock.Object, focuserMediatorMock.Object, imagingMediatorMock.Object, applicationStatusMediatorMock.Object, filterMediatorMock.Object);

            var settings = new Mock<IMeridianFlipSettings>();
            settings.SetupGet(m => m.MinutesAfterMeridian).Returns(minTimeToFlip);
            settings.SetupGet(m => m.MaxMinutesAfterMeridian).Returns(maxTimeToFlip);
            settings.SetupGet(m => m.PauseTimeBeforeMeridian).Returns(0);
            settings.SetupGet(m => m.UseSideOfPier).Returns(true);

            profileServiceMock.SetupGet(m => m.ActiveProfile.MeridianFlipSettings).Returns(settings.Object);

            var telescopeInfo = new TelescopeInfo() {
                TimeToMeridianFlip = TimeSpan.FromMinutes(remainingTimeToFlip).TotalHours,
                SideOfPier = pierSide,
                Connected = true
            };
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(telescopeInfo);

            var nextItemMock = new Mock<ISequenceItem>();
            nextItemMock.Setup(x => x.GetEstimatedDuration()).Returns(TimeSpan.FromMinutes(minTimeToFlip));

            sut.ShouldTrigger(nextItemMock.Object).Should().Be(expectToFlip);
        }

        [Test]
        /* Exposure time is 7 minutes
         * Remaining time to exceed minimum time is 3 minutes
         * Remaining time to exceed maximum time is 8 minutes
         * => The exposure still fits in, no flip yet
         */
        [TestCase(7, 5, 10, 8, PierSide.pierWest, false)]
        /* Exposure time is 9 minutes
         * Remaining time to exceed minimum time is 3 minutes
         * Remaining time to exceed maximum time is 8 minutes
         * => The exposure does not fit, flip needs to start with a wait time
         */
        [TestCase(9, 5, 10, 8, PierSide.pierWest, true)]
        /* Same Test as before, but pier side is already correct and no flip required */
        [TestCase(9, 5, 10, 8, PierSide.pierEast, false)]
        public void ShouldFlip_BeforeMinimumTime_NoPause_PierSideIsUsed_EvaluateIfFlipIsNecessary(double nextItemExpectedTime, double minTimeToFlip, double maxTimeToFlip, double remainingTimeToFlip, PierSide pierSide, bool expectToFlip) {
            var sut = new MeridianFlipTrigger(profileServiceMock.Object, cameraMediatorMock.Object, telescopeMediatorMock.Object, guiderMediatorMock.Object, focuserMediatorMock.Object, imagingMediatorMock.Object, applicationStatusMediatorMock.Object, filterMediatorMock.Object);

            var settings = new Mock<IMeridianFlipSettings>();
            settings.SetupGet(m => m.MinutesAfterMeridian).Returns(minTimeToFlip);
            settings.SetupGet(m => m.MaxMinutesAfterMeridian).Returns(maxTimeToFlip);
            settings.SetupGet(m => m.PauseTimeBeforeMeridian).Returns(0);
            settings.SetupGet(m => m.UseSideOfPier).Returns(true);

            profileServiceMock.SetupGet(m => m.ActiveProfile.MeridianFlipSettings).Returns(settings.Object);

            var telescopeInfo = new TelescopeInfo() {
                TimeToMeridianFlip = TimeSpan.FromMinutes(remainingTimeToFlip).TotalHours,
                SideOfPier = pierSide,
                Connected = true
            };
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(telescopeInfo);

            var nextItemMock = new Mock<ISequenceItem>();
            nextItemMock.Setup(x => x.GetEstimatedDuration()).Returns(TimeSpan.FromMinutes(nextItemExpectedTime));

            sut.ShouldTrigger(nextItemMock.Object).Should().Be(expectToFlip);
        }

        [Test]
        /* Exposure time is 7 minutes
         * Remaining time to exceed minimum time is 3 minutes
         * Remaining time to exceed maximum time is 8 minutes
         * => The exposure still fits in, no flip yet
         */
        [TestCase(7, 5, 10, 8, PierSide.pierWest, false)]
        /* Exposure time is 9 minutes
         * Remaining time to exceed minimum time is 3 minutes
         * Remaining time to exceed maximum time is 8 minutes
         * => The exposure does not fit, flip needs to start with a wait time
         */
        [TestCase(9, 5, 10, 8, PierSide.pierWest, true)]
        /* Same Test as before, but pier side is already correct, however the pier side should not be considered and a flip is required*/
        [TestCase(9, 5, 10, 8, PierSide.pierEast, true)]
        public void ShouldFlip_BeforeMinimumTime_NoPause_PierSideIsNOTUsed_EvaluateIfFlipIsNecessary(double nextItemExpectedTime, double minTimeToFlip, double maxTimeToFlip, double remainingTimeToFlip, PierSide pierSide, bool expectToFlip) {
            var sut = new MeridianFlipTrigger(profileServiceMock.Object, cameraMediatorMock.Object, telescopeMediatorMock.Object, guiderMediatorMock.Object, focuserMediatorMock.Object, imagingMediatorMock.Object, applicationStatusMediatorMock.Object, filterMediatorMock.Object);

            var settings = new Mock<IMeridianFlipSettings>();
            settings.SetupGet(m => m.MinutesAfterMeridian).Returns(minTimeToFlip);
            settings.SetupGet(m => m.MaxMinutesAfterMeridian).Returns(maxTimeToFlip);
            settings.SetupGet(m => m.PauseTimeBeforeMeridian).Returns(0);
            settings.SetupGet(m => m.UseSideOfPier).Returns(false);

            profileServiceMock.SetupGet(m => m.ActiveProfile.MeridianFlipSettings).Returns(settings.Object);

            var telescopeInfo = new TelescopeInfo() {
                TimeToMeridianFlip = TimeSpan.FromMinutes(remainingTimeToFlip).TotalHours,
                SideOfPier = pierSide,
                Connected = true
            };
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(telescopeInfo);

            var nextItemMock = new Mock<ISequenceItem>();
            nextItemMock.Setup(x => x.GetEstimatedDuration()).Returns(TimeSpan.FromMinutes(nextItemExpectedTime));

            sut.ShouldTrigger(nextItemMock.Object).Should().Be(expectToFlip);
        }
    }
}