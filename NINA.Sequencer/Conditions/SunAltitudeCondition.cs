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
using NINA.Profile.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.Core.Utility;
using NINA.Astrometry;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NINA.Sequencer.Utility;
using System.Runtime.Serialization;

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
            ConditionWatchdog = new ConditionWatchdog(InterruptWhenSunOutsideOfBounds, TimeSpan.FromSeconds(5));
        }

        private async Task InterruptWhenSunOutsideOfBounds() {
            CalculateCurrentSunState();
            if (!Check(null, null)) {
                if (this.Parent != null) {
                    Logger.Info("Sun is outside of the specified range - Interrupting current Instruction Set");
                    await this.Parent.Interrupt();
                }
            }
        }

        private SunAltitudeCondition(SunAltitudeCondition cloneMe) : this(cloneMe.profileService) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new SunAltitudeCondition(this) {
                UserSunAltitude = UserSunAltitude,
                Comparator = Comparator
            };
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context) {
            RunWatchdogIfInsideSequenceRoot();
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

        public override void AfterParentChanged() {
            RunWatchdogIfInsideSequenceRoot();
        }

        public override string ToString() {
            return $"Condition: {nameof(SunAltitudeCondition)}, " +
                $"CurrentSunAltitude: {CurrentSunAltitude}, Comparator: {Comparator}, UserSunAltitude: {UserSunAltitude}";
        }

        public override bool Check(ISequenceItem previousItem, ISequenceItem nextItem) {
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

            CurrentSunAltitude = AstroUtil.GetSunAltitude(now, latlong.Latitude, latlong.Longitude);
        }
    }
}