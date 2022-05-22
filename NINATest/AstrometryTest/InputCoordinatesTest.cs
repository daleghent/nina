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
using Newtonsoft.Json;
using NINA.Astrometry;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINATest.AstrometryTest {

    [TestFixture]
    public class InputCoordinatesTest {
        [Test]
        [TestCase(1,10,20,1,10,20,false)]
        [TestCase(1, 10, 20, 0, 10, 20, false)]
        [TestCase(1, 10, 20, -1, 10, 20, true)]
        [TestCase(1, 10, 20, -0, 10, 20, true)]
        public void SerializationAndDeserializationTest(int raHours, int raMinutes, int raSeconds, int decDegree, int decMinutes, int decSeconds, bool negativeDec) {

            var coordinates = new InputCoordinates();
            coordinates.RAHours = raHours;
            coordinates.RAMinutes = raMinutes;
            coordinates.RASeconds = raSeconds;
            coordinates.DecDegrees = decDegree;
            coordinates.DecMinutes = decMinutes;
            coordinates.DecSeconds = decSeconds;
            coordinates.NegativeDec = negativeDec;

            var json = JsonConvert.SerializeObject(coordinates);

            var sut = JsonConvert.DeserializeObject<InputCoordinates>(json);

            sut.RAHours.Should().Be(raHours);
            sut.RAMinutes.Should().Be(raMinutes);
            sut.RASeconds.Should().Be(raSeconds);
            sut.DecDegrees.Should().Be(decDegree);
            sut.DecMinutes.Should().Be(decMinutes);
            sut.DecSeconds.Should().Be(decSeconds);
            sut.NegativeDec.Should().Be(negativeDec);

            if(negativeDec) {
                sut.Coordinates.Dec.Should().BeLessThan(0);
            } else {
                sut.Coordinates.Dec.Should().BeGreaterThanOrEqualTo(0);
            }

        }
    }
}
