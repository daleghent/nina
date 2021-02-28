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
using NINA.Sequencer.Container.ExecutionStrategy;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.Sequencer.Container {

    [ExportMetadata("Name", "Lbl_SequenceContainer_SequenceRootContainer_Name")]
    [ExportMetadata("Description", "Lbl_SequenceContainer_SequentialContainer_Description")]
    [ExportMetadata("Icon", "BoxSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Container")]
    [Export(typeof(ISequenceContainer))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SequenceRootContainer : SequenceContainer, ISequenceRootContainer, IImmutableContainer {

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            this.Items.Clear();
        }

        [ImportingConstructor]
        public SequenceRootContainer() : base(new SequentialStrategy()) {
        }

        public override ICommand DropIntoCommand => new RelayCommand((o) => {
            (Items[1] as TargetAreaContainer).DropIntoCommand.Execute(o);
        });

        public override object Clone() {
            return new SequenceRootContainer() {
                Icon = Icon,
                Name = Name,
                Category = Category,
                Description = Description,
            };
        }
    }
}