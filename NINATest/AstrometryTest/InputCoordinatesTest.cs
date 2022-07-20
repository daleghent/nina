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
using NINA.Equipment.Model;
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
        [TestCase(1, 10, 20, 1, 10, 20, false)]
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

            if (negativeDec) {
                sut.Coordinates.Dec.Should().BeLessThan(0);
            } else {
                sut.Coordinates.Dec.Should().BeGreaterThanOrEqualTo(0);
            }

        }

        /* The tests below are regression tests over all possible RA and Dec combintations and will take a long time to compute
         * Uncomment them in case you need some changes to the coordinate logic and want to validate that all expected values are still matching
         */
        //[Test]
        //public void SerializationAndDeserialization_RightAscension_Test() {

        //    // Right Ascension Tests
        //    for (int raHours = 0; raHours < 24; raHours++) {
        //        for (int raMinutes = 0; raMinutes < 60; raMinutes++) {
        //            for (int raSeconds = 0; raSeconds < 60; raSeconds++) {


        //                var decDegree = 0;
        //                var decMinutes = 0;
        //                var decSeconds = 0;
        //                var negativeDec = false;

        //                var coordinates = new InputCoordinates();
        //                coordinates.RAHours = raHours;
        //                coordinates.RAMinutes = raMinutes;
        //                coordinates.RASeconds = raSeconds;
        //                coordinates.DecDegrees = decDegree;
        //                coordinates.DecMinutes = decMinutes;
        //                coordinates.DecSeconds = decSeconds;
        //                coordinates.NegativeDec = negativeDec;

        //                coordinates.RAHours.Should().Be(raHours);
        //                coordinates.RAMinutes.Should().Be(raMinutes);
        //                coordinates.RASeconds.Should().Be(raSeconds);
        //                coordinates.DecDegrees.Should().Be(decDegree);
        //                coordinates.DecMinutes.Should().Be(decMinutes);
        //                coordinates.DecSeconds.Should().Be(decSeconds);
        //                coordinates.NegativeDec.Should().Be(negativeDec);

        //                if (negativeDec) {
        //                    coordinates.Coordinates.Dec.Should().BeLessThan(0);
        //                } else {
        //                    coordinates.Coordinates.Dec.Should().BeGreaterThanOrEqualTo(0);
        //                }



        //                var json = JsonConvert.SerializeObject(coordinates);

        //                var sut = JsonConvert.DeserializeObject<InputCoordinates>(json);

        //                sut.RAHours.Should().Be(raHours);
        //                sut.RAMinutes.Should().Be(raMinutes);
        //                sut.RASeconds.Should().Be(raSeconds);
        //                sut.DecDegrees.Should().Be(decDegree);
        //                sut.DecMinutes.Should().Be(decMinutes);
        //                sut.DecSeconds.Should().Be(decSeconds);
        //                sut.NegativeDec.Should().Be(negativeDec);

        //                if (negativeDec) {
        //                    sut.Coordinates.Dec.Should().BeLessThan(0);
        //                } else {
        //                    sut.Coordinates.Dec.Should().BeGreaterThanOrEqualTo(0);
        //                }

        //            }
        //        }
        //    }
        //}

        //[Test]
        //public void SerializationAndDeserialization_Declination_Test(/*int raHours, int raMinutes, int raSeconds, int decDegree, int decMinutes, int decSeconds, bool negativeDec*/) {
            
        //    for (int decDegree = -89; decDegree < 90; decDegree++) {
        //        for (int decMinutes = 0; decMinutes < 60; decMinutes++) {
        //            for (int decSeconds = 0; decSeconds < 60; decSeconds++) {


        //                var raHours = 0;
        //                var raMinutes = 0;
        //                var raSeconds = 0;
        //                var negativeDec = decDegree < 0;

        //                var coordinates = new InputCoordinates();
        //                coordinates.RAHours = raHours;
        //                coordinates.RAMinutes = raMinutes;
        //                coordinates.RASeconds = raSeconds;
        //                coordinates.DecDegrees = decDegree;
        //                coordinates.DecMinutes = decMinutes;
        //                coordinates.DecSeconds = decSeconds;
        //                coordinates.NegativeDec = negativeDec;

        //                coordinates.RAHours.Should().Be(raHours);
        //                coordinates.RAMinutes.Should().Be(raMinutes);
        //                coordinates.RASeconds.Should().Be(raSeconds);
        //                coordinates.DecDegrees.Should().Be(decDegree);
        //                coordinates.DecMinutes.Should().Be(decMinutes);
        //                coordinates.DecSeconds.Should().Be(decSeconds);
        //                coordinates.NegativeDec.Should().Be(negativeDec);

        //                if (negativeDec) {
        //                    coordinates.Coordinates.Dec.Should().BeLessThan(0);
        //                } else {
        //                    coordinates.Coordinates.Dec.Should().BeGreaterThanOrEqualTo(0);
        //                }



        //                var json = JsonConvert.SerializeObject(coordinates);

        //                var sut = JsonConvert.DeserializeObject<InputCoordinates>(json);

        //                sut.RAHours.Should().Be(raHours);
        //                sut.RAMinutes.Should().Be(raMinutes);
        //                sut.RASeconds.Should().Be(raSeconds);
        //                sut.DecDegrees.Should().Be(decDegree);
        //                sut.DecMinutes.Should().Be(decMinutes);
        //                sut.DecSeconds.Should().Be(decSeconds);
        //                sut.NegativeDec.Should().Be(negativeDec);

        //                if (negativeDec) {
        //                    sut.Coordinates.Dec.Should().BeLessThan(0);
        //                } else {
        //                    sut.Coordinates.Dec.Should().BeGreaterThanOrEqualTo(0);
        //                }

        //            }
        //        }
        //    }
        //}

        //[Test]
        //public void SerializationAndDeserialization_NegativeDec_BetweenZeroAndMinusOne_Test() {


        //    // Special cases for negative declination between 0 and -1
        //    for (int decMinutes = 0; decMinutes < 60; decMinutes++) {
        //        for (int decSeconds = 0; decSeconds < 60; decSeconds++) {


        //            var raHours = 0;
        //            var raMinutes = 0;
        //            var raSeconds = 0;
        //            var negativeDec = true;

        //            var coordinates = new InputCoordinates();
        //            coordinates.RAHours = raHours;
        //            coordinates.RAMinutes = raMinutes;
        //            coordinates.RASeconds = raSeconds;
        //            coordinates.DecDegrees = 0;
        //            coordinates.NegativeDec = negativeDec;
        //            coordinates.DecMinutes = decMinutes;
        //            coordinates.DecSeconds = decSeconds;

        //            coordinates.RAHours.Should().Be(raHours);
        //            coordinates.RAMinutes.Should().Be(raMinutes);
        //            coordinates.RASeconds.Should().Be(raSeconds);
        //            coordinates.DecDegrees.Should().Be(0);
        //            coordinates.DecMinutes.Should().Be(decMinutes);
        //            coordinates.DecSeconds.Should().Be(decSeconds);
        //            coordinates.NegativeDec.Should().Be(negativeDec);

        //            if (negativeDec && (decMinutes > 0 || decSeconds > 0)) {
        //                coordinates.Coordinates.Dec.Should().BeLessThan(0);
        //            } else {
        //                coordinates.Coordinates.Dec.Should().BeGreaterThanOrEqualTo(0);
        //            }



        //            var json = JsonConvert.SerializeObject(coordinates);

        //            var sut = JsonConvert.DeserializeObject<InputCoordinates>(json);

        //            sut.RAHours.Should().Be(raHours);
        //            sut.RAMinutes.Should().Be(raMinutes);
        //            sut.RASeconds.Should().Be(raSeconds);
        //            sut.DecDegrees.Should().Be(0);
        //            sut.DecMinutes.Should().Be(decMinutes);
        //            sut.DecSeconds.Should().Be(decSeconds);
        //            sut.NegativeDec.Should().Be(negativeDec);

        //            if (negativeDec && (decMinutes > 0 || decSeconds > 0)) {
        //                sut.Coordinates.Dec.Should().BeLessThan(0);
        //            } else {
        //                sut.Coordinates.Dec.Should().BeGreaterThanOrEqualTo(0);
        //            }

        //        }

        //    }

        //}

        //[Test]
        //public void CaptureSequenceList_SerializationAndDeserialization_RightAscension_Test() {

        //    // Right Ascension Tests
        //    for (int raHours = 0; raHours < 24; raHours++) {
        //        for (int raMinutes = 0; raMinutes < 60; raMinutes++) {
        //            for (int raSeconds = 0; raSeconds < 60; raSeconds++) {


        //                var decDegree = 0;
        //                var decMinutes = 0;
        //                var decSeconds = 0;
        //                var negativeDec = false;

        //                var coordinates = new CaptureSequenceList();
        //                coordinates.RAHours = raHours;
        //                coordinates.RAMinutes = raMinutes;
        //                coordinates.RASeconds = raSeconds;
        //                coordinates.DecDegrees = decDegree;
        //                coordinates.DecMinutes = decMinutes;
        //                coordinates.DecSeconds = decSeconds;
        //                coordinates.NegativeDec = negativeDec;

        //                coordinates.RAHours.Should().Be(raHours);
        //                coordinates.RAMinutes.Should().Be(raMinutes);
        //                coordinates.RASeconds.Should().Be(raSeconds);
        //                coordinates.DecDegrees.Should().Be(decDegree);
        //                coordinates.DecMinutes.Should().Be(decMinutes);
        //                coordinates.DecSeconds.Should().Be(decSeconds);
        //                coordinates.NegativeDec.Should().Be(negativeDec);

        //                if (negativeDec) {
        //                    coordinates.Coordinates.Dec.Should().BeLessThan(0);
        //                } else {
        //                    coordinates.Coordinates.Dec.Should().BeGreaterThanOrEqualTo(0);
        //                }

        //            }
        //        }
        //    }




        //}

        //[Test]
        //public void CaptureSequenceList_SerializationAndDeserialization_Declination_Test() {

        //    for (int decDegree = -89; decDegree < 90; decDegree++) {
        //        for (int decMinutes = 0; decMinutes < 60; decMinutes++) {
        //            for (int decSeconds = 0; decSeconds < 60; decSeconds++) {


        //                var raHours = 0;
        //                var raMinutes = 0;
        //                var raSeconds = 0;
        //                var negativeDec = decDegree < 0;

        //                var coordinates = new CaptureSequenceList();
        //                coordinates.RAHours = raHours;
        //                coordinates.RAMinutes = raMinutes;
        //                coordinates.RASeconds = raSeconds;
        //                coordinates.DecDegrees = decDegree;
        //                coordinates.DecMinutes = decMinutes;
        //                coordinates.DecSeconds = decSeconds;
        //                coordinates.NegativeDec = negativeDec;

        //                coordinates.RAHours.Should().Be(raHours);
        //                coordinates.RAMinutes.Should().Be(raMinutes);
        //                coordinates.RASeconds.Should().Be(raSeconds);
        //                coordinates.DecDegrees.Should().Be(decDegree);
        //                coordinates.DecMinutes.Should().Be(decMinutes);
        //                coordinates.DecSeconds.Should().Be(decSeconds);
        //                coordinates.NegativeDec.Should().Be(negativeDec);

        //                if (negativeDec) {
        //                    coordinates.Coordinates.Dec.Should().BeLessThan(0);
        //                } else {
        //                    coordinates.Coordinates.Dec.Should().BeGreaterThanOrEqualTo(0);
        //                }
        //            }
        //        }
        //    }



        //}

        //[Test]
        //public void CaptureSequenceList_SerializationAndDeserialization_NegativeDec_BetweenZeroAndMinusOne_Test() {


        //    // Special cases for negative declination between 0 and -1
        //    for (int decMinutes = 0; decMinutes < 60; decMinutes++) {
        //        for (int decSeconds = 0; decSeconds < 60; decSeconds++) {


        //            var raHours = 0;
        //            var raMinutes = 0;
        //            var raSeconds = 0;
        //            var negativeDec = true;

        //            var coordinates = new CaptureSequenceList();
        //            coordinates.RAHours = raHours;
        //            coordinates.RAMinutes = raMinutes;
        //            coordinates.RASeconds = raSeconds;
        //            coordinates.DecDegrees = 0;
        //            coordinates.NegativeDec = negativeDec;
        //            coordinates.DecMinutes = decMinutes;
        //            coordinates.DecSeconds = decSeconds;

        //            coordinates.RAHours.Should().Be(raHours);
        //            coordinates.RAMinutes.Should().Be(raMinutes);
        //            coordinates.RASeconds.Should().Be(raSeconds);
        //            coordinates.DecDegrees.Should().Be(0);
        //            coordinates.DecMinutes.Should().Be(decMinutes);
        //            coordinates.DecSeconds.Should().Be(decSeconds);
        //            coordinates.NegativeDec.Should().Be(negativeDec);

        //            if (negativeDec && (decMinutes > 0 || decSeconds > 0)) {
        //                coordinates.Coordinates.Dec.Should().BeLessThan(0);
        //            } else {
        //                coordinates.Coordinates.Dec.Should().BeGreaterThanOrEqualTo(0);
        //            }
        //        }

        //    }

        //}



    }
}
