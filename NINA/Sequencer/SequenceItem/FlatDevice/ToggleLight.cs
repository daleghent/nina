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
using NINA.Sequencer.Validations;
using NINA.Utility.Mediator.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.SequenceItem.FlatDevice {

    [ExportMetadata("Name", "Lbl_SequenceItem_FlatDevice_ToggleLight_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_FlatDevice_ToggleLight_Description")]
    [ExportMetadata("Icon", "LightBulbSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_FlatDevice")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class ToggleLight : SequenceItem, IValidatable {

        [ImportingConstructor]
        public ToggleLight(IFlatDeviceMediator flatDeviceMediator) {
            this.flatDeviceMediator = flatDeviceMediator;
        }

        private IFlatDeviceMediator flatDeviceMediator;
        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        private bool on;

        [JsonProperty]
        public bool On {
            get => on;
            set {
                on = value;
                RaisePropertyChanged();
            }
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            // Todo - this interface lacks progress and token
            return Task.Run(() => flatDeviceMediator.ToggleLight(On));
        }

        public override object Clone() {
            return new ToggleLight(flatDeviceMediator) {
                Icon = Icon,
                Name = Name,
                Category = Category,
                Description = Description,
                On = On,
            };
        }

        public bool Validate() {
            var i = new List<string>();
            var info = flatDeviceMediator.GetInfo();
            if (!info.Connected) {
                i.Add(Locale.Loc.Instance["LblFlatDeviceNotConnected"]);
            } else if (!info.SupportsOpenClose) {
                i.Add(Locale.Loc.Instance["Lbl_SequenceItem_Validation_FlatDeviceCannotOpenClose"]);
            }
            Issues = i;
            return i.Count == 0;
        }

        public override void AfterParentChanged() {
            Validate();
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(ToggleLight)}";
        }
    }
}