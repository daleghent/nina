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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace NINA.Utility.Astrometry {

    public class CustomHorizon {
        private double[] azimuths;
        private double[] altitudes;

        /// <param name="horizonMap">A map containing azimuth->altitude mappings</param>
        private CustomHorizon(IDictionary<double, double> horizonMap) {
            this.azimuths = horizonMap.Keys.ToArray();
            this.altitudes = horizonMap.Values.ToArray();
        }

        public double GetAltitude(double azimuth) {
            if (azimuth < 0 || azimuth > 359) { azimuth = Astrometry.EuclidianModulus(azimuth, 360); }
            return Accord.Math.Tools.Interpolate1D(azimuth, azimuths, altitudes, 0, 0);
        }

        public double GetMaxAltitude() {
            return this.altitudes.Max();
        }

        public double GetMinAltitude() {
            return this.altitudes.Min();
        }

        public static CustomHorizon FromReader(TextReader sr) {
            var horizonMap = new SortedDictionary<double, double>();

            string line;
            while ((line = sr.ReadLine()?.Trim()) != null) {
                // Lines starting with # are comments
                if (!line.StartsWith("#")) {
                    var columns = line.Split(' ');
                    if (columns.Length == 2) {
                        if (double.TryParse(columns[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var azimuth)) {
                            if (double.TryParse(columns[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var altitude)) {
                                horizonMap[azimuth] = altitude;
                            } else {
                                Logger.Warning($"Invalid value for altitude {columns[0]}");
                            }
                        } else {
                            Logger.Warning($"Invalid value for azimuth {columns[0]}");
                        }
                    } else {
                        Logger.Warning($"Invalid line for horizon values {line}");
                    }
                }
            }

            if (horizonMap.Count < 2) {
                throw new ArgumentException("Horizon file does not contain enough entries or is invalid");
            }

            // Groom Incomplete Data
            // 1) No 0 or 360 Azimuth is detected => Find the nearest datapoint and add it to the list
            // 2) No 0 Azimuth is present but 360 Azimuth is present => Add value from 360 to 0
            // 2) No 360 Azimuth is present but 0 Azimuth is present => Add value from 0 to 360
            if (!horizonMap.ContainsKey(0) && !horizonMap.ContainsKey(360)) {
                var nearest0Azimuth = horizonMap.Keys.OrderBy(x => Math.Abs(x)).First();
                var nearest360Azimuth = horizonMap.Keys.OrderByDescending(x => Math.Abs(x)).First();

                var key = nearest0Azimuth;
                if (360 - nearest360Azimuth < nearest0Azimuth) {
                    key = nearest360Azimuth;
                }

                horizonMap[0] = horizonMap[key];
                horizonMap[360] = horizonMap[key];
            } else if (!horizonMap.ContainsKey(0) && horizonMap.ContainsKey(360)) {
                horizonMap[0] = horizonMap[360];
            } else if (horizonMap.ContainsKey(0) && !horizonMap.ContainsKey(360)) {
                horizonMap[360] = horizonMap[0];
            }

            var horizon = new CustomHorizon(horizonMap);
            return horizon;
        }

        /// <summary>
        /// Creates an instance of the custom horizon object to calculate horizon altitude based on a given azimuth out of a file that specifies the horizon
        /// The Horizon file must consist of a list of azimuth and alitutde pairs that are separated by a space and line breaks
        /// A minimum of two points are required to approximate the horizon
        /// Lines starting with '#' character will be treated as comments and therefore ignored
        /// </summary>
        /// <example>
        /// # File Example
        /// 0 10
        /// 90 20
        /// 180 30
        /// 270 20
        /// 360 10
        /// </example>
        /// <param name="filePath">The file pointing to the horizon file</param>
        /// <returns>An instance of CustomHorizon</returns>
        public static CustomHorizon FromFile(string filePath) {
            if (File.Exists(filePath)) {
                using (var fs = File.OpenRead(filePath)) {
                    using (var sr = new StreamReader(fs)) {
                        return FromReader(sr);
                    }
                }
            } else {
                throw new FileNotFoundException("Horizon file not found", filePath);
            }
        }
    }
}