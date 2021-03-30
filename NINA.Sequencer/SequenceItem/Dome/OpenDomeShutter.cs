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
using NINA.Sequencer.Exceptions;
using NINA.Sequencer.Validations;
using NINA.Utility.Mediator.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.SequenceItem.Dome {

    [ExportMetadata("Name", "Lbl_SequenceItem_Dome_OpenDomeShutter_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Dome_OpenDomeShutter_Description")]
    [ExportMetadata("Icon", "ObservatorySVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Dome")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class OpenDomeShutter : SequenceItem, IValidatable {

        [ImportingConstructor]
        public OpenDomeShutter(IDomeMediator domeMediator) {
            this.domeMediator = domeMediator;
        }

        private IDomeMediator domeMediator;
        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (Validate()) {
                // Todo - this interface lacks progress
                return domeMediator.OpenShutter(token);
            } else {
                throw new SequenceItemSkippedException(string.Join(",", Issues));
            }
        }

        public override object Clone() {
            return new OpenDomeShutter(domeMediator) {
                Icon = Icon,
                Name = Name,
                Category = Category,
                Description = Description,
            };
        }

        public bool Validate() {
            var i = new List<string>();
            if (!domeMediator.GetInfo().Connected) {
                i.Add(Locale.Loc.Instance["LblDomeNotConnected"]);
            }
            Issues = i;
            return i.Count == 0;
        }

        public override void AfterParentChanged() {
            Validate();
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(OpenDomeShutter)}";
        }
    }
}