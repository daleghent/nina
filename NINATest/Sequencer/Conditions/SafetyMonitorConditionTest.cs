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
using NINA.Sequencer.Conditions;
using NINA.Utility.Mediator.Interfaces;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINATest.Sequencer.Conditions {

    [TestFixture]
    public class SafetyMonitorConditionTest {
        private Mock<ISafetyMonitorMediator> monitorMock;

        [SetUp]
        public void Setup() {
            monitorMock = new Mock<ISafetyMonitorMediator>();
        }

        [Test]
        public void SafetyMonitorConditionTest_Clone_GoodClone() {
            var sut = new SafetyMonitorCondition(monitorMock.Object);
            sut.Icon = new System.Windows.Media.GeometryGroup();
            var item2 = (SafetyMonitorCondition)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Icon.Should().BeSameAs(sut.Icon);
        }

        [Test]
        public void Check_True_WhenConnectedAndSafe() {
            var sut = new SafetyMonitorCondition(monitorMock.Object);
            monitorMock.Setup(x => x.GetInfo()).Returns(new NINA.Model.MySafetyMonitor.SafetyMonitorInfo() { Connected = true, IsSafe = true });

            sut.Check(null).Should().BeTrue();
            sut.IsSafe.Should().BeTrue();
        }

        [Test]
        public void Check_False_WhenNotConnected() {
            var sut = new SafetyMonitorCondition(monitorMock.Object);
            monitorMock.Setup(x => x.GetInfo()).Returns(new NINA.Model.MySafetyMonitor.SafetyMonitorInfo() { Connected = false });

            sut.Check(null).Should().BeFalse();
            sut.IsSafe.Should().BeFalse();
        }

        [Test]
        public void Check_False_WhenConnectedAndNotSafe() {
            var sut = new SafetyMonitorCondition(monitorMock.Object);
            monitorMock.Setup(x => x.GetInfo()).Returns(new NINA.Model.MySafetyMonitor.SafetyMonitorInfo() { Connected = true, IsSafe = false });

            sut.Check(null).Should().BeFalse();
            sut.IsSafe.Should().BeFalse();
        }
    }
}