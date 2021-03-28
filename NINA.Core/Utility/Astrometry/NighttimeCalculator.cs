using NINA.Profile;
using System;
using System.Collections.Generic;

namespace NINA.Utility.Astrometry {

    public class NighttimeCalculator : BaseINPC, INighttimeCalculator {
        private readonly IProfileService profileService;

        public NighttimeCalculator(IProfileService profile) {
            profileService = profile;
            Cache = new Dictionary<string, NighttimeData>();
        }

        private IDictionary<string, NighttimeData> Cache;

        private object lockObj = new object();

        public NighttimeData Calculate(DateTime? date = null) {
            lock (lockObj) {
                var selectedDate = date.HasValue ? date.Value : DateTime.Now;
                var referenceDate = GetReferenceDate(selectedDate);
                var latitude = profileService.ActiveProfile.AstrometrySettings.Latitude;
                var longitude = profileService.ActiveProfile.AstrometrySettings.Longitude;

                var key = $"{referenceDate:yyyy-MM-dddd-HH-mm-ss}_{latitude}_{longitude}";

                if (Cache.TryGetValue(key, out var nighttimeData)) {
                    return nighttimeData;
                } else {
                    var twilightRiseAndSet = Astrometry.GetNightTimes(referenceDate, latitude, longitude);
                    var nauticalTwilightRiseAndSet = Astrometry.GetNauticalNightTimes(referenceDate, latitude, longitude);
                    var moonRiseAndSet = Astrometry.GetMoonRiseAndSet(referenceDate, latitude, longitude);
                    var sunRiseAndSet = Astrometry.GetSunRiseAndSet(referenceDate, latitude, longitude);
                    var moonPhase = Astrometry.GetMoonPhase(referenceDate);
                    var illumination = Astrometry.GetMoonIllumination(referenceDate);

                    var data = new NighttimeData(date: selectedDate, referenceDate: referenceDate, moonPhase: moonPhase, moonIllumination: illumination, twilightRiseAndSet: twilightRiseAndSet, nauticalTwilightRiseAndSet: nauticalTwilightRiseAndSet,
                        sunRiseAndSet: sunRiseAndSet, moonRiseAndSet: moonRiseAndSet);
                    Cache[key] = data;
                    return data;
                }
            }
        }

        public static DateTime GetReferenceDate(DateTime reference) {
            DateTime d = reference;
            if (d.Hour > 12) {
                d = new DateTime(d.Year, d.Month, d.Day, 12, 0, 0, reference.Kind);
            } else {
                var tmp = d.AddDays(-1);
                d = new DateTime(tmp.Year, tmp.Month, tmp.Day, 12, 0, 0, reference.Kind);
            }
            return d;
        }
    }
}