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
using NINA.Sequencer.Conditions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINATest.Sequencer.Conditions {

    [TestFixture]
    public class TimeSpanConditionTest {

        [Test]
        public void TimeSpanCondition_Clone_GoodClone() {
            var sut = new TimeSpanCondition();
            sut.Icon = new System.Windows.Media.GeometryGroup();
            var item2 = (TimeSpanCondition)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Icon.Should().BeSameAs(sut.Icon);
            item2.Hours.Should().Be(sut.Hours);
            item2.Minutes.Should().Be(sut.Minutes);
            item2.Seconds.Should().Be(sut.Seconds);
        }

        [Test]
        public void TimeSpanCondition_Constructor_NoCrash() {
            var sut = new TimeSpanCondition();

            sut.Hours.Should().Be(0);
            sut.Minutes.Should().Be(1);
            sut.Seconds.Should().Be(0);
        }
    }
}