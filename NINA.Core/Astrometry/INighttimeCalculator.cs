using System;

namespace NINA.Astrometry {

    public interface INighttimeCalculator {

        NighttimeData Calculate(DateTime? selectedDate = null);
    }
}