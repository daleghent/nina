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

namespace NINA.Utility.Astrometry {

    [Serializable]
    public class Angle {

        public static Angle ByHours(double hours) {
            var degree = Astrometry.HoursToDegrees(hours);
            return new Angle(degree, Astrometry.ToRadians(degree), hours);
        }

        public static Angle ByDegree(double degree) {
            return new Angle(degree, Astrometry.ToRadians(degree), Astrometry.DegreesToHours(degree));
        }

        public static Angle ByRadians(double radians) {
            var degree = Astrometry.ToDegree(radians);
            return new Angle(Astrometry.ToDegree(radians), radians, Astrometry.DegreesToHours(degree));
        }

        public static Angle Zero { get; } = Angle.ByDegree(0);

        private Angle(double degree, double radians, double hours) {
            this.Degree = degree;
            this.Radians = radians;
            this.Hours = hours;
        }

        public double Degree { get; }
        public double ArcMinutes => Degree * 60d;
        public double ArcSeconds => ArcMinutes * 60d;

        public double Radians { get; }
        public double Hours { get; }

        public override string ToString() {
            return Astrometry.DegreesToDMS(Degree);
        }

        public Angle Sin() {
            return Angle.ByRadians(Math.Sin(this.Radians));
        }

        public Angle Asin() {
            return Angle.ByRadians(Math.Asin(this.Radians));
        }

        public Angle Cos() {
            return Angle.ByRadians(Math.Cos(this.Radians));
        }

        public Angle Acos() {
            return Angle.ByRadians(Math.Acos(this.Radians));
        }

        public Angle Atan() {
            return Angle.ByRadians(Math.Atan(this.Radians));
        }

        public Angle Abs() {
            return Angle.ByRadians(Math.Abs(this.Radians));
        }

        public Angle Atan2(Angle angle) {
            return Angle.ByRadians(Math.Atan2(angle.Radians, this.Radians));
        }

        public static Angle Atan2(Angle y, Angle x) {
            return Angle.ByRadians(Math.Atan2(y.Radians, x.Radians));
        }

        private static double EQUALS_EPSILON = 1E-13;

        public bool Equals(Angle that, Angle tolerance) {
            var thisDegrees = Astrometry.EuclidianModulus(this.Degree, 360.0);
            var thatDegrees = Astrometry.EuclidianModulus(that.Degree, 360.0);
            var diffDegrees = Math.Abs(thisDegrees - thatDegrees);
            var toleranceDegrees = Astrometry.EuclidianModulus(tolerance.Degree, 360.0);
            return (diffDegrees - toleranceDegrees) <= EQUALS_EPSILON
                || ((360.0 - diffDegrees) - toleranceDegrees) <= EQUALS_EPSILON;
        }

        public override bool Equals(object obj) {
            return obj is Angle angle &&
                   Degree == angle.Degree &&
                   ArcMinutes == angle.ArcMinutes &&
                   ArcSeconds == angle.ArcSeconds &&
                   Radians == angle.Radians &&
                   Hours == angle.Hours;
        }

        public override int GetHashCode() {
            int hashCode = 2008533830;
            hashCode = hashCode * -1521134295 + Degree.GetHashCode();
            hashCode = hashCode * -1521134295 + ArcMinutes.GetHashCode();
            hashCode = hashCode * -1521134295 + ArcSeconds.GetHashCode();
            hashCode = hashCode * -1521134295 + Radians.GetHashCode();
            hashCode = hashCode * -1521134295 + Hours.GetHashCode();
            return hashCode;
        }

        public Angle Copy() {
            return Angle.ByRadians(this.Radians);
        }

        public static Angle operator +(Angle a, Angle b) {
            return Angle.ByRadians(a.Radians + b.Radians);
        }

        public static Angle operator +(double a, Angle b) {
            return Angle.ByRadians(a + b.Radians);
        }

        public static Angle operator +(Angle a, double b) {
            return Angle.ByRadians(a.Radians + b);
        }

        public static Angle operator -(Angle a, Angle b) {
            return Angle.ByRadians(a.Radians - b.Radians);
        }

        public static Angle operator -(double a, Angle b) {
            return Angle.ByRadians(a - b.Radians);
        }

        public static Angle operator -(Angle a, double b) {
            return Angle.ByRadians(a.Radians - b);
        }

        public static Angle operator *(Angle a, Angle b) {
            return Angle.ByRadians(a.Radians * b.Radians);
        }

        public static Angle operator *(double a, Angle b) {
            return Angle.ByRadians(a * b.Radians);
        }

        public static Angle operator /(Angle a, Angle b) {
            return Angle.ByRadians(a.Radians / b.Radians);
        }

        public static Angle operator /(Angle a, double b) {
            return Angle.ByRadians(a.Radians / b);
        }

        public static Angle operator /(double a, Angle b) {
            return Angle.ByRadians(a / b.Radians);
        }
    }
}