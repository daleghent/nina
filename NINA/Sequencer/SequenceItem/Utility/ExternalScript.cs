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
using NINA.Sequencer.Exceptions;
using NINA.Sequencer.Validations;
using NINA.Utility;
using NINA.Utility.ExternalCommand;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.SequenceItem.Utility {

    [ExportMetadata("Name", "Lbl_SequenceItem_Utility_ExternalScript_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Utility_ExternalScript_Description")]
    [ExportMetadata("Icon", "ScriptSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Utility")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class ExternalScript : SequenceItem, IValidatable {

        public ExternalScript() {
        }

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        private string script;

        [JsonProperty]
        public string Script {
            get => script;
            set {
                script = value;
                RaisePropertyChanged();
            }
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (Validate()) {
                string sequenceCompleteCommand = Script;
                ExternalCommandExecutor externalCommandExecutor = new ExternalCommandExecutor(progress);
                return externalCommandExecutor.RunSequenceCompleteCommandTask(sequenceCompleteCommand, token);
            } else {
                throw new SequenceItemSkippedException(string.Join(",", Issues));
            }
        }

        public override object Clone() {
            return new ExternalScript() {
                Icon = Icon,
                Script = Script,
                Name = Name,
                Description = Description,
            };
        }

        public bool Validate() {
            var i = new List<string>();
            var sequenceCompleteCommand = Script;
            if (!string.IsNullOrWhiteSpace(sequenceCompleteCommand) && !ExternalCommandExecutor.CommandExists(sequenceCompleteCommand)) {
                i.Add(string.Format(Locale.Loc.Instance["LblSequenceCommandAtCompletionNotFound"], ExternalCommandExecutor.GetComandFromString(sequenceCompleteCommand)));
            }
            Issues = i;
            return i.Count == 0;
        }

        public override void AfterParentChanged() {
            Validate();
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(ExternalScript)}, Script: {Script}";
        }
    }
}