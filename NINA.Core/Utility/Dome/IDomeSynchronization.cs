using NINA.Core.Enum;
using NINA.Astrometry;

namespace NINA.Utility {

    public interface IDomeSynchronization {

        Angle TargetDomeAzimuth(
            Coordinates scopeCoordinates,
            double localSiderealTime,
            Angle siteLatitude,
            Angle siteLongitude,
            PierSide sideOfPier);
    }
}