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
using NINA.Profile;
using NINA.Sequencer.SequenceItem;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Enum;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace NINA.Sequencer.Conditions {

    [ExportMetadata("Name", "Lbl_SequenceCondition_MoonIlluminationCondition_Name")]
    [ExportMetadata("Description", "Lbl_SequenceCondition_MoonIlluminationCondition_Description")]
    [ExportMetadata("Icon", "BrightnessSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Condition")]
    [Export(typeof(ISequenceCondition))]
    [JsonObject(MemberSerialization.OptIn)]
    public class MoonIlluminationCondition : SequenceCondition {
        private readonly IProfileService profileService;
        private double userMoonIllumination;
        private double currentMoonIllumination;
        private ComparisonOperatorEnum comparator;

        [ImportingConstructor]
        public MoonIlluminationCondition() {
            UserMoonIllumination = 0d;
            Comparator = ComparisonOperatorEnum.GREATER_THAN;

            CalculateCurrentMoonState();

            //todo: find a better home for this
            Task.Run(async () => {
                await Task.Delay(15000);
                while (true) {
                    try {
                        CalculateCurrentMoonState();
                    } catch (Exception ee) {
                        Logger.Error(ee);
                    }
                    await Task.Delay(5000);
                }
            });
        }

        [JsonProperty]
        public double UserMoonIllumination {
            get => userMoonIllumination;
            set {
                userMoonIllumination = value;
                RaisePropertyChanged();
                CalculateCurrentMoonState();
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

        public double CurrentMoonIllumination {
            get => currentMoonIllumination;
            private set {
                currentMoonIllumination = value;
                RaisePropertyChanged();
            }
        }

        public ComparisonOperatorEnum[] ComparisonOperators => Enum.GetValues(typeof(ComparisonOperatorEnum))
            .Cast<ComparisonOperatorEnum>()
            .Where(p => p != ComparisonOperatorEnum.EQUALS)
            .Where(p => p != ComparisonOperatorEnum.NOT_EQUAL)
            .ToArray();

        public override object Clone() {
            return new MoonIlluminationCondition() {
                Icon = Icon,
                Name = Name,
                Category = Category,
                Description = Description,
                UserMoonIllumination = UserMoonIllumination,
                Comparator = Comparator
            };
        }

        public override void AfterParentChanged() {
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(MoonIlluminationCondition)}, " +
                $"CurrentMoonIllumination: { CurrentMoonIllumination}%, Comparator: {Comparator}, UserMoonIllumination: {UserMoonIllumination}%";
        }

        public override void ResetProgress() {
        }

        public override void SequenceBlockFinished() {
        }

        public override void SequenceBlockStarted() {
        }

        public override bool Check(ISequenceItem nextItem) {
            // See if the moon's illumination is outside of the user's wishes
            switch (Comparator) {
                case ComparisonOperatorEnum.LESS_THAN:
                    if (CurrentMoonIllumination < UserMoonIllumination) { return false; }
                    break;

                case ComparisonOperatorEnum.LESS_THAN_OR_EQUAL:
                    if (CurrentMoonIllumination <= UserMoonIllumination) { return false; }
                    break;

                case ComparisonOperatorEnum.GREATER_THAN:
                    if (CurrentMoonIllumination > UserMoonIllumination) { return false; }
                    break;

                case ComparisonOperatorEnum.GREATER_THAN_OR_EQUAL:
                    if (CurrentMoonIllumination >= UserMoonIllumination) { return false; }
                    break;
            }

            // Everything is fine
            return true;
        }

        private void CalculateCurrentMoonState() {
            var now = DateTime.UtcNow;

            CurrentMoonIllumination = Astrometry.GetMoonIllumination(now) * 100;
        }
    }
}