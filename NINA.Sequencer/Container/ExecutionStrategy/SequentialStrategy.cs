#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace NINA.Sequencer.Container.ExecutionStrategy {

    public class SequentialStrategy : IExecutionStrategy {

        public object Clone() {
            return new SequentialStrategy();
        }

        public async Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            ISequenceItem previous = null;
            ISequenceItem next = null;
            context.Iterations = 0;

            InitializeBlock(context);

            try {
                while ((next = GetNextItem(context)) != null && CanContinue(context, previous, next)) {
                    StartBlock(context);

                    while ((next = GetNextItem(context)) != null && CanContinue(context, previous, next)) {
                        token.ThrowIfCancellationRequested();
                        await RunTriggers(context, previous, next, progress, token);
                        await next.Run(progress, token);
                        previous = next;
                    }

                    FinishBlock(context);

                    if (CanContinue(context, previous, next)) {
                        foreach (var item in context.GetItemsSnapshot()) {
                            if (item is ISequenceContainer) {
                                (item as ISequenceContainer).ResetAll();
                            } else {
                                item.ResetProgress();
                            }
                        }
                    }
                }

                context.Skip();
                //Mark rest of items as skipped
                foreach (var item in context.GetItemsSnapshot().Where(x => x.Status == SequenceEntityStatus.CREATED)) {
                    item.Skip();
                }
            } finally {
                TeardownBlock(context);
            }
        }

        private void TeardownBlock(ISequenceContainer context) {
            var conditionable = context as IConditionable;
            if (conditionable != null) {
                foreach (var condition in conditionable.GetConditionsSnapshot()) {
                    condition.SequenceBlockTeardown();
                }
            }
            var triggerable = context as ITriggerable;
            if (triggerable != null) {
                foreach (var trigger in triggerable.GetTriggersSnapshot()) {
                    trigger.SequenceBlockTeardown();
                }
            }
        }

        private void InitializeBlock(ISequenceContainer context) {
            var conditionable = context as IConditionable;
            if (conditionable != null) {
                foreach (var condition in conditionable.GetConditionsSnapshot()) {
                    condition.SequenceBlockInitialize();
                }
            }
            var triggerable = context as ITriggerable;
            if (triggerable != null) {
                foreach (var trigger in triggerable.GetTriggersSnapshot()) {
                    trigger.SequenceBlockInitialize();
                }
            }
        }

        private ISequenceItem GetNextItem(ISequenceContainer context) {
            var items = context.GetItemsSnapshot();
            return items.FirstOrDefault(x => x.Status == SequenceEntityStatus.CREATED);
        }

        private async Task RunTriggers(ISequenceContainer container, ISequenceItem previousItem, ISequenceItem nextItem, IProgress<ApplicationStatus> progress, CancellationToken token) {
            var triggerable = container as ITriggerable;
            if (triggerable != null) {
                await triggerable.RunTriggers(previousItem, nextItem, progress, token);
            }

            if (container.Parent != null) {
                await RunTriggers(container.Parent, previousItem, nextItem, progress, token);
            }
        }

        private void StartBlock(ISequenceContainer container) {
            var conditionable = container as IConditionable;
            if (conditionable != null) {
                foreach (var condition in conditionable.GetConditionsSnapshot()) {
                    condition.SequenceBlockStarted();
                }
            }
            var triggerable = container as ITriggerable;
            if (triggerable != null) {
                foreach (var trigger in triggerable.GetTriggersSnapshot()) {
                    trigger.SequenceBlockStarted();
                }
            }
        }

        private void FinishBlock(ISequenceContainer container) {
            container.Iterations++;

            var conditionable = container as IConditionable;
            if (conditionable != null) {
                foreach (var condition in conditionable.GetConditionsSnapshot()) {
                    condition.SequenceBlockFinished();
                }
            }

            var triggerable = container as ITriggerable;
            if (triggerable != null) {
                foreach (var trigger in triggerable.GetTriggersSnapshot()) {
                    trigger.SequenceBlockFinished();
                }
            }
        }

        private bool CanContinue(ISequenceContainer container, ISequenceItem previousItem, ISequenceItem nextItem) {
            var conditionable = container as IConditionable;
            var canContinue = false;
            var conditions = conditionable?.GetConditionsSnapshot();
            if (conditions.Count > 0) {
                canContinue = conditionable.CheckConditions(previousItem, nextItem);
            } else {
                canContinue = container.Iterations < 1;
            }

            if (container.Parent != null) {
                canContinue = canContinue && CanContinue(container.Parent, previousItem, nextItem);
            }

            return canContinue;
        }
    }
}