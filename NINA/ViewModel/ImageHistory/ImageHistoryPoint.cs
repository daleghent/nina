#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using CsvHelper.Configuration;
using NINA.Model.ImageData;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using System;
using System.IO;

namespace NINA.ViewModel.ImageHistory {

    public class ImageHistoryPoint : BaseINPC {

        public ImageHistoryPoint(int id, IImageStatistics statistics, string imageType) {
            Id = id;
            Statistics = statistics;
            if (statistics != null) {
                Median = statistics.Median;
                Mean = statistics.Mean;
                StDev = statistics.StDev;
                MAD = statistics.MedianAbsoluteDeviation;
            }
            Type = imageType;
        }

        public void PopulateAFPoint(AutoFocus.AutoFocusReport report) {
            AutoFocusPoint = new AutoFocusPoint();
            AutoFocusPoint.OldPosition = report.InitialFocusPoint.Position;
            AutoFocusPoint.NewPosition = report.CalculatedFocusPoint.Position;
            AutoFocusPoint.Temperature = report.Temperature;
            AutoFocusPoint.Time = report.Timestamp;
        }

        public void PopulateProperties(ImageSavedEventArgs imageSavedEventArgs) {
            if (imageSavedEventArgs == null)
                return;
            if (imageSavedEventArgs.StarDetectionAnalysis != null) {
                HFR = imageSavedEventArgs.StarDetectionAnalysis.HFR;
                Stars = imageSavedEventArgs.StarDetectionAnalysis.DetectedStars;
            }
            Filter = imageSavedEventArgs.Filter;
            Duration = imageSavedEventArgs.Duration;
            if (imageSavedEventArgs.PathToImage != null)
                Filename = Path.GetFileName(imageSavedEventArgs.PathToImage.LocalPath);

            if (imageSavedEventArgs.MetaData?.Target != null) {
                Target = imageSavedEventArgs.MetaData.Target;
            }
        }

        public int Id { get; private set; }
        public int Index { get; set; }
        public double Zero { get; } = 0.05;

        public IImageStatistics Statistics { get; private set; }

        public AutoFocusPoint AutoFocusPoint { get; private set; }

        public double HFR { get; private set; }

        public int Stars { get; private set; }

        public double Median { get; private set; }
        public double Mean { get; private set; }
        public double StDev { get; private set; }
        public double MAD { get; private set; }

        public string Filter { get; private set; }
        public double Duration { get; private set; }
        public string Filename { get; private set; }

        public DateTime dateTime { get; private set; } = DateTime.Now;

        public string Type { get; private set; }

        public TargetParameter Target { get; private set; }
    }

    public sealed class ImageHistoryPointMap : ClassMap<ImageHistoryPoint> {

        public ImageHistoryPointMap() {
            Map(m => m.Id);
            Map(m => m.Target.Name);
            Map(m => m.Target.Coordinates.RA);
            Map(m => m.Target.Coordinates.Dec);
            Map(m => m.Target.Rotation);
            Map(m => m.Filename);
            Map(m => m.dateTime);
            Map(m => m.Duration);
            Map(m => m.Filter);
            Map(m => m.HFR);
            Map(m => m.Stars);
            Map(m => m.Median);
            Map(m => m.Mean);
            Map(m => m.StDev);
            Map(m => m.MAD);
        }
    }
}