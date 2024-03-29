#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry;
using NINA.Core.Locale;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace NINA.PlateSolving.Solvers {

    internal class LocalPlateSolver : CLISolver {
        private string bashLocation;

        public LocalPlateSolver(string cygwinRoot)
            : base("cmd.exe") {
            this.bashLocation = Path.GetFullPath(Path.Combine(cygwinRoot, "bin", "bash.exe"));
        }

        protected override string GetArguments(
            string imageFilePath,
            string outputFilePath,
            PlateSolveParameter parameter,
            PlateSolveImageProperties imageProperties) {
            List<string> options = new List<string>();

            options.Add("--overwrite");
            options.Add("--index-xyls none");
            options.Add("--corr none");
            options.Add("--rdls none");
            options.Add("--match none");
            options.Add("--new-fits none");
            //options.Add("-C cancel--crpix");
            options.Add("-center");
            options.Add($"--objs {parameter.MaxObjects}");
            options.Add("--no-plots");
            options.Add("--resort");
            options.Add($"--downsample {parameter.DownSampleFactor}");
            var lowArcSecPerPix = imageProperties.ArcSecPerPixel - 0.2;
            var highArcSecPerPix = imageProperties.ArcSecPerPixel + 0.2;
            options.Add("--scale-units arcsecperpix");
            options.Add(string.Format("-L {0}", lowArcSecPerPix.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)));
            options.Add(string.Format("-H {0}", highArcSecPerPix.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)));

            if (parameter.SearchRadius > 0 && parameter.Coordinates != null) {
                options.Add($"--ra {parameter.Coordinates.RADegrees.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}");
                options.Add($"--dec {parameter.Coordinates.Dec.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}");
                options.Add($"--radius {parameter.SearchRadius.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}");
            }

            return string.Format("/C \"\"{0}\" --login -c '/usr/bin/solve-field {1} \"{2}\"'\"", bashLocation, string.Join(" ", options), imageFilePath.Replace("\\", "/"));
        }

        protected override PlateSolveResult ReadResult(string outputFilePath, PlateSolveParameter parameter, PlateSolveImageProperties imageProperties) {
            var result = new PlateSolveResult() { Success = false };
            if (File.Exists(outputFilePath)) {
                var startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
                startInfo.FileName = "cmd.exe";
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
                startInfo.Arguments = string.Format("/C \"\"{0}\" --login -c 'wcsinfo \"{1}\"'\"", bashLocation, outputFilePath.Replace("\\", "/"));
                using (var process = new System.Diagnostics.Process()) {
                    process.StartInfo = startInfo;
                    process.Start();
                    Dictionary<string, string> wcsinfo = new Dictionary<string, string>();
                    while (!process.StandardOutput.EndOfStream) {
                        var line = process.StandardOutput.ReadLine();
                        if (line != null) {
                            var valuepair = line.Split(' ');
                            if (valuepair != null && valuepair.Length == 2) {
                                wcsinfo[valuepair[0]] = valuepair[1];
                            }
                        }
                    }

                    if (wcsinfo.ContainsKey("crval0")
                        && wcsinfo.ContainsKey("crval1")
                        && wcsinfo.ContainsKey("crpix0")
                        && wcsinfo.ContainsKey("crpix1")
                        && wcsinfo.ContainsKey("cd11")
                        && wcsinfo.ContainsKey("cd12")
                        && wcsinfo.ContainsKey("cd21")
                        && wcsinfo.ContainsKey("cd22")) {
                        var crval1 = double.Parse(wcsinfo["crval0"], CultureInfo.InvariantCulture);
                        var crval2 = double.Parse(wcsinfo["crval1"], CultureInfo.InvariantCulture);
                        var crpix1 = double.Parse(wcsinfo["crpix0"], CultureInfo.InvariantCulture);
                        var crpix2 = double.Parse(wcsinfo["crpix1"], CultureInfo.InvariantCulture);
                        var cd11 = double.Parse(wcsinfo["cd11"], CultureInfo.InvariantCulture);
                        var cd12 = double.Parse(wcsinfo["cd12"], CultureInfo.InvariantCulture);
                        var cd21 = double.Parse(wcsinfo["cd21"], CultureInfo.InvariantCulture);
                        var cd22 = double.Parse(wcsinfo["cd22"], CultureInfo.InvariantCulture);

                        var wcs = new WorldCoordinateSystem(
                            crval1,
                            crval2,
                            crpix1,
                            crpix2,
                            cd11,
                            cd12,
                            cd21,
                            cd22
                        );

                        result.Flipped = !wcs.Flipped;
                    }

                    double ra = 0, dec = 0;
                    if (wcsinfo.TryGetValue("ra_center", out string value)) {
                        ra = double.Parse(value, CultureInfo.InvariantCulture);
                    }
                    if (wcsinfo.TryGetValue("dec_center", out value)) {
                        dec = double.Parse(value, CultureInfo.InvariantCulture);
                    }
                    if (wcsinfo.TryGetValue("orientation_center", out value)) {
                        result.PositionAngle = 360 - (180 - double.Parse(value, CultureInfo.InvariantCulture) + 360);
                    }
                    if (wcsinfo.TryGetValue("pixscale", out value)) {
                        result.Pixscale = double.Parse(value, CultureInfo.InvariantCulture);
                        if (!double.IsNaN(result.Pixscale)) {
                            result.Radius = AstroUtil.ArcsecToDegree(Math.Sqrt(Math.Pow(imageProperties.ImageWidth * result.Pixscale, 2) + Math.Pow(imageProperties.ImageHeight * result.Pixscale, 2)) / 2d);
                        }
                    }

                    result.Coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Degrees);
                    result.Success = true;
                }
            }
            return result;
        }

        protected override string GetLocalizedPlateSolverName() {
            return Loc.Instance["LblCygwinBashNotFound"];
        }

        protected override string GetOutputPath(string imageFilePath) {
            return Path.Combine(Path.GetDirectoryName(imageFilePath), Path.GetFileNameWithoutExtension(imageFilePath)) + ".wcs";
        }
    }
}