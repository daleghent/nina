#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NUnit.Framework;
using FluentAssertions;
using NINA.Astrometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using NINA.Core.Model;

namespace NINATest.AstrometryTest {

    [TestFixture]
    public class CustomHorizonTest {

        [Test]
        public void Testbasdf() {
            var testFile = Path.Combine(TestContext.CurrentContext.TestDirectory, "AstrometryTest", "HorizonData", "full360.hrz");
            var customHorizon = CustomHorizon.FromFile(testFile);
        }

        [Test]
        public void CompleteAzimuth_MinMaxAltitudeRetrievedCorrectly() {
            var data = Path.Combine(TestContext.CurrentContext.TestDirectory, "AstrometryTest", "HorizonData", "full360.hrz");

            var sut = CustomHorizon.FromFile(data);

            sut.GetMaxAltitude().Should().Be(70.9);
            sut.GetMinAltitude().Should().Be(7.6);
        }

        [Test]
        [TestCase(0, 36.8)]
        [TestCase(359, 36.9)]
        [TestCase(-1, 36.9)]
        [TestCase(360, 36.8)]
        [TestCase(1111160, 33.7)]
        [TestCase(90, 8.4)]
        [TestCase(180, 43.5)]
        [TestCase(270, 29.0)]
        [TestCase(291, 43.3)]
        public void CompleteAzimuth_AbsoluteValuesRetrievedCorrectly(double azimuth, double expectedAltitude) {
            var data = Path.Combine(TestContext.CurrentContext.TestDirectory, "AstrometryTest", "HorizonData", "full360.hrz");

            var sut = CustomHorizon.FromFile(data);

            sut.GetAltitude(azimuth).Should().Be(expectedAltitude);
        }

        [Test]
        [TestCase(0, 14)]
        [TestCase(359, 14.6)]
        [TestCase(100, 46.6666666666)]
        [TestCase(200, 25.470588235294)]
        public void PartialAzimuth_InterpolationRetrievedCorrectly(double azimuth, double expectedAltitude) {
            var data = Path.Combine(TestContext.CurrentContext.TestDirectory, "AstrometryTest", "HorizonData", "partial.hrz");

            var sut = CustomHorizon.FromFile(data);

            sut.GetAltitude(azimuth).Should().BeApproximately(expectedAltitude, 0.0000001);
        }

        [Test]
        [TestCase(0, 30)]
        [TestCase(359, 29.833333333333)]
        [TestCase(100, 25.124555160)]
        [TestCase(200, 16.22775800)]
        public void IncompleteAzimuth_InterpolationRetrievedCorrectly(double azimuth, double expectedAltitude) {
            var data = Path.Combine(TestContext.CurrentContext.TestDirectory, "AstrometryTest", "HorizonData", "incomplete.hrz");

            var sut = CustomHorizon.FromFile(data);

            sut.GetAltitude(azimuth).Should().BeApproximately(expectedAltitude, 0.0000001);
        }

        [Test]
        public void FileNotFound_FileNotFoundExceptionThrown() {
            var data = Path.Combine(TestContext.CurrentContext.TestDirectory, "AstrometryTest", "HorizonData", "invalid.abc");

            Action act = () => CustomHorizon.FromFile(data);

            act.Should().Throw<FileNotFoundException>();
        }

        [Test]
        public void FileNotFound_ArgumentExceptionThrown() {
            var data = Path.Combine(TestContext.CurrentContext.TestDirectory, "AstrometryTest", "HorizonData", "empty.hrz");

            Action act = () => CustomHorizon.FromFile(data);

            act.Should().Throw<ArgumentException>().WithMessage("Horizon file does not contain enough entries or is invalid");
        }
    }
}