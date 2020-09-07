using NINA.Utility.Astrometry;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINATest {

    [TestFixture]
    public class NighttimeCalculatorTest {

        [Test]
        public void AfterNoonTest() {
            var date = new DateTime(2020, 5, 4, 14, 0, 0);
            var referenceDate = NighttimeCalculator.GetReferenceDate(date);
            var expectedDate = new DateTime(date.Year, date.Month, date.Day, 12, 0, 0);
            Assert.AreEqual(expectedDate, referenceDate);
        }

        [Test]
        public void BeforeNoonTest() {
            var date = new DateTime(2020, 5, 4, 10, 0, 0);
            var referenceDate = NighttimeCalculator.GetReferenceDate(date);
            var dayBefore = date.AddDays(-1);
            var expectedDate = new DateTime(dayBefore.Year, dayBefore.Month, dayBefore.Day, 12, 0, 0);
            Assert.AreEqual(expectedDate, referenceDate);
        }

        [Test]
        public void AtNoonTest() {
            var date = new DateTime(2020, 5, 4, 12, 0, 0);
            var referenceDate = NighttimeCalculator.GetReferenceDate(date);
            var dayBefore = date.AddDays(-1);
            var expectedDate = new DateTime(dayBefore.Year, dayBefore.Month, dayBefore.Day, 12, 0, 0);
            Assert.AreEqual(expectedDate, referenceDate);
        }
    }
}
