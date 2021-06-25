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
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using NINA.Astrometry;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Locale;

namespace NINA.Sequencer.SequenceItem.Utility {

    [ExportMetadata("Name", "Lbl_SequenceItem_Utility_WaitForSunAltitude_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Utility_WaitForSunAltitude_Description")]
    [ExportMetadata("Icon", "SunriseSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Utility")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class WaitForSunAltitude : SequenceItem {
        private readonly IProfileService profileService;
        private double userSunAltitude;
        private double currentSunAltitude;
        private ComparisonOperatorEnum comparator;

        [ImportingConstructor]
        public WaitForSunAltitude(IProfileService profileService) {
            this.profileService = profileService;
            UserSunAltitude = 0d;
            Comparator = ComparisonOperatorEnum.GREATER_THAN;

            UpdateCurrentSunState();
        }

        private WaitForSunAltitude(WaitForSunAltitude cloneMe) : this(cloneMe.profileService) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new WaitForSunAltitude(this) {
                Comparator = Comparator,
                UserSunAltitude = UserSunAltitude
            };
        }

        [JsonProperty]
        public double UserSunAltitude {
            get => userSunAltitude;
            set {
                userSunAltitude = value;
                RaisePropertyChanged();
                UpdateCurrentSunState();
            }
        }

        [JsonProperty]
        public ComparisonOperatorEnum Comparator {
            get => comparator;
            set {
                comparator = value;
                RaisePropertyChanged();
            }
        }

        public double CurrentSunAltitude {
            get => currentSunAltitude;
            set {
                currentSunAltitude = value;
                RaisePropertyChanged();
            }
        }

        public ComparisonOperatorEnum[] ComparisonOperators => Enum.GetValues(typeof(ComparisonOperatorEnum))
            .Cast<ComparisonOperatorEnum>()
            .Where(p => p != ComparisonOperatorEnum.EQUALS)
            .Where(p => p != ComparisonOperatorEnum.NOT_EQUAL)
            .ToArray();

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            do {
                UpdateCurrentSunState();

                bool mustWait = false;

                switch (Comparator) {
                    case ComparisonOperatorEnum.LESS_THAN:
                        if (CurrentSunAltitude < UserSunAltitude) { mustWait = true; }
                        break;

                    case ComparisonOperatorEnum.LESS_THAN_OR_EQUAL:
                        if (CurrentSunAltitude <= UserSunAltitude) { mustWait = true; }
                        break;

                    case ComparisonOperatorEnum.GREATER_THAN:
                        if (CurrentSunAltitude > UserSunAltitude) { mustWait = true; }
                        break;

                    case ComparisonOperatorEnum.GREATER_THAN_OR_EQUAL:
                        if (CurrentSunAltitude >= UserSunAltitude) { mustWait = true; }
                        break;
                }

                if (mustWait) {
                    progress.Report(new ApplicationStatus() {
                        Status = string.Format(Loc.Instance["Lbl_SequenceItem_Utility_WaitForSunAltitude_Progress"],
                            Math.Round(CurrentSunAltitude, 2),
                            AttributeHelper.GetDescription(Comparator),
                            Math.Round(UserSunAltitude, 2))
                    });

                    await NINA.Core.Utility.CoreUtil.Delay(TimeSpan.FromSeconds(1), token);
                } else {
                    break;
                }
            } while (true);
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(WaitForSunAltitude)}, UserSunAltitude: {UserSunAltitude}, Compartor: {Comparator}, CurrentSunAltitude: {CurrentSunAltitude}";
        }

        private void UpdateCurrentSunState() {
            var latlong = profileService.ActiveProfile.AstrometrySettings;
            var now = DateTime.UtcNow;

            CurrentSunAltitude = AstroUtil.GetSunAltitude(now, latlong.Latitude, latlong.Longitude);
        }
    }
}