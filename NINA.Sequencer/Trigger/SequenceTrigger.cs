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
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace NINA.Sequencer.Trigger {

    [JsonObject(MemberSerialization.OptIn)]
    public abstract class SequenceTrigger : BaseINPC, ISequenceTrigger {

        public SequenceTrigger() {
            TriggerRunner = new SequentialContainer();
        }

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            this.TriggerRunner?.Items.Clear();
            this.TriggerRunner?.Conditions.Clear();
            this.TriggerRunner?.Triggers.Clear();
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public GeometryGroup Icon { get; set; }
        public string Category { get; set; }

        private bool showMenu;

        public bool ShowMenu {
            get => showMenu;
            set {
                showMenu = value;
                RaisePropertyChanged();
            }
        }

        public ICommand ShowMenuCommand => new RelayCommand((o) => ShowMenu = !ShowMenu);

        [JsonProperty]
        public ISequenceContainer Parent { get; set; }

        [JsonProperty]
        public SequentialContainer TriggerRunner { get; protected set; }

        private SequenceEntityStatus status = SequenceEntityStatus.CREATED;

        public SequenceEntityStatus Status {
            get => status;
            set {
                status = value;
                RaisePropertyChanged();
            }
        }

        //public abstract string Description { get; }

        public async Task Run(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            Status = SequenceEntityStatus.RUNNING;
            try {
                this.TriggerRunner.ResetAll();
                await this.Execute(context, progress, token);

                Status = SequenceEntityStatus.CREATED;
            } catch (OperationCanceledException) {
                Status = SequenceEntityStatus.CREATED;
            } catch (Exception ex) {
                Status = SequenceEntityStatus.FAILED;
                Logger.Error(ex);
                //Todo Error policy - e.g. Continue; Throw and cancel; Retry;
            }
        }

        public virtual void AfterParentChanged() {
        }

        public void AttachNewParent(ISequenceContainer newParent) {
            Parent = newParent;

            AfterParentChanged();
        }

        public abstract bool ShouldTrigger(ISequenceItem nextItem);

        public abstract Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token);

        public abstract object Clone();

        public abstract void Initialize();

        public ICommand DetachCommand => new RelayCommand((o) => Detach());

        public ICommand MoveUpCommand => null;

        public ICommand MoveDownCommand => null;

        public void Detach() {
            Parent?.Remove(this);
        }

        public void MoveUp() {
            throw new NotImplementedException();
        }

        public void MoveDown() {
            throw new NotImplementedException();
        }
    }
}