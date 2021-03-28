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
using NINA.Model;
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
            ISequenceItem next = null;
            context.Iterations = 0;

            while ((next = context.Items.FirstOrDefault(x => x.Status == SequenceEntityStatus.CREATED)) != null && CanContinue(context, next)) {
                StartBlock(context);

                while ((next = context.Items.FirstOrDefault(x => x.Status == SequenceEntityStatus.CREATED)) != null && CanContinue(context, next)) {
                    token.ThrowIfCancellationRequested();
                    await RunTriggers(context, next, progress, token);
                    await next.Run(progress, token);
                }

                FinishBlock(context);

                if (CanContinue(context, next)) {
                    foreach (var item in context.Items) {
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
            foreach (var item in context.Items.Where(x => x.Status == SequenceEntityStatus.CREATED)) {
                item.Skip();
            }
        }

        private async Task RunTriggers(ISequenceContainer container, ISequenceItem nextItem, IProgress<ApplicationStatus> progress, CancellationToken token) {
            var triggerable = container as ITriggerable;
            if (triggerable != null) {
                await triggerable.RunTriggers(nextItem, progress, token);
            }

            if (container.Parent != null) {
                await RunTriggers(container.Parent, nextItem, progress, token);
            }
        }

        private void StartBlock(ISequenceContainer container) {
            var conditionable = container as IConditionable;
            if (conditionable != null) {
                foreach (var condition in conditionable.Conditions) {
                    condition.SequenceBlockStarted();
                }
            }
            var triggerable = container as ITriggerable;
            if (triggerable != null) {
                foreach (var trigger in triggerable.Triggers) {
                    trigger.Initialize();
                }
            }
        }

        private void FinishBlock(ISequenceContainer container) {
            container.Iterations++;

            var conditionable = container as IConditionable;
            if (conditionable != null) {
                foreach (var condition in conditionable.Conditions) {
                    condition.SequenceBlockFinished();
                }
            }
        }

        private bool CanContinue(ISequenceContainer container, ISequenceItem nextItem) {
            var conditionable = container as IConditionable;
            var canContinue = false;
            if (conditionable?.Conditions?.Count > 0) {
                if (conditionable.Conditions.Count > 0) {
                    canContinue = conditionable.CheckConditions(nextItem);
                }
            } else {
                canContinue = container.Iterations < 1;
            }

            if (container.Parent != null) {
                canContinue = canContinue && CanContinue(container.Parent, nextItem);
            }

            return canContinue;
        }
    }
}