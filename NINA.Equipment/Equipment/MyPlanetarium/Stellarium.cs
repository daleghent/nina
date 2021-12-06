#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.Astrometry;
using NINA.Profile.Interfaces;
using System;
using System.Threading.Tasks;
using NINA.Core.Utility.Http;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NINA.Equipment.Exceptions;
using NINA.Equipment.Interfaces;
using System.Net;
using NINA.Core.Utility.Notification;

namespace NINA.Equipment.Equipment.MyPlanetarium {

    public class Stellarium : IPlanetarium {
        private string baseUrl;

        public Stellarium(IProfileService profileService) {
            var baseAddress = profileService.ActiveProfile.PlanetariumSettings.StellariumHost;
            var port = profileService.ActiveProfile.PlanetariumSettings.StellariumPort;
            this.baseUrl = $"http://{baseAddress}:{port}";
        }

        public string Name => "Stellarium";

        public bool CanGetRotationAngle => true;

        public async Task<Location> GetSite() {
            string route = "/api/main/status";

            try {
                var request = new HttpGetRequest(this.baseUrl + route);

                var response = await request.Request(new CancellationToken());
                if (string.IsNullOrEmpty(response)) throw new PlanetariumFailedToConnect();

                var jobj = JObject.Parse(response);
                var status = jobj.ToObject<StellariumStatus>();

                Location loc = new Location {
                    Latitude = status.Location.Latitude,
                    Longitude = status.Location.Longitude,
                    Elevation = status.Location.Altitude
                };

                return loc;
            } catch (Exception ex) {
                Logger.Error($"Stellarium: Failed to import site info: {ex}");
                throw;
            }
        }

        private async Task<DeepSkyObject> GetView() {
            string route = "/api/main/view";

            var request = new HttpGetRequest(this.baseUrl + route);
            try {
                var response = await request.Request(new CancellationToken());
                if (string.IsNullOrEmpty(response)) throw new PlanetariumFailedToConnect();

                /* The api returns arrays in an invalid json array format so we need to remove the quotes first */
                response = response.Replace("\"[", "[").Replace("]\"", "]");

                var jobj = JObject.Parse(response);
                var status = jobj.ToObject<StellariumView>();

                var x = Angle.ByRadians(status.J2000[0]);
                var y = Angle.ByRadians(status.J2000[1]);
                var z = Angle.ByRadians(status.J2000[2]);

                var dec = z.Asin();
                var ra = Angle.Atan2(y, x);

                // A bug in Stellarium >= 0.20 will cause it to report negative y values which translates to a negative RA value. This is not desired.
                if (ra.Radians < 0d) {
                    ra = (2 * Math.PI) + ra;
                }

                var coordinates = new Coordinates(ra, dec, Epoch.J2000);
                var dso = new DeepSkyObject(string.Empty, coordinates, string.Empty, null);

                try {
                    var selectedObject = await GetSelectedObjectInfo();
                    dso.Name = selectedObject.Name;
                } catch (PlanetariumObjectNotSelectedException) {
                    Logger.Info($"An ocular is active but no object is selected. An object name will not be populated.");
                }

                return dso;
            } catch (Exception ex) {
                Logger.Error($"Stellarium: Failed to import view info: {ex}");
                throw;
            }
        }

        public async Task<DeepSkyObject> GetTarget() {
            try {
                var isOcularsCcdEnabled = await IsOcularEnabled();

                // Get the view coordinates there is a CCD ocular active or if there is no object selected
                return isOcularsCcdEnabled ? await GetView() : await GetSelectedObjectInfo();
            } catch (Exception ex) {
                Logger.Error($"Stellarium: Failed to import object info: {ex}");
                throw;
            }
        }

        public async Task<double> GetRotationAngle() {
            string route = "/api/stelproperty/list?format=json";

            try {
                var request = new HttpGetRequest(this.baseUrl + route);
                var response = await request.Request(new CancellationToken());

                double angle = double.NaN;

                if (string.IsNullOrEmpty(response)) return double.NaN;

                var jobj = JObject.Parse(response);

                bool isOcularsCcdEnabled = ParseEnableCCD(jobj);

                if (isOcularsCcdEnabled) {
                    angle = ParseRotationAngle(jobj);

                    if (angle < 0d) {
                        angle += 360d;
                    }
                }

                return angle;
            } catch (Exception ex) {
                Logger.Error($"Stellarium: Failed to import rotation angle: {ex}");
                return double.NaN;
            }
        }

        private async Task<DeepSkyObject> GetSelectedObjectInfo() {
            string route = "/api/objects/info?format=json";

            try {
                var request = new HttpGetRequest(this.baseUrl + route);
                var response = await request.Request(new CancellationToken());

                if (string.IsNullOrEmpty(response)) throw new PlanetariumObjectNotSelectedException();

                var jobj = JObject.Parse(response);
                var status = jobj.ToObject<StellariumObject>();

                // Some objects have no "name" field, but do have a "localized-name" field.
                // Yet other objects have neither, but have a "designations" field that can hold multiple, hyphen-separated catalog designations.
                // Prefer in the following order: localized-name, name, the first name in the designations field, and lastly the type.
                var name = string.Empty;

                if (!string.IsNullOrEmpty(status?.LocalizedName)) {
                    name = status.LocalizedName;
                } else if (!string.IsNullOrEmpty(status?.Name)) {
                    name = status.Name;
                } else if (!string.IsNullOrEmpty(status?.Designations)) {
                    name = status.Designations.Split(new[] { " - " }, StringSplitOptions.None)[0];
                } else if (!string.IsNullOrEmpty(status?.Type)) {
                    name = status.Type;
                }

                var ra = AstroUtil.EuclidianModulus(status.RightAscension, 360d);
                var dec = status.Declination;

                var coordinates = new Coordinates(Angle.ByDegree(ra), Angle.ByDegree(dec), Epoch.J2000);
                var dso = new DeepSkyObject(name, coordinates, string.Empty, null);
                return dso;
            } catch (Exception ex) {
                Logger.Error($"Stellarium: Failed to import selected object info: {ex}");
                throw;
            }
        }

        private async Task<bool> IsOcularEnabled() {
            string route = "/api/stelproperty/list?format=json";

            var request = new HttpGetRequest(this.baseUrl + route);
            try {
                var response = await request.Request(new CancellationToken());
                if (string.IsNullOrEmpty(response)) return false;

                var jobj = JObject.Parse(response);
                return ParseEnableCCD(jobj);
            } catch {
                return false;
            }
        }

        public bool ParseEnableCCD(JObject jObject) {
            if (bool.TryParse((string)jObject["Oculars.enableCCD"]["value"], out bool enableCCD)) {
                return enableCCD;
            } else {
                return false;
            }
        }

        public double ParseRotationAngle(JObject jObject) {
            if (double.TryParse((string)jObject["Oculars.selectedCCDRotationAngle"]["value"], out double angle)) {
                return angle;
            } else {
                return 0d;
            }
        }

        public class StellariumView {

            [JsonProperty(PropertyName = "altAz")]
            public double[] AltAz;

            [JsonProperty(PropertyName = "j2000")]
            public double[] J2000;

            [JsonProperty(PropertyName = "jNow")]
            public double[] JNOW;

            public StellariumView(double[] altAz, double[] j2000, double[] jnow) {
                AltAz = altAz;
                J2000 = j2000;
                JNOW = jnow;
            }
        }

        public class StellariumObject {

            [JsonProperty(PropertyName = "raJ2000")]
            public double RightAscension;

            [JsonProperty(PropertyName = "decJ2000")]
            public double Declination;

            [JsonProperty(PropertyName = "name")]
            public string Name;

            [JsonProperty(PropertyName = "localized-name")]
            public string LocalizedName;

            [JsonProperty(PropertyName = "designations")]
            public string Designations;

            [JsonProperty(PropertyName = "type")]
            public string Type;

            public StellariumObject(double rightAscension, double declination, string name, string localizedName, string designations, string type) {
                RightAscension = rightAscension;
                Declination = declination;
                Name = name;
                LocalizedName = localizedName;
                Designations = designations;
                Type = type;
            }
        }

        public class StellariumLocation {

            [JsonProperty(PropertyName = "altitude")]
            public double Altitude;

            [JsonProperty(PropertyName = "latitude")]
            public double Latitude;

            [JsonProperty(PropertyName = "longitude")]
            public double Longitude;

            public StellariumLocation(double altitude, double latitude, double longitude) {
                Altitude = altitude;
                Latitude = latitude;
                Longitude = longitude;
            }
        }

        public class StellariumStatus {

            [JsonProperty(PropertyName = "location")]
            public StellariumLocation Location;

            public StellariumStatus(StellariumLocation location) {
                Location = location;
            }
        }
    }
}