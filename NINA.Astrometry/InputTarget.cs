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
using NINA.Core.Utility;
using NINA.Astrometry;
using System;
using NINA.Core.Model;

namespace NINA.Astrometry {

    [JsonObject(MemberSerialization.OptIn)]
    public class InputTarget : BaseINPC {

        public InputTarget(Angle latitude, Angle longitude, CustomHorizon horizon) {
            this.latitude = latitude;
            this.longitude = longitude;
            DeepSkyObject = new DeepSkyObject(string.Empty, new Coordinates(Angle.Zero, Angle.Zero, Epoch.J2000), string.Empty, horizon);
            DeepSkyObject.SetDateAndPosition(NighttimeCalculator.GetReferenceDate(DateTime.Now), latitude.Degree, longitude.Degree);
            InputCoordinates = new InputCoordinates();
        }

        private void InputCoordinates_OnCoordinatesChanged(object sender, EventArgs e) {
            RaiseCoordinatesChanged();
        }

        private bool expanded = true;

        [JsonProperty]
        public bool Expanded {
            get => expanded;
            set {
                expanded = value;
                RaisePropertyChanged();
            }
        }

        private DeepSkyObject deepSkyObject;

        public DeepSkyObject DeepSkyObject {
            get => deepSkyObject;
            set {
                deepSkyObject = value;
                RaisePropertyChanged();
            }
        }

        private string targetName;

        [JsonProperty]
        public string TargetName {
            get {
                return targetName;
            }
            set {
                targetName = value;
                RaisePropertyChanged();
                RaiseCoordinatesChanged();
            }
        }

        private double rotation;

        [JsonProperty]
        public double Rotation {
            get {
                return rotation;
            }
            set {
                rotation = value;
                RaiseCoordinatesChanged();
            }
        }

        private InputCoordinates inputCoordinates;
        private Angle latitude;
        private Angle longitude;

        [JsonProperty]
        public InputCoordinates InputCoordinates {
            get => inputCoordinates;
            set {
                if (inputCoordinates != null) {
                    InputCoordinates.CoordinatesChanged -= InputCoordinates_OnCoordinatesChanged;
                }
                inputCoordinates = value;
                if (inputCoordinates != null) {
                    InputCoordinates.CoordinatesChanged += InputCoordinates_OnCoordinatesChanged; ;
                }
                RaiseCoordinatesChanged();
            }
        }

        public void SetPosition(Angle latitude, Angle longitude) {
            this.latitude = latitude;
            this.longitude = longitude;
            if (this.DeepSkyObject != null) {
                this.DeepSkyObject.SetDateAndPosition(NighttimeCalculator.GetReferenceDate(DateTime.Now), latitude.Degree, longitude.Degree);
                RaiseCoordinatesChanged();
            }
        }

        private void RaiseCoordinatesChanged() {
            RaisePropertyChanged(nameof(Rotation));
            RaisePropertyChanged(nameof(InputCoordinates));

            DeepSkyObject.Name = TargetName;
            DeepSkyObject.Coordinates = InputCoordinates.Coordinates;
            DeepSkyObject.Rotation = Rotation;

            this.CoordinatesChanged?.Invoke(this, new EventArgs());
        }

        public event EventHandler CoordinatesChanged;

        public override string ToString() {
            return $"{InputCoordinates}; Rotation: {Rotation}";
        }
    }
}