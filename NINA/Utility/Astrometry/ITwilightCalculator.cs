using System;

namespace NINA.Utility.Astrometry {

    public interface ITwilightCalculator {

        TimeSpan GetTwilightDuration(DateTime date, double latitude, double longitude);
    }
}