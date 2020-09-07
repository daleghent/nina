using OxyPlot;
using System;

namespace NINA.Utility.Astrometry {

    public interface INighttimeCalculator {
        NighttimeData Calculate();
    }
}