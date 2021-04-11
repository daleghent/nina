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
using NINA.Sequencer.SequenceItem;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Sequencer.Conditions {

    [ExportMetadata("Name", "Lbl_SequenceCondition_TimeSpanCondition_Name")]
    [ExportMetadata("Description", "Lbl_SequenceCondition_TimeSpanCondition_Description")]
    [ExportMetadata("Icon", "HourglassSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Condition")]
    [Export(typeof(ISequenceCondition))]
    [JsonObject(MemberSerialization.OptIn)]
    public class TimeSpanCondition : SequenceCondition {
        private int hours;

        private int minutes;

        private int seconds;

        [ImportingConstructor]
        public TimeSpanCondition() {
            Minutes = 1;
            Ticker = new Ticker(TimeSpan.FromSeconds(1));
            Ticker.PropertyChanged += Ticker_PropertyChanged;
        }

        private void Ticker_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            RaisePropertyChanged(nameof(RemainingTime));
        }

        public Ticker Ticker { get; }

        [JsonProperty]
        public int Hours {
            get => hours;
            set {
                hours = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(RemainingTime));
            }
        }

        [JsonProperty]
        public int Minutes {
            get => minutes;
            set {
                minutes = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(RemainingTime));
            }
        }

        [JsonProperty]
        public int Seconds {
            get => seconds;
            set {
                seconds = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(RemainingTime));
            }
        }

        public TimeSpan RemainingTime {
            get {
                var duration = TimeSpan.FromHours(Hours) + TimeSpan.FromMinutes(Minutes) + TimeSpan.FromSeconds(Seconds);
                if (startTime.HasValue) {
                    var elapsed = DateTime.Now - startTime.Value;
                    if (elapsed > duration) {
                        return TimeSpan.Zero;
                    } else {
                        return duration - elapsed;
                    }
                } else {
                    return duration;
                }
            }
        }

        private DateTime? startTime;

        public override bool Check(ISequenceItem nextItem) {
            var nextItemDuration = nextItem?.GetEstimatedDuration() ?? TimeSpan.Zero;
            return (RemainingTime - nextItemDuration) > TimeSpan.Zero;
        }

        public override object Clone() {
            return new TimeSpanCondition() {
                Icon = Icon,
                Hours = Hours,
                Minutes = Minutes,
                Seconds = Seconds,
                Name = Name,
                Category = Category,
                Description = Description,
            };
        }

        public override void ResetProgress() {
            startTime = null;
        }

        public override void SequenceBlockFinished() {
        }

        public override void SequenceBlockStarted() {
            if (!startTime.HasValue) {
                startTime = DateTime.Now;
            }
        }

        public override string ToString() {
            return $"Condition: {nameof(TimeSpanCondition)}, Time: {Hours}:{Minutes}:{Seconds}h";
        }
    }
}