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
using NINA.Core.Model;
using NINA.Sequencer.Validations;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Locale;

namespace NINA.Sequencer.SequenceItem.Focuser {

    [ExportMetadata("Name", "Lbl_SequenceItem_Focuser_MoveFocuserAbsolute_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Focuser_MoveFocuserAbsolute_Description")]
    [ExportMetadata("Icon", "MoveFocuserSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Focuser")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class MoveFocuserAbsolute : SequenceItem, IValidatable {

        [ImportingConstructor]
        public MoveFocuserAbsolute(IFocuserMediator focuserMediator) {
            this.focuserMediator = focuserMediator;
        }

        private IFocuserMediator focuserMediator;

        private int position = 0;

        [JsonProperty]
        public int Position {
            get => position;
            set {
                position = value;
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
                // todo - Interface lacks progress
                return focuserMediator.MoveFocuser(Position, token);
            } else {
                throw new SequenceItemSkippedException(string.Join(",", Issues));
            }
        }

        public override object Clone() {
            return new MoveFocuserAbsolute(focuserMediator) {
                Icon = Icon,
                Name = Name,
                Category = Category,
                Description = Description,
                Position = Position
            };
        }

        public bool Validate() {
            var i = new List<string>();
            if (!focuserMediator.GetInfo().Connected) {
                i.Add(Loc.Instance["LblFocuserNotConnected"]);
            }
            Issues = i;
            return i.Count == 0;
        }

        public override void AfterParentChanged() {
            Validate();
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(MoveFocuserAbsolute)}, Position: {Position}";
        }
    }
}