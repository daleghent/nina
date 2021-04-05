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
using NINA.Profile;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem.FilterWheel;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Trigger.Guider;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel.ImageHistory;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Sequencer.SequenceItem.Imaging {

    [ExportMetadata("Name", "Lbl_SequenceItem_Imaging_SmartExposure_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Imaging_SmartExposure_Description")]
    [ExportMetadata("Icon", "CameraSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Camera")]
    [Export(typeof(ISequenceItem))]
    [Export(typeof(ISequenceContainer))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SmartExposure : SequentialContainer, IImmutableContainer {

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            this.Items.Clear();
            this.Conditions.Clear();
            this.Triggers.Clear();
        }

        [ImportingConstructor]
        public SmartExposure(
                IProfileService profileService,
                ICameraMediator cameraMediator,
                IImagingMediator imagingMediator,
                IImageSaveMediator imageSaveMediator,
                IImageHistoryVM imageHistoryVM,
                IFilterWheelMediator filterWheelMediator,
                IGuiderMediator guiderMediator) : this(
                    new SwitchFilter(profileService, filterWheelMediator),
                    new TakeExposure(profileService, cameraMediator, imagingMediator, imageSaveMediator, imageHistoryVM),
                    new LoopCondition(),
                    new DitherAfterExposures(guiderMediator, imageHistoryVM)
                ) {
        }

        /// <summary>
        /// Clone Constructor
        /// </summary>
        public SmartExposure(
                SwitchFilter switchFilter,
                TakeExposure takeExposure,
                LoopCondition loopCondition,
                DitherAfterExposures ditherAfterExposures
                ) {
            this.Add(switchFilter);
            this.Add(takeExposure);
            this.Add(loopCondition);
            this.Add(ditherAfterExposures);

            IsExpanded = false;
        }

        public SwitchFilter GetSwitchFilter() {
            return Items[0] as SwitchFilter;
        }

        public TakeExposure GetTakeExposure() {
            return Items[1] as TakeExposure;
        }

        public DitherAfterExposures GetDitherAfterExposures() {
            return Triggers[0] as DitherAfterExposures;
        }

        public LoopCondition GetLoopCondition() {
            return Conditions[0] as LoopCondition;
        }

        public override bool Validate() {
            var issues = new List<string>();
            var sw = GetSwitchFilter();
            var te = GetTakeExposure();
            var dither = GetDitherAfterExposures();

            bool valid = false;

            valid = te.Validate() && valid;
            issues.AddRange(te.Issues);

            if (sw.Filter != null) {
                valid = sw.Validate() && valid;
                issues.AddRange(sw.Issues);
            }

            if (dither.AfterExposures > 0) {
                valid = dither.Validate() && valid;
                issues.AddRange(dither.Issues);
            }

            Issues = issues;
            RaisePropertyChanged(nameof(Issues));

            return valid;
        }

        public override object Clone() {
            var clone = new SmartExposure(
                    (SwitchFilter)this.GetSwitchFilter().Clone(),
                    (TakeExposure)this.GetTakeExposure().Clone(),
                    (LoopCondition)this.GetLoopCondition().Clone(),
                    (DitherAfterExposures)this.GetDitherAfterExposures().Clone()
                ) {
                Icon = Icon,
                Name = Name,
                Category = Category,
                Description = Description
            };
            return clone;
        }

        public override TimeSpan GetEstimatedDuration() {
            return GetTakeExposure().GetEstimatedDuration();
        }
    }
}