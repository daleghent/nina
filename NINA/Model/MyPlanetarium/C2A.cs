#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Exceptions;
using NINA.Profile;
using System.Threading.Tasks;
using System.Text;
using System;
using System.Net.Sockets;

namespace NINA.Model.MyPlanetarium {

    internal class C2A : IPlanetarium {
        private string address;
        private int port;

        public C2A(IProfileService profileService) {
            this.address = profileService.ActiveProfile.PlanetariumSettings.C2AHost;
            this.port = profileService.ActiveProfile.PlanetariumSettings.C2APort;
        }

        public string Name {
            get {
                return "C2A";
            }
        }

        /// <summary>
        /// Get the selected object in C2A
        /// </summary>
        /// <returns></returns>
        public async Task<DeepSkyObject> GetTarget() {
            try {
                string response = await Query("GetRa;GetDe;");
                response = response.TrimEnd('\r', '\n');

                if (!string.IsNullOrEmpty(response)) {
                    string[] info = response.Split(';');

                    Coordinates newCoordinates = new Coordinates(double.Parse(info[0].Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture),
                                                         double.Parse(info[1].Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture),
                                                         Epoch.J2000, Coordinates.RAType.Hours);

                    return new DeepSkyObject(info[2], newCoordinates, string.Empty);
                } else {
                    throw new PlanetariumFailedToGetCoordinates();
                }
            } catch (Exception ex) {
                Logger.Error(ex);
                throw ex;
            }
        }

        /// <summary>
        /// Return the configured user location from C2A
        /// </summary>
        /// <returns></returns>
        public async Task<Coords> GetSite() {
            try {
                var response = await Query("GetLatitude;GetLongitude;");
                response = response.TrimEnd('\r', '\n');

                if (!string.IsNullOrEmpty(response)) {
                    var info = response.Split(';');

                    return new Coords {
                        Latitude = double.Parse(info[0].Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture),
                        Longitude = double.Parse(info[1].Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture),
                        Elevation = 0
                    };
                } else {
                    throw new PlanetariumFailedToGetCoordinates();
                }
            } catch (Exception ex) {
                Logger.Error(ex);
                throw ex;
            }
        }

        private async Task<string> Query(string command) {
            using (var client = new TcpClient()) {
                try {
                    await client.ConnectAsync(this.address, this.port);
                } catch (Exception ex) {
                    throw new PlanetariumFailedToConnect($"{address}:{port}: {ex}");
                }

                byte[] data = Encoding.ASCII.GetBytes($"{command}\r\n");
                var stream = client.GetStream();
                stream.Write(data, 0, data.Length);

                byte[] buffer = new byte[2048];
                var length = stream.Read(buffer, 0, buffer.Length);
                string response = Encoding.ASCII.GetString(buffer, 0, length);

                stream.Close();
                client.Close();

                Logger.Trace($"{Name} - Received Message {response}");

                return response;
            }
        }
    }
}