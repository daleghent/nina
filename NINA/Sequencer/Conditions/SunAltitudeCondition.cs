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
using NINA.Profile;
using NINA.Sequencer.SequenceItem;
using NINA.Utility;
using NINA.Utility.Astrometry;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace NINA.Sequencer.Conditions {

    [ExportMetadata("Name", "Lbl_SequenceCondition_SunAltitudeCondition_Name")]
    [ExportMetadata("Description", "Lbl_SequenceCondition_SunAltitudeCondition_Description")]
    [ExportMetadata("Icon", "SunriseSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Condition")]
    [Export(typeof(ISequenceCondition))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SunAltitudeCondition : SequenceCondition {
        private readonly IProfileService profileService;
        private double userSunAltitude;
        private double currentSunAltitude;
        private ComparisonOperatorEnum comparator;

        [ImportingConstructor]
        public SunAltitudeCondition(IProfileService profileService) {
            this.profileService = profileService;
            UserSunAltitude = 0d;
            Comparator = ComparisonOperatorEnum.GREATER_THAN;

            CalculateCurrentSunState();

            //todo: find a better home for this
            Task.Run(async () => {
                await Task.Delay(15000);
                while (true) {
                    try {
                        CalculateCurrentSunState();
                    } catch (Exception ee) {
                        Logger.Error(ee);
                    }
                    await Task.Delay(5000);
                }
            });
        }

        [JsonProperty]
        public double UserSunAltitude {
            get => userSunAltitude;
            set {
                userSunAltitude = value;
                RaisePropertyChanged();
                CalculateCurrentSunState();
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

        public override object Clone() {
            return new SunAltitudeCondition(profileService) {
                Icon = Icon,
                Name = Name,
                Category = Category,
                Description = Description,
                UserSunAltitude = UserSunAltitude,
                Comparator = Comparator
            };
        }

        public override void AfterParentChanged() {
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(SunAltitudeCondition)}, " +
                $"UserSunAltitude: {UserSunAltitude}, CurrentSunAltitude: {CurrentSunAltitude}, Comparator: {Comparator}";
        }

        public override void ResetProgress() {
        }

        public override void SequenceBlockFinished() {
        }

        public override void SequenceBlockStarted() {
        }

        public override bool Check(ISequenceItem nextItem) {
            // See if the sun's altitude is outside of the user's wishes
            switch (Comparator) {
                case ComparisonOperatorEnum.LESS_THAN:
                    if (CurrentSunAltitude < UserSunAltitude) { return false; }
                    break;

                case ComparisonOperatorEnum.LESS_THAN_OR_EQUAL:
                    if (CurrentSunAltitude <= UserSunAltitude) { return false; }
                    break;

                case ComparisonOperatorEnum.GREATER_THAN:
                    if (CurrentSunAltitude > UserSunAltitude) { return false; }
                    break;

                case ComparisonOperatorEnum.GREATER_THAN_OR_EQUAL:
                    if (CurrentSunAltitude >= UserSunAltitude) { return false; }
                    break;
            }

            // Everything is fine
            return true;
        }

        private void CalculateCurrentSunState() {
            var latlong = profileService.ActiveProfile.AstrometrySettings;
            var now = DateTime.UtcNow;

            CurrentSunAltitude = Astrometry.GetSunAltitude(now, latlong.Latitude, latlong.Longitude);
        }
    }
}