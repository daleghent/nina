#region "copyright"

/*
    Copyright © 2016 - 2022 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Model;
using OxyPlot;
using OxyPlot.Axes;
using System;
using System.Linq;

namespace NINA.Astrometry {

    public class DeepSkyObject : SkyObjectBase {

        public DeepSkyObject(string id, Coordinates coords, string imageRepository, CustomHorizon customHorizon)
            : base(id, imageRepository, customHorizon) {
            _coordinates = coords;
        }

        private Coordinates _coordinates;
        public override Coordinates Coordinates {
            get {
                return _coordinates;
            }
            set {
                _coordinates = value;
                if (_coordinates != null) {
                    UpdateHorizonAndTransit();
                }
                RaisePropertyChanged();
            }
        }

        public override Coordinates CoordinatesAt(DateTime at) {
            return Coordinates;
        }

        public override SiderealShiftTrackingRate ShiftTrackingRate => SiderealShiftTrackingRate.Disabled;
        public override SiderealShiftTrackingRate ShiftTrackingRateAt(DateTime at) => SiderealShiftTrackingRate.Disabled;

        public void Refresh() {
            UpdateHorizonAndTransit();
        }

        protected override void UpdateHorizonAndTransit() {
            var start = this._referenceDate;
            Altitudes.Clear();
            Horizon.Clear();
            var siderealTime = AstroUtil.GetLocalSiderealTime(start, _longitude);
            var hourAngle = AstroUtil.GetHourAngle(siderealTime, this.Coordinates.RA);

            for (double angle = hourAngle; angle < hourAngle + 24; angle += 0.1) {
                var degAngle = AstroUtil.HoursToDegrees(angle);
                var altitude = AstroUtil.GetAltitude(degAngle, _latitude, this.Coordinates.Dec);

                var azimuth = AstroUtil.GetAzimuth(degAngle, altitude, _latitude, this.Coordinates.Dec);

                Altitudes.Add(new DataPoint(DateTimeAxis.ToDouble(start), altitude));

                if (customHorizon != null) {
                    var horizonAltitude = customHorizon.GetAltitude(azimuth);
                    Horizon.Add(new DataPoint(DateTimeAxis.ToDouble(start), horizonAltitude));
                }

                start = start.AddHours(0.1);
            }

            MaxAltitude = Altitudes.OrderByDescending((x) => x.Y).FirstOrDefault();

            CalculateTransit(_latitude);
        }

        private void CalculateTransit(double latitude) {
            var alt0 = AstroUtil.GetAltitude(0, latitude, this.Coordinates.Dec);
            var alt180 = AstroUtil.GetAltitude(180, latitude, this.Coordinates.Dec);
            double transit;
            if (alt0 > alt180) {
                transit = AstroUtil.GetAzimuth(0, alt0, latitude, this.Coordinates.Dec);
            } else {
                transit = AstroUtil.GetAzimuth(180, alt180, latitude, this.Coordinates.Dec);
            }
            DoesTransitSouth = Convert.ToInt32(transit) == 180;
        }
    }
}