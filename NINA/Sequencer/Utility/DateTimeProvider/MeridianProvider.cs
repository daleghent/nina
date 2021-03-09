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
using NINA.Profile;
using NINA.Utility;
using NINA.Utility.Astrometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Sequencer.Utility.DateTimeProvider {

    [JsonObject(MemberSerialization.OptIn)]
    public class MeridianProvider : IDateTimeProvider {
        private IProfileService profileService;

        public MeridianProvider(IProfileService profileService) {
            this.profileService = profileService;
        }

        public string Name { get; } = Locale.Loc.Instance["LblMeridian"];
        public ICustomDateTime DateTime { get; set; } = new SystemDateTime();

        public DateTime GetDateTime(ISequenceEntity context) {
            var contextCoordinates = Utility.ItemUtility.RetrieveContextCoordinates(context?.Parent);
            if (contextCoordinates.Item1 != null) {
                var siderealTime = Angle.ByHours(Astrometry.GetLocalSiderealTime(DateTime.Now, profileService.ActiveProfile.AstrometrySettings.Longitude));
                var timeToMeridian = MeridianFlip.TimeToMeridian(contextCoordinates.Item1, siderealTime);
                return DateTime.Now + timeToMeridian;
            }
            return DateTime.Now;
        }
    }
}