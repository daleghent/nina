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
using NINA.Sequencer.Container.ExecutionStrategy;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NINA.Core.MyMessageBox;
using NINA.Core.Locale;

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

        public override ICommand ResetProgressCommand => new RelayCommand(
            (o) => {
                if (MyMessageBox.Show(Loc.Instance["Lbl_SequenceContainer_SequenceRootContainer_ResetPrompt"], Loc.Instance["Lbl_SequenceContainer_SequenceRootContainer_ResetPromptCaption"], System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxResult.No) == System.Windows.MessageBoxResult.Yes) {
                    base.ResetProgressCommand.Execute(o);
                }
            }
        );

        private object runningItemsLock = new object();
        private List<ISequenceItem> runningItems = new List<ISequenceItem>();

        public void AddRunningItem(ISequenceItem item) {
            lock (runningItemsLock) {
                runningItems.Add(item);
            }
        }

        public void RemoveRunningItem(ISequenceItem item) {
            lock (runningItemsLock) {
                runningItems.Remove(item);
            }
        }

        public void SkipCurrentRunningItems() {
            lock (runningItemsLock) {
                foreach (var item in runningItems) {
                    item.Skip();
                }
            }
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

        private string sequenceTitle;

        public string SequenceTitle {
            get {
                return string.IsNullOrEmpty(sequenceTitle) ? Name : sequenceTitle;
            }
            set {
                if (sequenceTitle != value) {
                    sequenceTitle = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}