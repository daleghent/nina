#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Model;
using NINA.Profile;
using NINA.Sequencer;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.FilterWheel;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Trigger.Guider;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel.ImageHistory;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.ViewModel.Sequencer.SimpleSequence {

    [JsonObject(MemberSerialization.OptIn)]
    public class SimpleExposure : SequentialContainer, IImmutableContainer {
        private bool enabled;
        private bool dither;
        private ISequencerFactory factory;
        private DitherAfterExposures ditherAfterExposures;

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            this.Items.Clear();
            this.Conditions.Clear();
            this.Triggers.Clear();
        }

        [ImportingConstructor]
        public SimpleExposure(ISequencerFactory factory) {
            this.factory = factory;
            var switchFilter = factory.GetItem<SwitchFilter>();
            var takeExposure = factory.GetItem<TakeExposure>();
            var loopCondition = factory.GetCondition<LoopCondition>();
            ditherAfterExposures = factory.GetTrigger<DitherAfterExposures>();

            this.Add(switchFilter);
            this.Add(takeExposure);
            this.Add(loopCondition);

            IsExpanded = false;
            Enabled = true;
        }

        [JsonProperty]
        public bool Enabled {
            get => enabled;
            set {
                enabled = value;
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public bool Dither {
            get => dither;
            set {
                dither = value;
                if (dither) {
                    this.Triggers.Add(ditherAfterExposures);
                } else {
                    this.Triggers.Remove(ditherAfterExposures);
                }

                RaisePropertyChanged();
            }
        }

        public SwitchFilter GetSwitchFilter() {
            return Items[0] as SwitchFilter;
        }

        public TakeExposure GetTakeExposure() {
            return Items[1] as TakeExposure;
        }

        public DitherAfterExposures GetDitherAfterExposures() {
            return ditherAfterExposures;
        }

        public LoopCondition GetLoopCondition() {
            return Conditions[0] as LoopCondition;
        }

        public SmartExposure TransformToSmartExposure() {
            var smart = factory.GetItem<SmartExposure>();

            var filter = smart.GetSwitchFilter();
            filter.Filter = this.GetSwitchFilter().Filter;

            var exposure = smart.GetTakeExposure();
            exposure.Binning = this.GetTakeExposure().Binning;
            exposure.ImageType = this.GetTakeExposure().ImageType;
            exposure.ExposureTime = this.GetTakeExposure().ExposureTime;
            exposure.Gain = this.GetTakeExposure().Gain;
            exposure.Offset = this.GetTakeExposure().Offset;

            var dither = smart.GetDitherAfterExposures();
            dither.AfterExposures = this.Dither ? this.GetDitherAfterExposures().AfterExposures : 0;

            var loop = smart.GetLoopCondition();
            loop.CompletedIterations = this.GetLoopCondition().CompletedIterations;
            loop.Iterations = this.GetLoopCondition().Iterations;

            return smart;
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (Enabled) {
                return base.Execute(progress, token);
            } else {
                return Task.CompletedTask;
            }
        }

        public override bool Validate() {
            var issues = new List<string>();
            var sw = GetSwitchFilter();
            var te = GetTakeExposure();

            bool valid = false;

            valid = te.Validate() && valid;
            issues.AddRange(te.Issues);

            if (sw.Filter != null) {
                valid = sw.Validate() && valid;
                issues.AddRange(sw.Issues);
            }

            if (Dither) {
                var ditherAfterExposures = GetDitherAfterExposures();
                valid = ditherAfterExposures.Validate() && valid;
                issues.AddRange(ditherAfterExposures.Issues);
            }

            Issues = issues;
            RaisePropertyChanged(nameof(Issues));

            return valid;
        }

        public override object Clone() {
            var clone = new SimpleExposure(factory) {
                Icon = Icon,
                Name = Name,
                Category = Category,
                Description = Description,
                Items = new ObservableCollection<ISequenceItem>(Items.Select(i => i.Clone() as ISequenceItem)),
                Triggers = new ObservableCollection<ISequenceTrigger>(),
                Conditions = new ObservableCollection<ISequenceCondition>(Conditions.Select(t => t.Clone() as ISequenceCondition)),
            };

            foreach (var item in clone.Items) {
                item.AttachNewParent(clone);
            }

            foreach (var condition in clone.Conditions) {
                condition.AttachNewParent(clone);
            }

            foreach (var trigger in clone.Triggers) {
                trigger.AttachNewParent(clone);
            }
            return clone;
        }
    }
}