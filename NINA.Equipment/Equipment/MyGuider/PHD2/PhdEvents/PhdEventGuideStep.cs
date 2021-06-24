#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Runtime.Serialization;
using NINA.Core.Interfaces;

namespace NINA.Equipment.Equipment.MyGuider.PHD2.PhdEvents {

    [DataContract]
    public class PhdEventGuideStep : PhdEvent, IGuideStep {

        [DataMember]
        private double frame;

        [DataMember]
        private double time;

        [DataMember]
        private string mount;

        [DataMember]
        private double dx;

        [DataMember]
        private double dy;

        [DataMember]
        private double rADistanceRaw;

        [DataMember]
        private double decDistanceRaw;

        [DataMember]
        private double raDistanceDisplay;

        [DataMember]
        private double decDistanceDisplay;

        [DataMember]
        private double rADistanceGuide;

        [DataMember]
        private double decDistanceGuide;

        [DataMember]
        private double raDistanceGuideDisplay;

        [DataMember]
        private double decDistanceGuideDisplay;

        [DataMember]
        private double rADuration;

        [DataMember]
        private string rADirection;

        [DataMember]
        private double dECDuration;

        [DataMember]
        private string decDirection;

        [DataMember]
        private double starMass;

        [DataMember]
        private double sNR;

        [DataMember]
        private double avgDist;

        [DataMember]
        private bool rALimited;

        [DataMember]
        private bool decLimited;

        [DataMember]
        private double errorCode;

        public PhdEventGuideStep() {
        }

        [DataMember]
        public double RADistanceGuideDisplay {
            get {
                return raDistanceGuideDisplay;
            }
            set {
                raDistanceGuideDisplay = value;
            }
        }

        [DataMember]
        public double DecDistanceGuideDisplay {
            get {
                return decDistanceGuideDisplay;
            }
            set {
                decDistanceGuideDisplay = value;
            }
        }

        [DataMember]
        public double Frame {
            get {
                return frame;
            }

            set {
                frame = value;
            }
        }

        [DataMember]
        public double Time {
            get {
                return time;
            }

            set {
                time = DateTime.UtcNow
               .Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
               .TotalSeconds;
            }
        }

        [DataMember]
        public string Mount {
            get {
                return mount;
            }

            set {
                mount = value;
            }
        }

        [DataMember]
        public double Dx {
            get {
                return dx;
            }

            set {
                dx = value;
            }
        }

        [DataMember]
        public double Dy {
            get {
                return dy;
            }

            set {
                dy = value;
            }
        }

        [DataMember]
        public double RADistanceRaw {
            get {
                return -rADistanceRaw;
            }

            set {
                rADistanceRaw = value;
            }
        }

        [DataMember]
        public double DECDistanceRaw {
            get {
                return decDistanceRaw;
            }

            set {
                decDistanceRaw = value;
            }
        }

        [DataMember]
        public double RADistanceGuide {
            get {
                return rADistanceGuide;
            }

            set {
                rADistanceGuide = value;
                RADistanceGuideDisplay = RADistanceGuide;
            }
        }

        [DataMember]
        public double DECDistanceGuide {
            get {
                return decDistanceGuide;
            }

            set {
                decDistanceGuide = value;
                DecDistanceGuideDisplay = DECDistanceRaw;
            }
        }

        [DataMember]
        public double RADuration {
            get {
                if (RADirection == "East") {
                    return -rADuration;
                } else {
                    return rADuration;
                }
            }

            set {
                rADuration = value;
            }
        }

        [DataMember]
        public string RADirection {
            get {
                return rADirection;
            }

            set {
                rADirection = value;
            }
        }

        [DataMember]
        public double DECDuration {
            get {
                if (DECDirection == "South") {
                    return -dECDuration;
                } else {
                    return dECDuration;
                }
            }

            set {
                dECDuration = value;
            }
        }

        [DataMember]
        public string DECDirection {
            get {
                return decDirection;
            }

            set {
                decDirection = value;
            }
        }

        [DataMember]
        public double StarMass {
            get {
                return starMass;
            }

            set {
                starMass = value;
            }
        }

        [DataMember]
        public double SNR {
            get {
                return sNR;
            }

            set {
                sNR = value;
            }
        }

        [DataMember]
        public double AvgDist {
            get {
                return avgDist;
            }

            set {
                avgDist = value;
            }
        }

        [DataMember]
        public bool RALimited {
            get {
                return rALimited;
            }

            set {
                rALimited = value;
            }
        }

        [DataMember]
        public bool DecLimited {
            get {
                return decLimited;
            }

            set {
                decLimited = value;
            }
        }

        [DataMember]
        public double ErrorCode {
            get {
                return errorCode;
            }

            set {
                errorCode = value;
            }
        }

        public IGuideStep Clone() {
            return (PhdEventGuideStep)this.MemberwiseClone();
        }
    }
}