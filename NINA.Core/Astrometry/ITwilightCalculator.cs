using System;

namespace NINA.Astrometry {

    public interface ITwilightCalculator {

        TimeSpan GetTwilightDuration(DateTime date, double latitude, double longitude);
    }
}