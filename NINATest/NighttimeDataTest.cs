using NINA.Utility.Astrometry;
using NUnit.Framework;
using OxyPlot.Axes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NINA.Utility.Astrometry.Astrometry;

namespace NINATest {

    [TestFixture]
    public class NighttimeDataTest {
        private DateTime date;
        private DateTime referenceDate;
        private double latitude;
        private double longitude;
        private RiseAndSetEvent twilightRiseAndSet;
        private RiseAndSetEvent moonRiseAndSet;
        private RiseAndSetEvent sunRiseAndSet;
        private MoonPhase moonPhase;
        private double? illumination;

        [SetUp]
        public void Setup() {
            date = new DateTime(2020, 5, 5, 14, 0, 0);
            referenceDate = NighttimeCalculator.GetReferenceDate(date);
            latitude = 41.0;
            longitude = 70.3;
            twilightRiseAndSet = Astrometry.GetNightTimes(referenceDate, latitude, longitude);
            moonRiseAndSet = Astrometry.GetMoonRiseAndSet(referenceDate, latitude, longitude);
            sunRiseAndSet = Astrometry.GetSunRiseAndSet(referenceDate, latitude, longitude);
            moonPhase = MoonPhase.FullMoon;
            illumination = 100.0;
        }

        public NighttimeData GetData() {
            return new NighttimeData(date, referenceDate, moonPhase, illumination, twilightRiseAndSet, sunRiseAndSet, moonRiseAndSet);
        }

        public List<OxyPlot.DataPoint> GetExpectedTwilightDuration() {
            if (twilightRiseAndSet == null) {
                return new List<OxyPlot.DataPoint>();
            }
            var twilightSet = twilightRiseAndSet.Set.Value > twilightRiseAndSet.Rise.Value ? twilightRiseAndSet.Set.Value.AddDays(-1) : twilightRiseAndSet.Set.Value;
            if (sunRiseAndSet == null) {
                return new List<OxyPlot.DataPoint>() {
                    new OxyPlot.DataPoint(Axis.ToDouble(twilightSet), 90),
                    new OxyPlot.DataPoint(Axis.ToDouble(twilightRiseAndSet.Rise), 90) };
            }
            var sunRiseSet = sunRiseAndSet.Set.Value > sunRiseAndSet.Rise.Value ? sunRiseAndSet.Set.Value.AddDays(-1) : sunRiseAndSet.Set.Value;
            return new List<OxyPlot.DataPoint>() {
                new OxyPlot.DataPoint(Axis.ToDouble(twilightSet), 90),
                new OxyPlot.DataPoint(Axis.ToDouble(sunRiseSet), 90),
                new OxyPlot.DataPoint(Axis.ToDouble(sunRiseSet), 0),
                new OxyPlot.DataPoint(Axis.ToDouble(sunRiseAndSet.Rise), 0),
                new OxyPlot.DataPoint(Axis.ToDouble(sunRiseAndSet.Rise), 90),
                new OxyPlot.DataPoint(Axis.ToDouble(twilightRiseAndSet.Rise), 90) };
        }

        public List<OxyPlot.DataPoint> GetExpectedNightDuration() {
            if (twilightRiseAndSet == null) {
                return new List<OxyPlot.DataPoint>();
            }
            var twilightSet = twilightRiseAndSet.Set.Value > twilightRiseAndSet.Rise.Value ? twilightRiseAndSet.Set.Value.AddDays(-1) : twilightRiseAndSet.Set.Value;
            return new List<OxyPlot.DataPoint>() {
                new OxyPlot.DataPoint(Axis.ToDouble(twilightRiseAndSet.Rise), 90),
                new OxyPlot.DataPoint(Axis.ToDouble(twilightSet), 90) };
        }

        [Test]
        public void AllSetTest() {
            var data = GetData();
            Assert.AreEqual(moonPhase, data.MoonPhase);
            Assert.AreEqual(illumination, data.Illumination);
            Assert.AreEqual(date, data.Date);
            Assert.AreEqual(referenceDate, data.ReferenceDate);
            Assert.AreEqual(twilightRiseAndSet, data.TwilightRiseAndSet);
            Assert.AreEqual(moonRiseAndSet, data.MoonRiseAndSet);
            Assert.AreEqual(sunRiseAndSet, data.SunRiseAndSet);
            CollectionAssert.AreEqual(GetExpectedNightDuration(), data.NightDuration.ToImmutableList());
            CollectionAssert.AreEqual(GetExpectedTwilightDuration(), data.TwilightDuration.ToImmutableList());
        }

        [Test]
        public void MoonPhaseChangedTest() {
            moonPhase = MoonPhase.LastQuarter;
            var data = GetData();
            Assert.AreEqual(moonPhase, data.MoonPhase);
        }

        [Test]
        public void IlluminationChangedTest() {
            illumination = null;
            var data = GetData();
            Assert.IsFalse(data.Illumination.HasValue);
        }

        [Test]
        public void TwilightNotSetTest() {
            twilightRiseAndSet = null;
            var data = GetData();
            CollectionAssert.AreEqual(GetExpectedNightDuration(), data.NightDuration.ToImmutableList());
            CollectionAssert.AreEqual(GetExpectedTwilightDuration(), data.TwilightDuration.ToImmutableList());
        }

        [Test]
        public void SunriseNotSetTest() {
            sunRiseAndSet = null;
            var data = GetData();
            CollectionAssert.AreEqual(GetExpectedNightDuration(), data.NightDuration.ToImmutableList());
            CollectionAssert.AreEqual(GetExpectedTwilightDuration(), data.TwilightDuration.ToImmutableList());
        }

        [Test]
        public void TwilightAndSunriseNotSetTest() {
            twilightRiseAndSet = null;
            sunRiseAndSet = null;
            var data = GetData();
            CollectionAssert.AreEqual(GetExpectedNightDuration(), data.NightDuration.ToImmutableList());
            CollectionAssert.AreEqual(GetExpectedTwilightDuration(), data.TwilightDuration.ToImmutableList());
        }
    }
}
