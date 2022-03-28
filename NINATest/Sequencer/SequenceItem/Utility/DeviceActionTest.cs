#region "copyright"

/*
    Copyright © 2016 - 2022 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using Moq;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Sequencer;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;

namespace NINATest.Sequencer.SequenceItem.Utility {

    [TestFixture]
    internal class DeviceActionTest {
        public Mock<ICameraMediator> cameraMediatorMock;
        public Mock<IDomeMediator> domeMediatorMock;
        public Mock<IFilterWheelMediator> fwMediatorMock;
        public Mock<IFlatDeviceMediator> fdMediatorMock;
        public Mock<IFocuserMediator> focuserMediatorMock;
        public Mock<IGuiderMediator> guiderMediatorMock;
        public Mock<IRotatorMediator> rotatorMediatorMock;
        public Mock<ISafetyMonitorMediator> sdMediatorMock;
        public Mock<ISwitchMediator> switchMediatorMock;
        public Mock<ITelescopeMediator> telescopeMediatorMock;
        public Mock<IWeatherDataMediator> wdMediatorMock;

        [SetUp]
        public void Setup() {
            cameraMediatorMock = new Mock<ICameraMediator>();
            domeMediatorMock = new Mock<IDomeMediator>();
            fwMediatorMock = new Mock<IFilterWheelMediator>();
            fdMediatorMock = new Mock<IFlatDeviceMediator>();
            focuserMediatorMock = new Mock<IFocuserMediator>();
            guiderMediatorMock = new Mock<IGuiderMediator>();
            rotatorMediatorMock = new Mock<IRotatorMediator>();
            sdMediatorMock = new Mock<ISafetyMonitorMediator>();
            switchMediatorMock = new Mock<ISwitchMediator>();
            telescopeMediatorMock = new Mock<ITelescopeMediator>();
            wdMediatorMock = new Mock<IWeatherDataMediator>();
        }

        [Test]
        public void Clone_ItemClonedProperly() {
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo() {
                SupportedActions = new List<string>() {
                    "Action1",
                    "Action2",
                },
            });

            var sut = new DeviceAction(cameraMediatorMock.Object, domeMediatorMock.Object, fwMediatorMock.Object, fdMediatorMock.Object,
                                       focuserMediatorMock.Object, guiderMediatorMock.Object, rotatorMediatorMock.Object, sdMediatorMock.Object,
                                       switchMediatorMock.Object, telescopeMediatorMock.Object, wdMediatorMock.Object) {
                DeviceType = NINA.Core.Enum.DeviceTypeEnum.TELESCOPE,
                ActionName = 1,
                ActionParameters = "someParameter",
            };

            var item2 = (DeviceAction)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.DeviceType.Should().Be(sut.DeviceType);
            item2.ActionName.Should().Be(sut.ActionName);
            item2.ActionParameters.Should().BeSameAs(sut.ActionParameters);
        }

        [Test]
        public void Validate_NoIssues() {
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo() {
                Connected = true,
                SupportedActions = new List<string>() {
                    "Action1",
                    "Action2",
                },
            });

            var sut = new DeviceAction(cameraMediatorMock.Object, domeMediatorMock.Object, fwMediatorMock.Object, fdMediatorMock.Object,
                                       focuserMediatorMock.Object, guiderMediatorMock.Object, rotatorMediatorMock.Object, sdMediatorMock.Object,
                                       switchMediatorMock.Object, telescopeMediatorMock.Object, wdMediatorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeTrue();

            sut.Issues.Should().BeEmpty();
        }

        [Test]
        [TestCase(false, 2)]
        [TestCase(true, 1)]
        public void Validate_NotConnected_OneIssue(bool isConnected, int count) {
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo() {
                Connected = isConnected,
                SupportedActions = new List<string>(),
            });

            var sut = new DeviceAction(cameraMediatorMock.Object, domeMediatorMock.Object, fwMediatorMock.Object, fdMediatorMock.Object,
                                       focuserMediatorMock.Object, guiderMediatorMock.Object, rotatorMediatorMock.Object, sdMediatorMock.Object,
                                       switchMediatorMock.Object, telescopeMediatorMock.Object, wdMediatorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeFalse();

            sut.Issues.Should().HaveCount(count);
        }

        [Test]
        public async Task Execute_NoIssues_LogicCalled() {
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo() {
                Connected = true,
                SupportedActions = new List<string>() {
                    "Action1",
                    "Action2",
                },
            });

            var sut = new DeviceAction(cameraMediatorMock.Object, domeMediatorMock.Object, fwMediatorMock.Object, fdMediatorMock.Object,
                                       focuserMediatorMock.Object, guiderMediatorMock.Object, rotatorMediatorMock.Object, sdMediatorMock.Object,
                                       switchMediatorMock.Object, telescopeMediatorMock.Object, wdMediatorMock.Object) {
                DeviceType = NINA.Core.Enum.DeviceTypeEnum.TELESCOPE,
                ActionName = 0,
                ActionParameters = "someParameters",
            };

            await sut.Execute(default, default);
            string actionName = telescopeMediatorMock.Object.GetInfo().SupportedActions[0];

            telescopeMediatorMock.Verify(x => x.Action(actionName, sut.ActionParameters));
        }

        [Test]

        public void GetEstimatedDuration_BasedOnParameters_ReturnsCorrectEstimate() {
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo() {
                Connected = true,
                SupportedActions = new List<string>() {
                    "Action1",
                    "Action2",
                },
            });

            var sut = new DeviceAction(cameraMediatorMock.Object, domeMediatorMock.Object, fwMediatorMock.Object, fdMediatorMock.Object,
                                       focuserMediatorMock.Object, guiderMediatorMock.Object, rotatorMediatorMock.Object, sdMediatorMock.Object,
                                       switchMediatorMock.Object, telescopeMediatorMock.Object, wdMediatorMock.Object) {
                DeviceType = NINA.Core.Enum.DeviceTypeEnum.TELESCOPE,
                ActionName = 0,
                ActionParameters = "someParameters",
            };

            var duration = sut.GetEstimatedDuration();

            duration.Should().Be(TimeSpan.Zero);
        }
    }
}