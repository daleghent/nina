#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using FTD2XX_NET;
using Moq;
using NINA.Exceptions;
using NINA.MGEN2.Commands.AppMode;

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Test.MGEN.Commands {

    [TestFixture]
    public class StartStarSearchCommandTest : CommandTestRunner {
        private Mock<IFTDI> ftdiMock = new Mock<IFTDI>();

        [Test]
        public void ConstructorTest() {
            var sut = new StartStarSearchCommand(2, 50);

            sut.CommandCode.Should().Be(0xca);
            sut.AcknowledgeCode.Should().Be(0xca);
            sut.SubCommandCode.Should().Be(0x30);
            sut.RequiredBaudRate.Should().Be(250000);
            sut.Timeout.Should().Be(15000);
        }

        [Test]
        public void Successful_Scenario_Test() {
            byte gain = 0x05;
            var exposureTime = new byte[] { 0xA0, 0x0F };
            byte stars = 215;
            SetupWrite(ftdiMock, new byte[] { 0xca }, new byte[] { 0x30 }, new byte[] { gain, exposureTime[0], exposureTime[1] });
            SetupRead(ftdiMock, new byte[] { 0xca }, new byte[] { 0x00 }, new byte[] { stars });

            var sut = new StartStarSearchCommand(gain, 4000);
            var result = sut.Execute(ftdiMock.Object);

            result.Success.Should().BeTrue();
            result.NumberOfStars.Should().Be(stars);
        }

        [Test]
        [TestCase(0x99, typeof(UnexpectedReturnCodeException))]
        [TestCase(0xf0, typeof(UILockedException))]
        [TestCase(0xf1, typeof(AnotherCommandInProgressException))]
        [TestCase(0xf2, typeof(CameraIsOffException))]
        [TestCase(0xf3, typeof(AutoGuidingActiveException))]
        public void Exception_Test(byte errorCode, Type ex) {
            SetupWrite(ftdiMock, new byte[] { 0xca }, new byte[] { 0x30 });
            SetupRead(ftdiMock, new byte[] { 0xca }, new byte[] { errorCode });

            var sut = new StartStarSearchCommand(2, 50);
            Action act = () => sut.Execute(ftdiMock.Object);

            TestDelegate test = new TestDelegate(act);

            MethodInfo method = typeof(Assert).GetMethod("Throws", new[] { typeof(TestDelegate) });
            MethodInfo generic = method.MakeGenericMethod(ex);

            generic.Invoke(this, new object[] { test });
        }
    }
}