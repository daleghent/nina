#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model.ImageData;
using NINA.Utility;

namespace NINA.ViewModel.ImageHistory {

    public class ImageHistoryPoint : BaseINPC {

        public ImageHistoryPoint(int id, IImageStatistics statistics) {
            Id = id;
            Statistics = statistics;
        }

        public void PopulateSDPoint(IStarDetectionAnalysis starDetectionAnalysis) {
            HFR = starDetectionAnalysis.HFR;
            DetectedStars = starDetectionAnalysis.DetectedStars;
        }

        public void PopulateAFPoint(AutoFocus.AutoFocusReport report) {
            AutoFocusPoint = new AutoFocusPoint();
            AutoFocusPoint.OldPosition = report.InitialFocusPoint.Position;
            AutoFocusPoint.NewPosition = report.CalculatedFocusPoint.Position;
            AutoFocusPoint.Temperature = report.Temperature;
            AutoFocusPoint.Time = report.Timestamp;
        }

        public int Id { get; private set; }
        public double Zero { get; } = 0.05;

        public IImageStatistics Statistics { get; private set; }

        public AutoFocusPoint AutoFocusPoint { get; private set; }

        public int DetectedStars { get; private set; }

        public double HFR { get; private set; }
    }
}