using OxyPlot;
using OxyPlot.Axes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace NINA.Utility.Astrometry {

    public class NighttimeData {

        public NighttimeData(
            DateTime date,
            DateTime referenceDate,
            Astrometry.MoonPhase moonPhase,
            double? moonIllumination,
            RiseAndSetEvent twilightRiseAndSet,
            RiseAndSetEvent sunRiseAndSet,
            RiseAndSetEvent moonRiseAndSet) {
            this.Date = date;
            this.ReferenceDate = referenceDate;
            this.MoonPhase = moonPhase;
            this.Illumination = moonIllumination;
            this.TwilightRiseAndSet = twilightRiseAndSet;
            this.SunRiseAndSet = sunRiseAndSet;
            this.MoonRiseAndSet = moonRiseAndSet;
            this.NightDuration = new AsyncObservableCollection<DataPoint>(CalculateNightDuration(twilightRiseAndSet));
            this.TwilightDuration = new AsyncObservableCollection<DataPoint>(CalculateTwilightDuration(twilightRiseAndSet, sunRiseAndSet));
            this.Ticker = new Ticker(TimeSpan.FromSeconds(30));
        }

        public Ticker Ticker { get; }

        private static IList<DataPoint> CalculateTwilightDuration(RiseAndSetEvent twilightRiseAndSet, RiseAndSetEvent sunRiseAndSet) {
            if (twilightRiseAndSet != null && twilightRiseAndSet.Rise.HasValue && twilightRiseAndSet.Set.HasValue) {
                var twilightRise = twilightRiseAndSet.Rise;
                var twilightSet = twilightRiseAndSet.Set;
                var dataPointsBuilder = ImmutableList.CreateBuilder<DataPoint>();
                dataPointsBuilder.Add(new DataPoint(Axis.ToDouble(twilightSet), 90));
                if (sunRiseAndSet != null) {
                    var rise = sunRiseAndSet.Rise;
                    var set = sunRiseAndSet.Set;
                    if (rise.HasValue && set.HasValue) {
                        dataPointsBuilder.Add(new DataPoint(Axis.ToDouble(set), 90));
                        dataPointsBuilder.Add(new DataPoint(Axis.ToDouble(set), 0));
                        dataPointsBuilder.Add(new DataPoint(Axis.ToDouble(rise), 0));
                        dataPointsBuilder.Add(new DataPoint(Axis.ToDouble(rise), 90));
                    }
                }
                dataPointsBuilder.Add(new DataPoint(Axis.ToDouble(twilightRise), 90));
                return dataPointsBuilder.ToImmutable();
            }
            return ImmutableList.Create<DataPoint>();
        }

        private static IList<DataPoint> CalculateNightDuration(RiseAndSetEvent twilightRiseAndSet) {
            if (twilightRiseAndSet != null && twilightRiseAndSet.Rise.HasValue && twilightRiseAndSet.Set.HasValue) {
                var rise = twilightRiseAndSet.Rise;
                var set = twilightRiseAndSet.Set;
                return ImmutableList.Create(
                    new DataPoint(Axis.ToDouble(rise), 90),
                    new DataPoint(Axis.ToDouble(set), 90));
            }

            return ImmutableList.Create<DataPoint>();
        }

        public DateTime Date { get; set; }
        public DateTime ReferenceDate { get; set; }
        public Astrometry.MoonPhase MoonPhase { get; set; }
        public double? Illumination { get; set; }
        public RiseAndSetEvent TwilightRiseAndSet { get; set; }
        public RiseAndSetEvent SunRiseAndSet { get; set; }
        public RiseAndSetEvent MoonRiseAndSet { get; set; }
        public AsyncObservableCollection<DataPoint> TwilightDuration { get; set; }
        public AsyncObservableCollection<DataPoint> NightDuration { get; set; }
    }
}