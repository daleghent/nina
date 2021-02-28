#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Accord.IO;
using Dasync.Collections;
using Newtonsoft.Json;
using NINA.Model;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container.ExecutionStrategy;
using NINA.Sequencer.DragDrop;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Validations;
using NINA.Utility;
using NINA.Utility.Enum;
using NINA.ViewModel.Sequencer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace NINA.Sequencer.Container {

    [JsonObject(MemberSerialization.OptIn)]
    public abstract class SequenceContainer : SequenceItem.SequenceItem, ISequenceContainer, IDropContainer, IConditionable, ITriggerable {
        private bool isExpanded = true;

        [JsonProperty]
        public IExecutionStrategy Strategy { get; }

        public SequenceContainer(IExecutionStrategy strategy) {
            this.Strategy = strategy;
        }

        [JsonProperty]
        public IList<ISequenceCondition> Conditions { get; protected set; } = new ObservableCollection<ISequenceCondition>();

        public virtual ICommand DropIntoCommand => new RelayCommand((o) => {
            DropInSequenceItem(o as DropIntoParameters);
        });

        public ICommand DropIntoConditionsCommand => new RelayCommand((o) => {
            DropInSequenceCondition(o as DropIntoParameters);
        });

        public ICommand DropIntoTriggersCommand => new RelayCommand((o) => {
            DropInSequenceTrigger(o as DropIntoParameters);
        });

        [JsonProperty]
        public bool IsExpanded {
            get {
                return isExpanded;
            }
            set {
                isExpanded = value;
                RaisePropertyChanged(nameof(IsExpanded));
            }
        }

        public int Iterations { get; set; } = 0;

        public IList<string> Issues { get; protected set; } = new ObservableCollection<string>();

        [JsonProperty]
        public IList<ISequenceItem> Items { get; protected set; } = new ObservableCollection<ISequenceItem>();

        public override ICommand ResetProgressCommand => new RelayCommand(
            (o) => {
                ResetAll();
                ResetProgressCascaded();
            }
        );

        [JsonProperty]
        public IList<ISequenceTrigger> Triggers { get; protected set; } = new ObservableCollection<ISequenceTrigger>();

        private void DropInSequenceCondition(DropIntoParameters parameters) {
            ISequenceCondition item;
            var source = parameters.Source as ISequenceCondition;
            //var target = parameters.Target as ISequenceItem;

            if (source?.Parent != null) {
                item = source;
            } else {
                item = (ISequenceCondition)source.Clone();
            }

            if (Conditions.FirstOrDefault(x => x.Name == item.Name) == null) {
                InsertIntoSequenceBlocks(Conditions.Count, item);
            }
        }

        private void DropInSequenceItem(DropIntoParameters parameters) {
            ISequenceItem item;
            ISequenceItem source;

            if (parameters.Source is TemplatedSequenceContainer) {
                item = (parameters.Source as TemplatedSequenceContainer).Clone();
            } else if (parameters.Source is TargetSequenceContainer) {
                item = (parameters.Source as TargetSequenceContainer).Clone();
            } else {
                source = parameters.Source as ISequenceItem;
                if (source.Parent != null && !parameters.Duplicate) {
                    item = source;
                } else {
                    item = (ISequenceItem)source.Clone();
                }
            }
            var target = parameters.Target as ISequenceItem;

            if (parameters.Position == DropTargetEnum.Center && item.Parent != this) {
                InsertIntoSequenceBlocks(Items.Count, item);
            }

            var targetContainer = parameters.Target == this ? Parent : this;

            if (parameters.Position == DropTargetEnum.Bottom || parameters.Position == DropTargetEnum.Top) {
                var newIndex = targetContainer.Items.IndexOf(targetContainer == Parent ? this : target);
                var oldIndex = targetContainer.Items.IndexOf(item);

                var drop = targetContainer as IDropContainer;
                if (oldIndex == -1) {
                    if (parameters.Position == DropTargetEnum.Top) {
                        drop.InsertIntoSequenceBlocks(newIndex, item);
                    } else {
                        drop.InsertIntoSequenceBlocks(newIndex + 1, item);
                    }
                } else {
                    if (parameters.Position == DropTargetEnum.Top && newIndex > oldIndex) newIndex--;
                    if (parameters.Position == DropTargetEnum.Bottom && newIndex < oldIndex) newIndex++;
                    drop.MoveWithinIntoSequenceBlocks(oldIndex, newIndex);
                }
            }

            RaisePropertyChanged(nameof(Items));
        }

        private void DropInSequenceTrigger(DropIntoParameters parameters) {
            ISequenceTrigger item;
            var source = parameters.Source as ISequenceTrigger;
            //var target = parameters.Target as ISequenceItem;

            if (source.Parent != null) {
                item = source;
            } else {
                item = (ISequenceTrigger)source.Clone();
            }

            if (Triggers.FirstOrDefault(x => x.Name == item.Name) == null) {
                InsertIntoSequenceBlocks(Triggers.Count, item);
            }
        }

        public void Add(ISequenceItem item) {
            item.Parent?.Remove(item);
            item.AttachNewParent(this);
            Items.Add(item);
        }

        public void Add(ISequenceCondition condition) {
            condition.Parent?.Remove(condition);
            condition.AttachNewParent(this);
            Conditions.Add(condition);
        }

        public void Add(ISequenceTrigger trigger) {
            trigger.Parent?.Remove(trigger);
            trigger.AttachNewParent(this);
            Triggers.Add(trigger);
        }

        public override void AfterParentChanged() {
            foreach (var item in Items) {
                item.AfterParentChanged();

                IValidatable validatable = (item as IValidatable);
                if (validatable != null) {
                    validatable.Validate();
                }
            }
            foreach (var condition in Conditions) {
                condition.AfterParentChanged();

                IValidatable validatable = (condition as IValidatable);
                if (validatable != null) {
                    validatable.Validate();
                }
            }
            foreach (var trigger in Triggers) {
                trigger.AfterParentChanged();

                IValidatable validatable = (trigger as IValidatable);
                if (validatable != null) {
                    validatable.Validate();
                }
            }
        }

        public bool CheckConditions(ISequenceItem nextItem) {
            if (Conditions.Count == 0) {
                return false;
            }

            bool check = true;
            foreach (var condition in Conditions) {
                check = check && condition.Check(nextItem);
            }

            return check;
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            return Strategy.Execute(this, progress, token);
        }

        public ISequenceRootContainer GetRootContainer(ISequenceContainer container) {
            if (container.Parent == null) {
                if (!(container is ISequenceRootContainer)) {
                    return null;
                } else {
                    return container as ISequenceRootContainer;
                }
            } else {
                return GetRootContainer(container.Parent);
            }
        }

        public void InsertIntoSequenceBlocks(int index, ISequenceTrigger sequenceBlock) {
            if (sequenceBlock.Parent != this) {
                sequenceBlock.Parent?.Remove(sequenceBlock);
                sequenceBlock.AttachNewParent(this);
            }

            if (index == Triggers.Count) {
                Triggers.Add(sequenceBlock);
            } else {
                Triggers.Insert(index, sequenceBlock);
            }
        }

        public void InsertIntoSequenceBlocks(int index, ISequenceCondition sequenceBlock) {
            if (sequenceBlock.Parent != this) {
                sequenceBlock.Parent?.Remove(sequenceBlock);
                sequenceBlock.AttachNewParent(this);
            }

            if (index == Conditions.Count) {
                Conditions.Add(sequenceBlock);
            } else {
                Conditions.Insert(index, sequenceBlock);
            }
        }

        public void InsertIntoSequenceBlocks(int index, ISequenceItem sequenceBlock) {
            if (index < 0) return;

            if (sequenceBlock.Parent != this) {
                sequenceBlock.Parent?.Remove(sequenceBlock);
                sequenceBlock.AttachNewParent(this);
            }

            if (index > Items.Count) {
                Items.Add(sequenceBlock);
            } else {
                Items.Insert(index, sequenceBlock);
            }
        }

        public void MoveDown(ISequenceItem item) {
            var index = Items.IndexOf(item);
            if (index == Items.Count - 1) {
                if (Parent == null) return;
                var thisIndex = Parent.Items.IndexOf(this);

                // Special handling for root level having the static three container
                if (Parent is SequenceRootContainer) {
                    if (thisIndex == Parent.Items.Count - 1) return;
                    var newParent = Parent.Items[thisIndex + 1] as ISequenceContainer;
                    newParent.Items.Insert(0, item);
                    item.Parent?.Remove(item);
                    item.AttachNewParent(newParent);
                } else {
                    if (thisIndex == Parent.Items.Count - 1) {
                        Parent.Items.Add(item);
                    } else {
                        Parent.Items.Insert(thisIndex + 1, item);
                    }
                    item.Parent?.Remove(item);
                    item.AttachNewParent(Parent);
                }
            } else {
                int newIndex = index + 1;
                var container = Items[newIndex] as ISequenceContainer;
                if (container?.IsExpanded == true && !(container is IImmutableContainer)) {
                    container.Items.Insert(0, item);
                    item.Parent?.Remove(item);
                    item.AttachNewParent(container);
                } else {
                    MoveWithinIntoSequenceBlocks(index, newIndex);
                }
            }
        }

        public void MoveUp(ISequenceItem item) {
            var index = Items.IndexOf(item);
            if (index == 0) {
                if (Parent == null) return;
                var thisIndex = Parent.Items.IndexOf(this);

                // Special handling for root level having the static three container
                if (Parent is SequenceRootContainer) {
                    if (thisIndex == 0) return;
                    var newParent = Parent.Items[thisIndex - 1] as ISequenceContainer;
                    newParent.Add(item);
                } else {
                    Parent.Items.Insert(thisIndex, item);
                    item.Parent?.Remove(item);
                    item.AttachNewParent(Parent);
                }
            } else {
                int newIndex = index - 1;
                var container = Items[newIndex] as ISequenceContainer;
                if (container?.IsExpanded == true && !(container is IImmutableContainer)) {
                    container.Items.Add(item);
                    item.Parent?.Remove(item);
                    item.AttachNewParent(container);
                } else {
                    MoveWithinIntoSequenceBlocks(index, newIndex);
                }
            }
        }

        public void MoveWithinIntoSequenceBlocks(int index, int newIndex) {
            if (Items.Count == 0) return;
            if (index < 0 || index >= Items.Count) return;
            if (newIndex < 0) return;

            if (index == newIndex) return;
            var item = Items[index];
            Items.RemoveAt(index);
            if (newIndex >= Items.Count) {
                Items.Add(item);
            } else {
                Items.Insert(newIndex, item);
            }
        }

        public bool Remove(ISequenceItem item) {
            if (item.Parent == this) {
                item.AttachNewParent(null);
            }
            item.Parent?.Remove(item);
            return Items.Remove(item);
        }

        public bool Remove(ISequenceCondition condition) {
            if (condition.Parent == this) {
                condition.AttachNewParent(null);
            }
            condition.Parent?.Remove(condition);
            return Conditions.Remove(condition);
        }

        public bool Remove(ISequenceTrigger trigger) {
            if (trigger.Parent == this) {
                trigger.AttachNewParent(null);
            }
            trigger.Parent?.Remove(trigger);
            return Triggers.Remove(trigger);
        }

        public void ResetConditions() {
            foreach (var condition in Conditions) {
                condition.ResetProgress();
            }
        }

        public void ResetAll() {
            ResetConditions();
            ResetProgress();
            foreach (var child in Items) {
                if (child is ISequenceContainer) (child as ISequenceContainer).ResetAll();
                else child.ResetProgress();
            }
        }

        public override void ResetProgress() {
            base.ResetProgress();
            Iterations = 0;
            foreach (var item in Items) {
                item.ResetProgress();
            }
        }

        public override void ResetProgressCascaded() {
            base.ResetProgress();
            this.Parent?.ResetProgressCascaded();
        }

        public async Task RunTriggers(ISequenceItem nextItem, IProgress<ApplicationStatus> progress, CancellationToken token) {
            foreach (var trigger in Triggers) {
                if (trigger.ShouldTrigger(nextItem)) {
                    await trigger.Run(nextItem.Parent, progress, token);
                }
            }
        }

        public virtual bool Validate() {
            var valid = true;
            foreach (var item in Items) {
                IValidatable validatable = (item as IValidatable);
                if (validatable != null) {
                    valid = validatable.Validate() && valid;
                }
            }
            foreach (var item in Conditions) {
                IValidatable validatable = (item as IValidatable);
                if (validatable != null) {
                    valid = validatable.Validate() && valid;
                }
            }
            foreach (var item in Triggers) {
                IValidatable validatable = (item as IValidatable);
                if (validatable != null) {
                    valid = validatable.Validate() && valid;
                }
            }
            return valid;
        }

        public override string ToString() {
            var conditionString = string.Empty;
            foreach (var condition in Conditions) {
                conditionString += condition.ToString() + ", ";
            }
            var triggerString = string.Empty;
            foreach (var trigger in Triggers) {
                triggerString += trigger.ToString();
            }
            return $"Category: {Category}, Item: {this.GetType()}, Strategy: {Strategy.GetType().Name}, Items: {Items.Count}, Conditions: {conditionString} Triggers: {triggerString}";
        }
    }
}