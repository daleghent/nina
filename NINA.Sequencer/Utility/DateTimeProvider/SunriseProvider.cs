#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Utility;
using NINA.Astrometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Sequencer.Utility.DateTimeProvider {

    [JsonObject(MemberSerialization.OptIn)]
    public class SunriseProvider : IDateTimeProvider {
        private INighttimeCalculator nighttimeCalculator;

        public SunriseProvider(INighttimeCalculator nighttimeCalculator) {
            this.nighttimeCalculator = nighttimeCalculator;
        }

        public string Name { get; } = Locale.Loc.Instance["LblSunRise"];
        public ICustomDateTime DateTime { get; set; } = new SystemDateTime();

        public DateTime GetDateTime(ISequenceEntity context) {
            var night = nighttimeCalculator.Calculate().SunRiseAndSet.Rise;
            if (!night.HasValue) {
                night = DateTime.Now;
            }
            return night.Value;
        }
    }
}