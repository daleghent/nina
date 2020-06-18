using OxyPlot;
using System;

namespace NINA.Utility.Astrometry {

    public interface INighttimeCalculator {
        RiseAndSetEvent MoonRiseAndSet { get; }
        AsyncObservableCollection<DataPoint> NightDuration { get; }
        RiseAndSetEvent SunRiseAndSet { get; }
        AsyncObservableCollection<DataPoint> TwilightDuration { get; }
        RiseAndSetEvent TwilightRiseAndSet { get; }

        Astrometry.MoonPhase MoonPhase { get; }

        double? Illumination { get; }

        DateTime SelectedDate { get; set; }
    }
}