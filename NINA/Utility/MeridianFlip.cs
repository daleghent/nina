using NINA.Model.MyTelescope;
using NINA.Profile;
using NINA.Utility.Astrometry;
using System;

namespace NINA.Utility {
    public static class MeridianFlip {

        public static TimeSpan TimeToMeridian(Coordinates coordinates, Angle localSiderealTime) {
            coordinates = coordinates.Transform(Epoch.JNOW);
            var rightAscension = Angle.ByHours(coordinates.RA);
            var hoursToMeridian = (rightAscension.Hours - localSiderealTime.Hours) % 12.0;
            if (hoursToMeridian < 0.0) {
                hoursToMeridian += 12.0;
            }
            return TimeSpan.FromHours(hoursToMeridian);
        }

        public static PierSide ExpectedPierSide(Coordinates coordinates, Angle localSiderealTime) {
            coordinates = coordinates.Transform(Epoch.JNOW);
            var rightAscension = Angle.ByHours(coordinates.RA);
            var hoursToLST = (rightAscension.Hours - localSiderealTime.Hours) % 24.0;
            if (hoursToLST < 0.0) {
                hoursToLST += 24.0;
            }

            if (hoursToLST < 12.0) {
                return PierSide.pierWest;
            } else {
                return PierSide.pierEast;
            }
        }

        public static TimeSpan TimeToMeridianFlip(
            IMeridianFlipSettings settings,
            Coordinates coordinates,
            Angle localSiderealTime,
            PierSide currentSideOfPier) {
            var timeToMeridian = TimeToMeridian(coordinates: coordinates, localSiderealTime: localSiderealTime);
            if (settings.UseSideOfPier) {
                if (currentSideOfPier == PierSide.pierUnknown) {
                    throw new ArgumentException("UseSideOfPier is enabled but pier side is Unknown");
                }
                var expectedPierSide = ExpectedPierSide(coordinates: coordinates, localSiderealTime: localSiderealTime);
                if (expectedPierSide != currentSideOfPier) {
                    // The current side of pier is not what the expected pier side should be, which means the next transit
                    // won't require a flip. Thus, the next meridian flip won't be for another 12 hours
                    timeToMeridian += TimeSpan.FromHours(12.0);
                }
            }

            var timeToMeridianFlip = timeToMeridian + TimeSpan.FromMinutes(settings.MaxMinutesAfterMeridian);
            if (timeToMeridianFlip >= TimeSpan.FromDays(1)) {
                timeToMeridianFlip -= TimeSpan.FromDays(1);
            }
            return timeToMeridianFlip;
        }
    }
}
