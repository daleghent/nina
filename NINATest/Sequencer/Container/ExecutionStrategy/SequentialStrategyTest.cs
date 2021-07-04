#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Moq;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Sequencer;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.Container.ExecutionStrategy;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace NINATest.Sequencer.Container.ExecutionStrategy {

    [TestFixture]
    public class SequentialStrategyTest {

        [Test]
        public async Task Execute_WithoutConditions_ExecutedOnce() {
            var containerMock = new Mock<SequenceContainer>(new Mock<IExecutionStrategy>().Object);
            var item1Mock = new Mock<ISequenceItem>();

            item1Mock
                .Setup(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                    .Callback(() => item1Mock.SetupGet(x => x.Status).Returns(SequenceEntityStatus.FINISHED));

            var item2Mock = new Mock<ISequenceItem>();
            item2Mock
                .Setup(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .Callback(() => item2Mock.SetupGet(x => x.Status).Returns(SequenceEntityStatus.FINISHED));
            containerMock.Object.Add(item1Mock.Object);
            containerMock.Object.Add(item2Mock.Object);

            var sut = new SequentialStrategy();
            await sut.Execute(containerMock.Object, default, default);

            item1Mock.Verify(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
            item2Mock.Verify(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Execute_WithCondition_AllExecutedOnce() {
            var containerMock = new Mock<SequenceContainer>(new Mock<IExecutionStrategy>().Object);
            var conditionMock = new Mock<ISequenceCondition>();
            conditionMock
                .SetupSequence(x => x.Check(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>()))
                .Returns(true)
                .Returns(true)
                .Returns(true)
                .Returns(false);
            containerMock.Object.Add(conditionMock.Object);

            var item1Mock = new Mock<ISequenceItem>();
            item1Mock
                .SetupSequence(x => x.Status)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED);
            var item2Mock = new Mock<ISequenceItem>();
            item2Mock
                .SetupSequence(x => x.Status)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED);
            var items = new List<ISequenceItem>() { item1Mock.Object, item2Mock.Object };
            foreach (var item in items) {
                containerMock.Object.Items.Add(item);
            }

            var sut = new SequentialStrategy();
            await sut.Execute(containerMock.Object, default, default);

            item1Mock.Verify(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
            item2Mock.Verify(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Execute_WithCondition_OnlyFirstExecuted() {
            var containerMock = new Mock<SequenceContainer>(new Mock<IExecutionStrategy>().Object);
            var conditionMock = new Mock<ISequenceCondition>();
            conditionMock
                .SetupSequence(x => x.Check(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>()))
                .Returns(true)
                .Returns(true)
                .Returns(false);
            containerMock.Object.Add(conditionMock.Object);

            var item1Mock = new Mock<ISequenceItem>();
            item1Mock
                .SetupSequence(x => x.Status)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED);
            var item2Mock = new Mock<ISequenceItem>();
            item2Mock
                .SetupSequence(x => x.Status)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED);
            var items = new List<ISequenceItem>() { item1Mock.Object, item2Mock.Object };
            foreach (var item in items) {
                containerMock.Object.Items.Add(item);
            }

            var sut = new SequentialStrategy();
            await sut.Execute(containerMock.Object, default, default);

            item1Mock.Verify(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
            item2Mock.Verify(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Execute_WithOneInstruction_FirstExecutes_NoPreviousPassedToRunTriggers() {
            var containerMock = new Mock<SequenceContainer>(new Mock<IExecutionStrategy>().Object);
            var triggerableMock = containerMock.As<ITriggerable>();
            triggerableMock.Setup(x => x.GetTriggersSnapshot()).Returns(new List<ISequenceTrigger>());

            var item1Mock = new Mock<ISequenceItem>();
            item1Mock
                .SetupSequence(x => x.Status)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED);

            var items = new List<ISequenceItem>() { item1Mock.Object };
            foreach (var item in items) {
                containerMock.Object.Items.Add(item);
            }

            var sut = new SequentialStrategy();
            await sut.Execute(containerMock.Object, default, default);

            triggerableMock.Verify(x => x.RunTriggers(It.Is<ISequenceItem>(item => item == null), It.Is<ISequenceItem>(item => item == item1Mock.Object), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Execute_WithTwoInstructions_FirstExecutes_NoPreviousPassedToRunTriggers_SecondExecutes_PreviousItemPassed() {
            var containerMock = new Mock<SequenceContainer>(new Mock<IExecutionStrategy>().Object);
            var triggerableMock = containerMock.As<ITriggerable>();
            triggerableMock.Setup(x => x.GetTriggersSnapshot()).Returns(new List<ISequenceTrigger>());

            var item1Mock = new Mock<ISequenceItem>();
            item1Mock
                .SetupSequence(x => x.Status)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED);
            var item2Mock = new Mock<ISequenceItem>();
            item2Mock
                .SetupSequence(x => x.Status)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED);
            var items = new List<ISequenceItem>() { item1Mock.Object, item2Mock.Object };
            foreach (var item in items) {
                containerMock.Object.Items.Add(item);
            }

            var sut = new SequentialStrategy();
            await sut.Execute(containerMock.Object, default, default);

            triggerableMock.Verify(x => x.RunTriggers(It.Is<ISequenceItem>(item => item == null), It.Is<ISequenceItem>(item => item == item1Mock.Object), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
            triggerableMock.Verify(x => x.RunTriggers(It.Is<ISequenceItem>(item => item == item1Mock.Object), It.Is<ISequenceItem>(item => item == item2Mock.Object), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Execute_WithOneInstruction_FirstExecutes_NoPreviousPassedToCheckConditions() {
            var containerMock = new Mock<SequenceContainer>(new Mock<IExecutionStrategy>().Object);

            var conditionableMock = containerMock.As<IConditionable>();
            conditionableMock.Setup(x => x.GetConditionsSnapshot()).Returns(new List<ISequenceCondition>() { new Mock<ISequenceCondition>().Object });
            conditionableMock.Setup(x => x.CheckConditions(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>())).Returns(true);

            var item1Mock = new Mock<ISequenceItem>();
            item1Mock
                .SetupSequence(x => x.Status)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED);

            var items = new List<ISequenceItem>() { item1Mock.Object };
            foreach (var item in items) {
                containerMock.Object.Items.Add(item);
            }

            var sut = new SequentialStrategy();
            await sut.Execute(containerMock.Object, default, default);

            // Will be called twice - once on start of container and once prior to execution
            conditionableMock.Verify(x => x.CheckConditions(It.Is<ISequenceItem>(item => item == null), It.Is<ISequenceItem>(item => item == item1Mock.Object)), Times.Exactly(2));
            // Will be called once - after execution of the single item
            conditionableMock.Verify(x => x.CheckConditions(It.Is<ISequenceItem>(item => item == item1Mock.Object), It.Is<ISequenceItem>(item => item == null)), Times.Once);
        }

        [Test]
        public async Task Execute_WithTwoInstructions_FirstExecutes_NoPreviousPassedToCheckConditions_SecondExecutes_PreviousItemPassed() {
            var containerMock = new Mock<SequenceContainer>(new Mock<IExecutionStrategy>().Object);

            var conditionableMock = containerMock.As<IConditionable>();
            conditionableMock.Setup(x => x.GetConditionsSnapshot()).Returns(new List<ISequenceCondition>() { new Mock<ISequenceCondition>().Object });
            conditionableMock.Setup(x => x.CheckConditions(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>())).Returns(true);

            var item1Mock = new Mock<ISequenceItem>();
            item1Mock
                .SetupSequence(x => x.Status)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED);
            var item2Mock = new Mock<ISequenceItem>();
            item2Mock
                .SetupSequence(x => x.Status)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED);
            var items = new List<ISequenceItem>() { item1Mock.Object, item2Mock.Object };
            foreach (var item in items) {
                containerMock.Object.Items.Add(item);
            }

            var sut = new SequentialStrategy();
            await sut.Execute(containerMock.Object, default, default);

            // Will be called twice - once on start of container and once prior to execution
            conditionableMock.Verify(x => x.CheckConditions(It.Is<ISequenceItem>(item => item == null), It.Is<ISequenceItem>(item => item == item1Mock.Object)), Times.Exactly(2));
            // Will be called once - after execution of the first item
            conditionableMock.Verify(x => x.CheckConditions(It.Is<ISequenceItem>(item => item == item1Mock.Object), It.Is<ISequenceItem>(item => item == item2Mock.Object)), Times.Once);
            // Will be called once - after execution of the last item
            conditionableMock.Verify(x => x.CheckConditions(It.Is<ISequenceItem>(item => item == item2Mock.Object), It.Is<ISequenceItem>(item => item == null)), Times.Once);
        }
    }
}