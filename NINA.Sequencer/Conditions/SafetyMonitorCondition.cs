#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using NINA.Utility.Mediator.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Sequencer.Conditions {

    [ExportMetadata("Name", "Lbl_SequenceCondition_SafetyMonitorCondition_Name")]
    [ExportMetadata("Description", "Lbl_SequenceCondition_SafetyMonitorCondition_Description")]
    [ExportMetadata("Icon", "ShieldSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Condition")]
    [Export(typeof(ISequenceCondition))]
    public class SafetyMonitorCondition : SequenceCondition, IValidatable {
        private ISafetyMonitorMediator safetyMonitorMediator;

        [ImportingConstructor]
        public SafetyMonitorCondition(ISafetyMonitorMediator safetyMonitorMediator) {
            this.safetyMonitorMediator = safetyMonitorMediator;
        }

        private bool isSafe;

        public bool IsSafe {
            get => isSafe;
            private set {
                isSafe = value;
                RaisePropertyChanged();
            }
        }

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        public bool Validate() {
            var i = new List<string>();
            var info = safetyMonitorMediator.GetInfo();

            if (!info.Connected) {
                i.Add(Locale.Loc.Instance["LblSafetyMonitorNotConnected"]);
            } else {
                IsSafe = info.IsSafe;
            }

            Issues = i;
            return i.Count == 0;
        }

        public override bool Check(ISequenceItem nextItem) {
            var info = safetyMonitorMediator.GetInfo();
            IsSafe = info.Connected && info.IsSafe;
            return IsSafe;
        }

        public override object Clone() {
            return new SafetyMonitorCondition(safetyMonitorMediator) {
                Icon = Icon,
                Name = Name,
                Category = Category,
                Description = Description,
            };
        }

        public override void ResetProgress() {
        }

        public override void SequenceBlockFinished() {
        }

        public override void SequenceBlockStarted() {
        }

        public override string ToString() {
            return $"Condition: {nameof(SafetyMonitorCondition)}";
        }
    }
}