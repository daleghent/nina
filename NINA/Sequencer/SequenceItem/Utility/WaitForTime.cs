#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Model;
using NINA.Sequencer.Utility.DateTimeProvider;
using NINA.Utility.Astrometry;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xceed.Wpf.Toolkit;

namespace NINA.Sequencer.SequenceItem.Utility {

    [ExportMetadata("Name", "Lbl_SequenceItem_Utility_WaitForTime_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Utility_WaitForTime_Description")]
    [ExportMetadata("Icon", "ClockSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Utility")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class WaitForTime : SequenceItem {

        [ImportingConstructor]
        public WaitForTime(IList<IDateTimeProvider> dateTimeProviders) {
            this.DateTimeProviders = dateTimeProviders;
            this.SelectedProvider = DateTimeProviders?.FirstOrDefault();
        }

        public WaitForTime(IList<IDateTimeProvider> dateTimeProviders, IDateTimeProvider selectedProvider) {
            this.DateTimeProviders = dateTimeProviders;
            this.SelectedProvider = selectedProvider;
        }

        private int hours;

        [JsonProperty]
        public int Hours {
            get => hours;
            set {
                hours = value;
                RaisePropertyChanged();
            }
        }

        private int minutes;

        [JsonProperty]
        public int Minutes {
            get => minutes;
            set {
                minutes = value;
                RaisePropertyChanged();
            }
        }

        private int seconds;

        [JsonProperty]
        public int Seconds {
            get => seconds;
            set {
                seconds = value;
                RaisePropertyChanged();
            }
        }

        public override object Clone() {
            return new WaitForTime(DateTimeProviders, SelectedProvider) {
                Icon = Icon,
                Hours = Hours,
                Minutes = Minutes,
                Seconds = Seconds,
                Name = Name,
                Category = Category,
                Description = Description,
            };
        }

        private IDateTimeProvider selectedProvider;

        [JsonProperty]
        public IDateTimeProvider SelectedProvider {
            get => selectedProvider;
            set {
                selectedProvider = value;
                if (selectedProvider != null) {
                    var t = selectedProvider.GetDateTime();
                    Hours = t.Hour;
                    Minutes = t.Minute;
                    Seconds = t.Second;
                    RaisePropertyChanged();
                }
            }
        }

        private IList<IDateTimeProvider> dateTimeProviders;

        public IList<IDateTimeProvider> DateTimeProviders {
            get => dateTimeProviders;
            set {
                dateTimeProviders = value;
                RaisePropertyChanged();
            }
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            return NINA.Utility.Utility.Wait(GetEstimatedDuration(), token, progress);
        }

        public override TimeSpan GetEstimatedDuration() {
            var now = DateTime.Now;
            var then = new DateTime(now.Year, now.Month, now.Day, Hours, Minutes, Seconds);

            if (now.Hour <= 12 && then.Hour > 12) {
                then = then.AddDays(-1);
            }

            //In case it is 22:00:00 but you want to wait until 01:00:00 o'clock a day of 1 needs to be added
            if (now.Hour > 12 && then.Hour < 12) {
                then = then.AddDays(1);
            }

            return then - DateTime.Now;
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(WaitForTime)}, Time: {Hours}:{Minutes}:{Seconds}h";
        }
    }
}