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
using NINA.Model.MySwitch;
using NINA.Sequencer.Exceptions;
using NINA.Sequencer.Validations;
using NINA.Utility.Mediator.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.SequenceItem.Switch {

    [ExportMetadata("Name", "Lbl_SequenceItem_Switch_SetSwitchValue_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Switch_SetSwitchValue_Description")]
    [ExportMetadata("Icon", "ButtonSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Switch")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SetSwitchValue : SequenceItem, IValidatable {
        private ISwitchMediator switchMediator;

        [ImportingConstructor]
        public SetSwitchValue(ISwitchMediator switchMediator) {
            this.switchMediator = switchMediator;
            this.switchIndex = -1;
            SwitchInfo = switchMediator.GetInfo();
        }

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        public override object Clone() {
            return new SetSwitchValue(switchMediator) {
                Icon = Icon,
                Name = Name,
                Category = Category,
                Description = Description,
                SwitchIndex = SwitchIndex,
                Value = Value
            };
        }

        private double value;

        [JsonProperty]
        public double Value {
            get => value;
            set {
                this.value = value;
                Validate();
                RaisePropertyChanged();
            }
        }

        private short switchIndex;

        [JsonProperty]
        public short SwitchIndex {
            get => switchIndex;
            set {
                if (value > -1) {
                    switchIndex = value;
                    RaisePropertyChanged();
                }
            }
        }

        private IWritableSwitch selectedSwitch;

        [JsonIgnore]
        public IWritableSwitch SelectedSwitch {
            get => selectedSwitch;
            set {
                selectedSwitch = value;
                Validate();
                RaisePropertyChanged();
            }
        }

        private SwitchInfo switchInfo;

        public SwitchInfo SwitchInfo {
            get => switchInfo;
            set {
                switchInfo = value;
                RaisePropertyChanged();
            }
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (Validate()) {
                return switchMediator.SetSwitchValue(switchIndex, Value, progress, token);
            } else {
                throw new SequenceItemSkippedException(string.Join(",", Issues));
            }
        }

        public bool Validate() {
            var i = new List<string>();
            SwitchInfo = switchMediator.GetInfo();
            if (!SwitchInfo?.Connected == true) {
                i.Add(Locale.Loc.Instance["LblSwitchNotConnected"]);
            } else {
                if (SelectedSwitch == null && switchIndex >= 0 && SwitchInfo.WritableSwitches.Count > switchIndex) {
                    SelectedSwitch = SwitchInfo.WritableSwitches[switchIndex];
                }
                if (SelectedSwitch == null) {
                    i.Add(string.Format(Locale.Loc.Instance["Lbl_SequenceItem_Validation_NoSwitchSelected"]));
                }
            }
            var s = SelectedSwitch;
            if (s != null) {
                if (Value < s.Minimum || Value > s.Maximum)
                    i.Add(string.Format(Locale.Loc.Instance["Lbl_SequenceItem_Validation_InvalidSwitchValue"], s.Minimum, s.Maximum, s.StepSize));
            }
            Issues = i;
            return i.Count == 0;
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(SetSwitchValue)}, SwitchIndex {SwitchIndex}, Value: {Value}";
        }
    }
}