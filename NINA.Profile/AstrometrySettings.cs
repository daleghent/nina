#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Profile.Interfaces;
using System;
using System.Runtime.Serialization;

namespace NINA.Profile {

    [Serializable()]
    [DataContract]
    public class AstrometrySettings : Settings, IAstrometrySettings {

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context) {
            try {
                if (!string.IsNullOrWhiteSpace(HorizonFilePath)) {
                    Horizon = CustomHorizon.FromFile(HorizonFilePath);
                }
            } catch (Exception) {
            }
        }

        protected override void SetDefaultValues() {
            hemisphereType = Hemisphere.NORTHERN;
            latitude = 0;
            longitude = 0;
            horizonFilePath = string.Empty;
        }

        private Hemisphere hemisphereType;

        [DataMember]
        public Hemisphere HemisphereType {
            get {
                return hemisphereType;
            }
            set {
                if (hemisphereType != value) {
                    hemisphereType = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double latitude;

        [DataMember]
        public double Latitude {
            get {
                return latitude;
            }
            set {
                if (latitude != value) {
                    latitude = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double longitude;

        [DataMember]
        public double Longitude {
            get {
                return longitude;
            }
            set {
                if (longitude != value) {
                    longitude = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string horizonFilePath;

        [DataMember]
        public string HorizonFilePath {
            get => horizonFilePath;
            set {
                if (horizonFilePath != value) {
                    horizonFilePath = value;
                    RaisePropertyChanged();
                }
            }
        }

        private CustomHorizon horizon;

        public CustomHorizon Horizon {
            get => horizon;
            set {
                horizon = value;
                RaisePropertyChanged();
            }
        }
    }
}