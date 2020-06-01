#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Accord;
using Newtonsoft.Json;
using NINA.Utility.Enum;
using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.ViewModel.AutoFocus {
    public class AutoFocusReport {
        [JsonProperty]
        public DateTime Timestamp { get; set; }

        [JsonProperty]
        public double Temperature { get; set; }

        [JsonProperty]
        public string Method { get; set; }

        [JsonProperty]
        public string Fitting { get; set; }

        [JsonProperty]
        public FocusPoint InitialFocusPoint { get; set; } = new FocusPoint();

        [JsonProperty]
        public FocusPoint CalculatedFocusPoint { get; set; } = new FocusPoint();

        [JsonProperty]
        public FocusPoint PreviousFocusPoint { get; set; } = new FocusPoint();

        [JsonProperty]
        public IEnumerable<FocusPoint> MeasurePoints { get; set; }

        [JsonProperty]
        public Intersections Intersections { get; set; }

        /// <summary>
        /// Generates a JSON report into %localappdata%\NINA\AutoFocus for the complete autofocus run containing all the measurements
        /// </summary>
        /// <param name="initialFocusPosition"></param>
        /// <param name="initialHFR"></param>
        public static AutoFocusReport GenerateReport(
            ICollection<ScatterErrorPoint> FocusPoints,
            double initialFocusPosition,
            double initialHFR,
            DataPoint focusPoint,
            AutoFocusPoint lastFocusPoint,
            AFMethodEnum method,
            AFCurveFittingEnum fitting,
            TrendlineFitting trendlineFitting,
            QuadraticFitting quadraticFitting,
            HyperbolicFitting hyperbolicFitting,
            GaussianFitting gaussianFitting,
            double temperature) {
            var report = new AutoFocusReport() {
                Timestamp = DateTime.Now,
                Temperature = temperature,
                InitialFocusPoint = new FocusPoint() {
                    Position = initialFocusPosition,
                    Value = initialHFR
                },
                CalculatedFocusPoint = new FocusPoint() {
                    Position = focusPoint.X,
                    Value = focusPoint.Y
                },
                PreviousFocusPoint = new FocusPoint() {
                    Position = lastFocusPoint?.Focuspoint.X ?? double.NaN,
                    Value = lastFocusPoint?.Focuspoint.Y ?? double.NaN
                },
                Method = method.ToString(),
                Fitting = method == AFMethodEnum.STARHFR ? fitting.ToString() : "GAUSSIAN",
                MeasurePoints = FocusPoints.Select(x => new FocusPoint() { Position = x.X, Value = x.Y, Error = x.ErrorY }),
                Intersections = new Intersections() {
                    TrendLineIntersection = new FocusPoint() { Position = trendlineFitting.Intersection.X, Value = trendlineFitting.Intersection.Y },
                    GaussianMaximum = new FocusPoint() { Position = gaussianFitting.Maximum.X, Value = gaussianFitting.Maximum.Y },
                    HyperbolicMinimum = new FocusPoint() { Position = hyperbolicFitting.Minimum.X, Value = hyperbolicFitting.Minimum.Y },
                    QuadraticMinimum = new FocusPoint() { Position = quadraticFitting.Minimum.X, Value = quadraticFitting.Minimum.Y }
                }
            };

            return report;
        }
    }

    public class Intersections {
        public FocusPoint TrendLineIntersection { get; set; }
        public FocusPoint HyperbolicMinimum { get; set; }
        public FocusPoint QuadraticMinimum { get; set; }
        public FocusPoint GaussianMaximum { get; set; }
    }

    public class FocusPoint {
        [JsonProperty]
        public double Position { get; set; }

        [JsonProperty]
        public double Value { get; set; }

        [JsonProperty]
        public double Error { get; set; }
    }
}