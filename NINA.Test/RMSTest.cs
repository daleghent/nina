#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Model;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Test {

    [TestFixture]
    public class RMSTest {

        [Test]
        public void RMS_DefaultConstructorTest() {
            RMS rms = new RMS();

            ClassicAssert.AreEqual(1, rms.Scale);
            ClassicAssert.AreEqual(0, rms.RA);
            ClassicAssert.AreEqual(0, rms.Dec);
            ClassicAssert.AreEqual(0, rms.Total);
        }

        [Test]
        public void RMS_AddSingleValue_CalculateCorrect() {
            RMS rms = new RMS();

            rms.AddDataPoint(10, 10);

            ClassicAssert.AreEqual(0, rms.RA);
            ClassicAssert.AreEqual(0, rms.Dec);
            ClassicAssert.AreEqual(0, rms.Total);
        }

        [Test]
        public void RMS_AddMultipleDataPoints_CalculateCorrect() {
            RMS rms = new RMS();

            rms.AddDataPoint(25, 1296);
            rms.AddDataPoint(625, 36);
            rms.AddDataPoint(25, 1296);
            rms.AddDataPoint(625, 36);

            ClassicAssert.AreEqual(300, rms.RA);
            ClassicAssert.AreEqual(630, rms.Dec);
            var total = Math.Sqrt((Math.Pow(300, 2) + Math.Pow(630, 2)));
            ClassicAssert.AreEqual(total, rms.Total);
        }

        [Test]
        public void RMS_AddMultipleDataPoints2_CalculateCorrect() {
            RMS rms = new RMS();

            rms.AddDataPoint(-25, -36);
            rms.AddDataPoint(-625, -1296);
            rms.AddDataPoint(-25, -36);
            rms.AddDataPoint(-625, -1296);

            ClassicAssert.AreEqual(300, rms.RA);
            ClassicAssert.AreEqual(630, rms.Dec);
            var total = Math.Sqrt((Math.Pow(300, 2) + Math.Pow(630, 2)));
            ClassicAssert.AreEqual(total, rms.Total);
        }

        [Test]
        public void RMS_AddMultipleDataPointsAndSetScale_CalculateCorrect() {
            RMS rms = new RMS();

            rms.AddDataPoint(-25, -36);
            rms.AddDataPoint(-625, -1296);
            rms.AddDataPoint(-25, -36);
            rms.AddDataPoint(-625, -1296);

            var scale = 1.59;
            rms.SetScale(scale);

            ClassicAssert.AreEqual(300, rms.RA);
            ClassicAssert.AreEqual(630, rms.Dec);
            var total = Math.Sqrt((Math.Pow(300, 2) + Math.Pow(630, 2)));
            ClassicAssert.AreEqual(total, rms.Total);
        }

        [Test]
        public void RMS_AddValuesAndClear_AllResetExceptScale() {
            RMS rms = new RMS();

            rms.AddDataPoint(-25, -36);
            rms.AddDataPoint(-625, -1296);
            rms.AddDataPoint(-25, -36);
            rms.AddDataPoint(-625, -1296);

            var scale = 1.59;
            rms.SetScale(scale);

            rms.Clear();

            ClassicAssert.AreEqual(scale, rms.Scale);
            ClassicAssert.AreEqual(0, rms.RA);
            ClassicAssert.AreEqual(0, rms.Dec);
            ClassicAssert.AreEqual(0, rms.Total);
        }

        [Test]
        public void RMS_AddValuesClearAndAddOneAgain_ValuesAppliedCorrectly() {
            RMS rms = new RMS();

            rms.AddDataPoint(-25, -36);
            rms.AddDataPoint(-625, -1296);
            rms.AddDataPoint(-25, -36);
            rms.AddDataPoint(-625, -1296);

            var scale = 1.59;
            rms.SetScale(scale);

            rms.Clear();
            rms.AddDataPoint(-25, -36);

            ClassicAssert.AreEqual(scale, rms.Scale);
            ClassicAssert.AreEqual(0, rms.RA);
            ClassicAssert.AreEqual(0, rms.Dec);
            ClassicAssert.AreEqual(0, rms.Total);
        }
    }
}