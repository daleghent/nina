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
using NINA.Sequencer.Conditions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINATest.Sequencer.Conditions {

    [TestFixture]
    public class LoopConditionTest {

        [Test]
        public void LoopConditionTest_Clone_GoodClone() {
            var sut = new LoopCondition();
            sut.Icon = new System.Windows.Media.GeometryGroup();
            var item2 = (LoopCondition)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Icon.Should().BeSameAs(sut.Icon);
            item2.Iterations.Should().Be(sut.Iterations);
        }

        [Test]
        public void Check_True_AfterNoIterations() {
            var sut = new LoopCondition();
            sut.Iterations = 5;

            sut.Check(null).Should().BeTrue();
        }

        [Test]
        public void Check_True_AfterSomeIterations() {
            var sut = new LoopCondition();
            sut.Iterations = 5;

            for (var i = 0; i < 2; i++) {
                sut.SequenceBlockStarted();
                sut.SequenceBlockFinished();
            }

            sut.Check(null).Should().BeTrue();
        }

        [Test]
        [TestCase(1)]
        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(5)]
        [TestCase(100)]
        public void Check_False_AfterAllIterations(int iterations) {
            var sut = new LoopCondition();
            sut.Iterations = iterations;

            for (var i = 0; i < iterations; i++) {
                sut.SequenceBlockStarted();
                sut.SequenceBlockFinished();
            }

            sut.Check(null).Should().BeFalse();
        }
    }
}