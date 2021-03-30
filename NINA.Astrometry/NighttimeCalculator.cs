#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Profile;
using NINA.Utility;
using System;
using System.Collections.Generic;

namespace NINA.Astrometry {

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
                    var twilightRiseAndSet = AstroUtil.GetNightTimes(referenceDate, latitude, longitude);
                    var nauticalTwilightRiseAndSet = AstroUtil.GetNauticalNightTimes(referenceDate, latitude, longitude);
                    var moonRiseAndSet = AstroUtil.GetMoonRiseAndSet(referenceDate, latitude, longitude);
                    var sunRiseAndSet = AstroUtil.GetSunRiseAndSet(referenceDate, latitude, longitude);
                    var moonPhase = AstroUtil.GetMoonPhase(referenceDate);
                    var illumination = AstroUtil.GetMoonIllumination(referenceDate);

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