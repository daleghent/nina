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
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.Utility;

namespace NINA.Sequencer.Conditions {

    [JsonObject(MemberSerialization.OptIn)]
    public abstract class SequenceCondition : BaseINPC, ISequenceCondition {

        public SequenceCondition() {
        }

        public SequenceCondition(SequenceCondition cloneMe) {
            CopyMetaData(cloneMe);
        }

        protected void CopyMetaData(SequenceCondition cloneMe) {
            Icon = cloneMe.Icon;
            Name = cloneMe.Name;
            Category = cloneMe.Category;
            Description = cloneMe.Description;
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public GeometryGroup Icon { get; set; }
        public string Category { get; set; }
        public SequenceEntityStatus Status { get; set; }

        [JsonProperty]
        public ISequenceContainer Parent { get; set; }

        public ICommand ResetProgressCommand => new RelayCommand((o) => { ResetProgress(); ShowMenu = false; });

        private bool showMenu;

        public bool ShowMenu {
            get => showMenu;
            set {
                showMenu = value;
                RaisePropertyChanged();
            }
        }

        public IConditionWatchdog ConditionWatchdog { get; set; }

        protected void RunWatchdogIfInsideSequenceRoot() {
            if (ConditionWatchdog != null) {
                if (ItemUtility.IsInRootContainer(Parent)) {
                    ConditionWatchdog.Start();
                } else {
                    ConditionWatchdog.Cancel();
                }
            }
        }

        public ICommand ShowMenuCommand => new RelayCommand((o) => ShowMenu = !ShowMenu);

        public virtual void AfterParentChanged() {
        }

        public void AttachNewParent(ISequenceContainer newParent) {
            Parent = newParent;

            AfterParentChanged();
        }

        public abstract bool Check(ISequenceItem nextItem);

        public abstract object Clone();

        public virtual void ResetProgress() {
        }

        public virtual void Initialize() {
        }

        public virtual void SequenceBlockInitialize() {
        }

        public virtual void SequenceBlockStarted() {
        }

        public virtual void SequenceBlockFinished() {
        }

        public virtual void SequenceBlockTeardown() {
        }

        public virtual void Teardown() {
        }

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