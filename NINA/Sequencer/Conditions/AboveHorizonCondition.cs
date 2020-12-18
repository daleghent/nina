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
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Utility;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Sequencer.Conditions {

    [ExportMetadata("Name", "Lbl_SequenceCondition_AboveHorizonCondition_Name")]
    [ExportMetadata("Description", "Lbl_SequenceCondition_AboveHorizonCondition_Description")]
    [ExportMetadata("Icon", "WaitForAltitudeSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Condition")]
    [Export(typeof(ISequenceCondition))]
    [JsonObject(MemberSerialization.OptIn)]
    public class AboveHorizonCondition : SequenceCondition {
        private readonly IProfileService profileService;
        private InputCoordinates coordinates;
        private bool isEastOfMeridian;
        private double currentAltitude;
        private double currentAlzimuth;
        private string risingSettingDisplay;
        private bool hasDsoParent;
        private double horizonAltitude;

        [ImportingConstructor]
        public AboveHorizonCondition(IProfileService profileService) {
            this.profileService = profileService;
            Coordinates = new InputCoordinates();

            //todo: find a better home for this
            Task.Run(async () => {
                await Task.Delay(15000);
                while (true) {
                    try {
                        CalculateCurrentAltitude();
                    } catch (Exception ee) {
                        Logger.Error(ee);
                    }
                    await Task.Delay(5000);
                }
            });
        }

        [JsonProperty]
        public InputCoordinates Coordinates {
            get => coordinates;
            set {
                if (coordinates != null) {
                    coordinates.PropertyChanged -= Coordinates_PropertyChanged;
                }
                coordinates = value;
                if (coordinates != null) {
                    coordinates.PropertyChanged += Coordinates_PropertyChanged;
                }
                RaisePropertyChanged();
                CalculateCurrentAltitude();
            }
        }

        private void Coordinates_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            CalculateCurrentAltitude();
        }

        [JsonProperty]
        public bool HasDsoParent {
            get => hasDsoParent;
            set {
                hasDsoParent = value;
                RaisePropertyChanged();
            }
        }

        public double CurrentAltitude {
            get => currentAltitude;
            private set {
                currentAltitude = value;
                RaisePropertyChanged();
            }
        }

        public double HorizonAltitude {
            get => horizonAltitude;
            private set {
                horizonAltitude = value;
                RaisePropertyChanged();
            }
        }

        public double CurrentAzimuth {
            get => currentAlzimuth;
            private set {
                currentAlzimuth = value;
                RaisePropertyChanged();
            }
        }

        public string RisingSettingDisplay {
            get => risingSettingDisplay;
            private set {
                risingSettingDisplay = value;
                RaisePropertyChanged();
            }
        }

        public bool IsEastOfMeridian {
            get => isEastOfMeridian;
            private set {
                isEastOfMeridian = value;
                RaisePropertyChanged();
            }
        }

        public override object Clone() {
            return new AboveHorizonCondition(profileService) {
                Icon = Icon,
                Name = Name,
                Category = Category,
                Description = Description,
                Coordinates = Coordinates.Clone()
            };
        }

        public override void AfterParentChanged() {
            var coordinates = ItemUtility.RetrieveContextCoordinates(this.Parent).Item1;
            if (coordinates != null) {
                Coordinates.Coordinates = coordinates;
                HasDsoParent = true;
            } else {
                HasDsoParent = false;
            }
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(AboveHorizonCondition)}";
        }

        public override void ResetProgress() {
        }

        public override void SequenceBlockFinished() {
        }

        public override void SequenceBlockStarted() {
        }

        public override bool Check(ISequenceItem nextItem) {
            CalculateCurrentAltitude();

            var horizon = profileService.ActiveProfile.AstrometrySettings.Horizon;
            HorizonAltitude = 0d;
            if (horizon != null) {
                HorizonAltitude = Math.Round(horizon.GetAltitude(CurrentAzimuth), 2);
            }

            return CurrentAltitude >= HorizonAltitude;
        }

        private void CalculateCurrentAltitude() {
            var location = profileService.ActiveProfile.AstrometrySettings;
            var altaz = Coordinates
                .Coordinates
                .Transform(
                    Angle.ByDegree(location.Latitude),
                    Angle.ByDegree(location.Longitude));
            IsEastOfMeridian = altaz.AltitudeSite == AltitudeSite.EAST;
            var currentAlt = Math.Round(altaz.Altitude.Degree, 2);
            var currentAz = Math.Round(altaz.Azimuth.Degree, 2);
            CurrentAltitude = currentAlt;
            CurrentAzimuth = currentAz;
            RisingSettingDisplay =
                IsEastOfMeridian ? "^" : "v";
        }
    }
}