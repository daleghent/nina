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
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NUnit.Framework;
using OxyPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINATest.Sequencer.Conditions {

    [TestFixture]
    public class SequenceConditionTest {

        private class SeuqenceConditionImpl : SequenceCondition {

            public override bool Check(ISequenceItem nextItem) {
                throw new NotImplementedException();
            }

            public override object Clone() {
                throw new NotImplementedException();
            }

            public override void ResetProgress() {
                throw new NotImplementedException();
            }

            public override void SequenceBlockFinished() {
                throw new NotImplementedException();
            }

            public override void SequenceBlockStarted() {
                throw new NotImplementedException();
            }
        }

        [Test]
        public void AttachNewParent_Null_Test() {
            var sut = new SeuqenceConditionImpl();

            sut.AttachNewParent(null);

            sut.Parent.Should().BeNull();
        }

        [Test]
        public void AttachNewParent_NewParentAttached_Test() {
            var parent = new Mock<ISequenceContainer>();
            var sut = new SeuqenceConditionImpl();

            sut.AttachNewParent(parent.Object);

            sut.Parent.Should().Be(parent.Object);
        }

        [Test]
        public void Detach_Test() {
            var parent = new Mock<ISequenceContainer>();
            var sut = new SeuqenceConditionImpl();

            sut.AttachNewParent(parent.Object);
            sut.Detach();

            parent.Verify(x => x.Remove(It.Is<ISequenceCondition>(c => c == sut)), Times.Once);
        }

        [Test]
        public void MoveUp_Test() {
            var parent = new Mock<ISequenceContainer>();
            var sut = new SeuqenceConditionImpl();

            sut.AttachNewParent(parent.Object);
            Action act = () => sut.MoveUp();
            act.Should().Throw<NotImplementedException>();
        }

        [Test]
        public void MoveDown_Test() {
            var parent = new Mock<ISequenceContainer>();
            var sut = new SeuqenceConditionImpl();

            sut.AttachNewParent(parent.Object);
            Action act = () => sut.MoveDown();
            act.Should().Throw<NotImplementedException>();
        }
    }
}