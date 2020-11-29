#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Model;
using NINA.Profile;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Enum;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.SequenceItem.Utility {

    [ExportMetadata("Name", "Lbl_SequenceItem_Utility_WaitForMoonAltitude_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Utility_WaitForMoonAltitude_Description")]
    [ExportMetadata("Icon", "WaningGibbousMoonSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Utility")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class WaitForMoonAltitude : SequenceItem {
        private readonly IProfileService profileService;
        private double userMoonAltitude;
        private double currentMoonAltitude;
        private ComparisonOperatorEnum comparator;

        [ImportingConstructor]
        public WaitForMoonAltitude(IProfileService profileService) {
            this.profileService = profileService;
            UserMoonAltitude = 0d;
            Comparator = ComparisonOperatorEnum.GREATER_THAN;

            UpdateCurrentMoonState();
        }

        [JsonProperty]
        public double UserMoonAltitude {
            get => userMoonAltitude;
            set {
                userMoonAltitude = value;
                RaisePropertyChanged();
                UpdateCurrentMoonState();
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

        public double CurrentMoonAltitude {
            get => currentMoonAltitude;
            set {
                currentMoonAltitude = value;
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
                UpdateCurrentMoonState();

                bool mustWait = false;

                switch (Comparator) {
                    case ComparisonOperatorEnum.LESS_THAN:
                        if (CurrentMoonAltitude < UserMoonAltitude) { mustWait = true; }
                        break;

                    case ComparisonOperatorEnum.LESS_THAN_OR_EQUAL:
                        if (CurrentMoonAltitude <= UserMoonAltitude) { mustWait = true; }
                        break;

                    case ComparisonOperatorEnum.GREATER_THAN:
                        if (CurrentMoonAltitude > UserMoonAltitude) { mustWait = true; }
                        break;

                    case ComparisonOperatorEnum.GREATER_THAN_OR_EQUAL:
                        if (CurrentMoonAltitude >= UserMoonAltitude) { mustWait = true; }
                        break;
                }

                if (mustWait) {
                    progress.Report(new ApplicationStatus() {
                        Status = string.Format(Locale.Loc.Instance["Lbl_SequenceItem_Utility_WaitForMoonAltitude_Progress"],
                        Math.Round(CurrentMoonAltitude, 2),
                        AttributeHelper.GetDescription(Comparator),
                        Math.Round(UserMoonAltitude, 2))
                    });

                    await NINA.Utility.Utility.Delay(TimeSpan.FromSeconds(1), token);
                } else {
                    break;
                }
            } while (true);
        }

        public override object Clone() {
            return new WaitForMoonAltitude(profileService) {
                Icon = Icon,
                Name = Name,
                Category = Category,
                Description = Description,
                UserMoonAltitude = UserMoonAltitude
            };
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(WaitForMoonAltitude)}, UserMoonAltitude: {UserMoonAltitude}, Compartor: {Comparator}, CurrentMoonAltitude: {CurrentMoonAltitude}";
        }

        private void UpdateCurrentMoonState() {
            var latlong = profileService.ActiveProfile.AstrometrySettings;
            var now = DateTime.UtcNow;

            CurrentMoonAltitude = Astrometry.GetMoonAltitude(now, latlong.Latitude, latlong.Longitude);
        }
    }
}