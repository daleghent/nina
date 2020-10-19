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
using NINA.Sequencer.Conditions;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Utility.DateTimeProvider;
using NINA.Utility.Astrometry;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINATest.Sequencer.Conditions {

    [TestFixture]
    public class TimeConditionTest {

        [Test]
        public void TimeCondition_Clone_GoodClone() {
            var l = new List<IDateTimeProvider>();
            var sut = new TimeCondition(l);
            sut.Icon = new System.Windows.Media.GeometryGroup();
            var item2 = (TimeCondition)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Icon.Should().BeSameAs(sut.Icon);
            item2.DateTimeProviders.Should().BeSameAs(l);
            item2.Hours.Should().Be(sut.Hours);
            item2.Minutes.Should().Be(sut.Minutes);
            item2.Seconds.Should().Be(sut.Seconds);
        }

        [Test]
        public void TimeCondition_NoProviderInConstructor_NoCrash() {
            var sut = new TimeCondition(null);

            sut.Hours.Should().Be(0);
            sut.Minutes.Should().Be(0);
            sut.Seconds.Should().Be(0);
        }

        [Test]
        public void TimeCondition_SelectProviderInConstructor_TimeExtracted() {
            var providerMock = new Mock<IDateTimeProvider>();
            providerMock.Setup(x => x.GetDateTime()).Returns(new DateTime(2000, 2, 3, 4, 5, 6));

            var sut = new TimeCondition(new List<IDateTimeProvider>() { providerMock.Object });

            sut.Hours.Should().Be(4);
            sut.Minutes.Should().Be(5);
            sut.Seconds.Should().Be(6);
        }

        [Test]
        public void TimeCondition_SelectProvider_TimeExtracted() {
            var providerMock = new Mock<IDateTimeProvider>();
            providerMock.Setup(x => x.GetDateTime()).Returns(new DateTime(1, 2, 3, 4, 5, 6));
            var provider2Mock = new Mock<IDateTimeProvider>();
            provider2Mock.Setup(x => x.GetDateTime()).Returns(new DateTime(2000, 10, 30, 10, 20, 30));

            var sut = new TimeCondition(new List<IDateTimeProvider>() { providerMock.Object, provider2Mock.Object });
            sut.SelectedProvider = sut.DateTimeProviders.Last();

            sut.Hours.Should().Be(10);
            sut.Minutes.Should().Be(20);
            sut.Seconds.Should().Be(30);
        }
    }
}