#region "copyright"

/*
    Copyright � 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using System;

namespace NINA.Core.Model {

    public class RMS : BaseINPC {
        private int datapoints;
        private double sum_RA;
        private double sum_RA2;
        private double sum_Dec;
        private double sum_Dec2;
        private double ra;
        private double dec;
        private double total;
        private double peakRA = 0;
        private double peakDec = 0;

        public double RA {
            get => ra;

            set {
                ra = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(RAText));
            }
        }

        public double Dec {
            get => dec;

            set {
                dec = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(DecText));
            }
        }

        public double Total {
            get => total;

            set {
                total = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(TotalText));
            }
        }

        public string RAText => string.Format(Locale.Loc.Instance["LblPHD2RARMS"], RA.ToString("0.00"), (RA * Scale).ToString("0.00"));

        public string DecText => string.Format(Locale.Loc.Instance["LblPHD2DecRMS"], Dec.ToString("0.00"), (Dec * Scale).ToString("0.00"));

        public string TotalText => string.Format(Locale.Loc.Instance["LblPHD2TotalRMS"], Total.ToString("0.00"), (Total * Scale).ToString("0.00"));
        public string PeakRAText => string.Format(Locale.Loc.Instance["LblPHD2PeakRA"], PeakRA.ToString("0.00"), (PeakRA * Scale).ToString("0.00"));
        public string PeakDecText => string.Format(Locale.Loc.Instance["LblPHD2PeakDec"], PeakDec.ToString("0.00"), (PeakDec * Scale).ToString("0.00"));

        public double Scale { get; private set; } = 1;

        public double PeakRA { 
            get => peakRA; 
            set {
                peakRA = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(PeakRAText));
            }
        }

        public double PeakDec { 
            get => peakDec; 
            set {
                peakDec = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(PeakDecText));
            }
        }

        public int DataPoints { get => datapoints; } 

        public void AddDataPoint(double raDistance, double decDistance) {
            datapoints++;
            sum_RA += raDistance;
            sum_RA2 += (raDistance * raDistance);
            sum_Dec += decDistance;
            sum_Dec2 += (decDistance * decDistance);
            if (Math.Abs(raDistance) > PeakRA) {
                PeakRA = Math.Abs(raDistance);
            }
            if (Math.Abs(decDistance) > PeakDec) {
                PeakDec = Math.Abs(decDistance);
            }

            CalculateRMS();
        }

        public void RemoveDataPoint(double raDistance, double decDistance) {
            datapoints--;
            sum_RA -= raDistance;
            sum_RA2 -= (raDistance * raDistance);
            sum_Dec -= decDistance;
            sum_Dec2 -= (decDistance * decDistance);

            CalculateRMS();
        }

        private void CalculateRMS() {
            var ra = Math.Sqrt(datapoints * sum_RA2 - sum_RA * sum_RA) / datapoints;
            var dec = Math.Sqrt(datapoints * sum_Dec2 - sum_Dec * sum_Dec) / datapoints;
            RA = ra;
            Dec = dec;
            Total = Math.Sqrt((Math.Pow(dec, 2) + Math.Pow(ra, 2)));
        }

        public void Clear() {
            datapoints = 0;
            sum_RA = 0.0d;
            sum_RA2 = 0.0d;
            sum_Dec = 0.0d;
            sum_Dec2 = 0.0d;
            RA = 0;
            Dec = 0;
            Total = 0;
            PeakRA = 0;
            PeakDec = 0;
            RaiseAllPropertiesChanged();
        }

        public void SetScale(double scale) {
            this.Scale = scale;
            RaiseAllPropertiesChanged();
        }
    }
}