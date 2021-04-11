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

namespace NINA.Image.ImageAnalysis {

    public class HistogramMath {

        public enum ExposureAduState {
            ExposureBelowLowerBound,
            ExposureWithinBounds,
            ExposureAboveUpperBound
        }

        public static double GetLowerToleranceAduFromAdu(double histogramMeanAdu, double tolerance) {
            return histogramMeanAdu - histogramMeanAdu * tolerance;
        }

        public static double GetUpperToleranceAduFromAdu(double histogramMeanAdu, double tolerance) {
            return histogramMeanAdu + histogramMeanAdu * tolerance;
        }

        public static double CameraBitDepthToAdu(double cameraBitDepth) {
            return Math.Pow(2, cameraBitDepth);
        }

        public static double HistogramMeanAndCameraBitDepthToAdu(double histogramMean, double cameraBitDepth) {
            return histogramMean * CameraBitDepthToAdu(cameraBitDepth);
        }

        public static double GetLowerToleranceBoundInAdu(double histogramMean, double cameraBitDepth, double tolerance) {
            return GetLowerToleranceAduFromAdu(HistogramMeanAndCameraBitDepthToAdu(histogramMean, cameraBitDepth), tolerance);
        }

        public static double GetUpperToleranceBoundInAdu(double histogramMean, double cameraBitDepth, double tolerance) {
            return GetUpperToleranceAduFromAdu(HistogramMeanAndCameraBitDepthToAdu(histogramMean, cameraBitDepth), tolerance);
        }

        public static ExposureAduState GetExposureAduState(double mean, double targetMean, double cameraBitDepth, double tolerance) {
            var histogramMeanAdu = HistogramMeanAndCameraBitDepthToAdu(targetMean, cameraBitDepth);
            var histogramToleranceUpperBound = GetUpperToleranceAduFromAdu(histogramMeanAdu, tolerance);
            var histogramToleranceLowerBound = GetLowerToleranceAduFromAdu(histogramMeanAdu, tolerance);

            switch (mean) {
                case double m when m < histogramToleranceLowerBound:
                    return ExposureAduState.ExposureBelowLowerBound;

                case double m when m > histogramToleranceUpperBound:
                    return ExposureAduState.ExposureAboveUpperBound;

                default:
                    return ExposureAduState.ExposureWithinBounds;
            }
        }
    }
}