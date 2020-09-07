using NINA.Profile;
using System;

namespace NINA.Utility.Astrometry {

    internal class NighttimeCalculator : BaseINPC, INighttimeCalculator {
        private readonly IProfileService profileService;

        public NighttimeCalculator(IProfileService profile) {
            profileService = profile;
        }

        public NighttimeData Calculate(DateTime? date = null) {
            var selectedDate = date.HasValue ? date.Value : DateTime.UtcNow;
            var referenceDate = GetReferenceDate(selectedDate);
            var twilightRiseAndSet = Astrometry.GetNightTimes(referenceDate, profileService.ActiveProfile.AstrometrySettings.Latitude, profileService.ActiveProfile.AstrometrySettings.Longitude);
            var moonRiseAndSet = Astrometry.GetMoonRiseAndSet(referenceDate, profileService.ActiveProfile.AstrometrySettings.Latitude, profileService.ActiveProfile.AstrometrySettings.Longitude);
            var sunRiseAndSet = Astrometry.GetSunRiseAndSet(referenceDate, profileService.ActiveProfile.AstrometrySettings.Latitude, profileService.ActiveProfile.AstrometrySettings.Longitude);
            var moonPhase = Astrometry.GetMoonPhase(referenceDate);
            var illumination = Astrometry.GetMoonIllumination(referenceDate);
            return new NighttimeData(date: selectedDate, referenceDate: referenceDate, moonPhase: moonPhase, moonIllumination: illumination, twilightRiseAndSet: twilightRiseAndSet, 
                sunRiseAndSet: sunRiseAndSet, moonRiseAndSet: moonRiseAndSet);
        }

        public static DateTime GetReferenceDate(DateTime reference) {
            DateTime d = reference;
            if (d.Hour > 12) {
                d = new DateTime(d.Year, d.Month, d.Day, 12, 0, 0, DateTimeKind.Utc);
            } else {
                var tmp = d.AddDays(-1);
                d = new DateTime(tmp.Year, tmp.Month, tmp.Day, 12, 0, 0, DateTimeKind.Utc);
            }
            return d;
        }
    }
}