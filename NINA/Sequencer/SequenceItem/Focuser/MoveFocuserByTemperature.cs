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

namespace NINA.Sequencer.SequenceItem.Focuser {

    [ExportMetadata("Name", "Lbl_SequenceItem_Focuser_MoveFocuserByTemperature_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Focuser_MoveFocuserByTemperature_Description")]
    [ExportMetadata("Icon", "MoveFocuserByTemperatureSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Focuser")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class MoveFocuserByTemperature : SequenceItem, IValidatable {

        [ImportingConstructor]
        public MoveFocuserByTemperature(IFocuserMediator focuserMediator) {
            this.focuserMediator = focuserMediator;
        }

        private IFocuserMediator focuserMediator;

        private double slope = 1;

        [JsonProperty]
        public double Slope {
            get => slope;
            set {
                slope = value;
                RaisePropertyChanged();
            }
        }

        private double intercept = 0;

        [JsonProperty]
        public double Intercept {
            get => intercept;
            set {
                intercept = value;
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

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (Validate()) {
                var info = focuserMediator.GetInfo();
                var position = Slope * info.Temperature + Intercept;

                return focuserMediator.MoveFocuser((int)position, token);
            } else {
                throw new SequenceItemSkippedException(string.Join(",", Issues));
            }
        }

        public override object Clone() {
            return new MoveFocuserByTemperature(focuserMediator) {
                Icon = Icon,
                Name = Name,
                Category = Category,
                Description = Description,
                Slope = Slope,
                Intercept = Intercept,
            };
        }

        public bool Validate() {
            var i = new List<string>();
            var info = focuserMediator.GetInfo();
            if (!info.Connected) {
                i.Add(Locale.Loc.Instance["LblFocuserNotConnected"]);
            } else {
                if (double.IsNaN(info.Temperature)) {
                    i.Add(Locale.Loc.Instance["Lbl_SequenceItem_Focuser_MoveFocuserByTemperature_Validation_NoTemperature"]);
                }
            }
            Issues = i;
            return i.Count == 0;
        }

        public override void AfterParentChanged() {
            Validate();
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(MoveFocuserByTemperature)}, Slope: {Slope}, Intercept {Intercept}";
        }
    }
}