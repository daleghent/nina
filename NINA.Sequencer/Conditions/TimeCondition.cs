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
using NINA.Sequencer.Utility.DateTimeProvider;
using NINA.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace NINA.Sequencer.Conditions {

    [ExportMetadata("Name", "Lbl_SequenceCondition_TimeCondition_Name")]
    [ExportMetadata("Description", "Lbl_SequenceCondition_TimeCondition_Description")]
    [ExportMetadata("Icon", "ClockSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Condition")]
    [Export(typeof(ISequenceCondition))]
    [JsonObject(MemberSerialization.OptIn)]
    public class TimeCondition : SequenceCondition {
        private IList<IDateTimeProvider> dateTimeProviders;

        private int hours;

        private int minutes;

        private int seconds;

        private IDateTimeProvider selectedProvider;

        [ImportingConstructor]
        public TimeCondition(IList<IDateTimeProvider> dateTimeProviders) {
            this.DateTimeProviders = dateTimeProviders;
            this.SelectedProvider = DateTimeProviders?.FirstOrDefault();
            Ticker = new Ticker(TimeSpan.FromSeconds(1));
            Ticker.PropertyChanged += Ticker_PropertyChanged;
        }

        public TimeCondition(IList<IDateTimeProvider> dateTimeProviders, IDateTimeProvider selectedProvider) {
            this.DateTimeProviders = dateTimeProviders;
            this.SelectedProvider = selectedProvider;
            Ticker = new Ticker(TimeSpan.FromSeconds(1));
            Ticker.PropertyChanged += Ticker_PropertyChanged;
        }

        private void Ticker_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            RaisePropertyChanged(nameof(RemainingTime));
        }

        public Ticker Ticker { get; }

        public IList<IDateTimeProvider> DateTimeProviders {
            get => dateTimeProviders;
            set {
                dateTimeProviders = value;
                RaisePropertyChanged();
            }
        }

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

        [JsonProperty]
        public IDateTimeProvider SelectedProvider {
            get => selectedProvider;
            set {
                selectedProvider = value;
                if (selectedProvider != null) {
                    UpdateTime();
                    RaisePropertyChanged();
                }
            }
        }

        public TimeSpan RemainingTime {
            get {
                TimeSpan remaining = (CalculateRemainingTime() - DateTime.Now);
                if (remaining.TotalSeconds < 0) return new TimeSpan(0);
                return remaining;
            }
        }

        public override bool Check(ISequenceItem nextItem) {
            return DateTime.Now + (nextItem?.GetEstimatedDuration() ?? TimeSpan.Zero) <= CalculateRemainingTime();
        }

        private DateTime CalculateRemainingTime() {
            var now = DateTime.Now;
            var then = new DateTime(now.Year, now.Month, now.Day, Hours, Minutes, Seconds);

            //In case it is 22:00:00 but you want to wait until 01:00:00 o'clock a day of 1 needs to be added
            if (now.Hour > 12 && then.Hour < 12) {
                then = then.AddDays(1);
            }

            return then;
        }

        public override object Clone() {
            return new TimeCondition(DateTimeProviders, SelectedProvider) {
                Icon = Icon,
                Hours = Hours,
                Minutes = Minutes,
                Seconds = Seconds,
                Name = Name,
                Category = Category,
                Description = Description,
            };
        }

        private void UpdateTime() {
            var t = selectedProvider.GetDateTime(this);
            Hours = t.Hour;
            Minutes = t.Minute;
            Seconds = t.Second;
        }

        public override void AfterParentChanged() {
            if (!(selectedProvider is TimeProvider)) {
                UpdateTime();
            }
        }

        public override void ResetProgress() {
        }

        public override void SequenceBlockFinished() {
        }

        public override void SequenceBlockStarted() {
        }

        public override string ToString() {
            return $"Condition: {nameof(TimeCondition)}, Time: {Hours}:{Minutes}:{Seconds}h";
        }
    }
}