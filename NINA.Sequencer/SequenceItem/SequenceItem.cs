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
using NINA.Sequencer.Validations;
using NINA.Core.Utility;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using NINA.Sequencer.Utility;

namespace NINA.Sequencer.SequenceItem {

    [JsonObject(MemberSerialization.OptIn)]
    public abstract class SequenceItem : BaseINPC, ISequenceItem {

        public SequenceItem() {
        }

        public SequenceItem(SequenceItem cloneMe) {
            CopyMetaData(cloneMe);
        }

        protected void CopyMetaData(SequenceItem cloneMe) {
            Icon = cloneMe.Icon;
            Name = cloneMe.Name;
            Category = cloneMe.Category;
            Description = cloneMe.Description;
            Attempts = cloneMe.Attempts;
            ErrorBehavior = cloneMe.ErrorBehavior;
        }

        private string name;
        private bool showMenu;
        private SequenceEntityStatus status = SequenceEntityStatus.CREATED;
        public ICommand AddCloneToParentCommand => new RelayCommand((o) => { AddCloneToParent(); ShowMenu = false; });
        public string Category { get; set; }
        public string Description { get; set; }
        public ICommand DetachCommand => new RelayCommand((o) => Detach());
        public GeometryGroup Icon { get; set; }
        public ICommand MoveDownCommand => new RelayCommand((o) => MoveDown());
        public ICommand MoveUpCommand => new RelayCommand((o) => MoveUp());

        [JsonProperty]
        public string Name {
            get => name;
            set {
                name = value;
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public ISequenceContainer Parent { get; private set; }

        private InstructionErrorBehavior errorBehavior = InstructionErrorBehavior.ContinueOnError;

        [JsonProperty]
        public virtual InstructionErrorBehavior ErrorBehavior {
            get => errorBehavior;
            set {
                errorBehavior = value;
                RaisePropertyChanged();
            }
        }

        private int attempts = 1;

        [JsonProperty]
        public virtual int Attempts {
            get => attempts;
            set {
                if (value > 0) {
                    attempts = value;
                    RaisePropertyChanged();
                }
            }
        }

        public virtual ICommand ResetProgressCommand => new RelayCommand((o) => { ResetProgressCascaded(); ShowMenu = false; });

        public bool ShowMenu {
            get => showMenu;
            set {
                showMenu = value;
                RaisePropertyChanged();
            }
        }

        public ICommand ShowMenuCommand => new RelayCommand((o) => ShowMenu = !ShowMenu);

        public SequenceEntityStatus Status {
            get => status;
            set {
                status = value;
                RaisePropertyChanged();
            }
        }

        public void AddCloneToParent() {
            Parent?.Add((ISequenceItem)this.Clone());
        }

        public virtual void AfterParentChanged() {
            //Hook for behavior when parent changes
        }

        public void AttachNewParent(ISequenceContainer newParent) {
            Parent = newParent;

            AfterParentChanged();
        }

        public abstract object Clone();

        public void Detach() {
            Parent?.Remove(this);
        }

        public abstract Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token);

        public virtual TimeSpan GetEstimatedDuration() {
            return TimeSpan.Zero;
        }

        public virtual void MoveDown() {
            Parent?.MoveDown(this);
        }

        public virtual void MoveUp() {
            Parent?.MoveUp(this);
        }

        public virtual void ResetProgress() {
            this.Status = SequenceEntityStatus.CREATED;
        }

        public virtual void ResetProgressCascaded() {
            ResetProgress();
            this.Parent?.ResetProgressCascaded();
        }

        private CancellationTokenSource localCts;

        public async Task Run(IProgress<ApplicationStatus> progress, CancellationToken token) {
            using (localCts = CancellationTokenSource.CreateLinkedTokenSource(token)) {
                if (Status == SequenceEntityStatus.CREATED) {
                    Status = SequenceEntityStatus.RUNNING;

                    var root = ItemUtility.GetRootContainer(this.Parent);

                    try {
                        Logger.Info($"Starting {this}");
                        if (this is IValidatable && !(this is ISequenceContainer)) {
                            var validatable = this as IValidatable;
                            if (!validatable.Validate()) {
                                throw new SequenceItemSkippedException(string.Join(",", validatable.Issues));
                            }
                        }

                        if (root != null && !(this is ISequenceContainer)) {
                            root.AddRunningItem(this);
                        }

                        var success = false;
                        for (int i = 0; i < Attempts; i++) {
                            try {
                                await this.Execute(progress, localCts.Token);

                                Logger.Info($"Finishing {this}");
                                Status = SequenceEntityStatus.FINISHED;
                                success = true;
                            } catch (SequenceItemSkippedException ex) {
                                throw ex;
                            } catch (OperationCanceledException ex) {
                                throw ex;
                            } catch (Exception ex) {
                                Logger.Error($"{this} - ", ex);
                                success = false;
                            }
                        }

                        if (!success) {
                            var attemptWord = Attempts > 1 ? "attempts" : "attempt";
                            Status = SequenceEntityStatus.FAILED;
                            switch (ErrorBehavior) {
                                case InstructionErrorBehavior.AbortOnError:
                                    Logger.Error($"Instruction failed after {Attempts} {attemptWord}. Error behavior is set to {ErrorBehavior}. Aborting Sequence!");
                                    _ = root.Interrupt();
                                    break;

                                case InstructionErrorBehavior.SkipInstructionSetOnError:
                                    Logger.Error($"Instruction failed after {Attempts} {attemptWord}. Error behavior is set to {ErrorBehavior}. Skipping current instruction set.");
                                    _ = Parent?.Interrupt();
                                    break;

                                case InstructionErrorBehavior.SkipToSequenceEndInstructions:
                                    Logger.Error($"Instruction failed after {Attempts} {attemptWord}. Error behavior is set to {ErrorBehavior}. Skipping to end of sequence instructions.");
                                    _ = SkipToEndOfSequence(root);
                                    break;

                                default:
                                    Logger.Error($"Instruction failed after {Attempts} {attemptWord}. Error behavior is set to {ErrorBehavior}. Continuing.");
                                    break;
                            }
                        }
                    } catch (SequenceItemSkippedException ex) {
                        Logger.Warning($"{this} - " + ex.Message);
                        Status = SequenceEntityStatus.SKIPPED;
                    } catch (OperationCanceledException ex) {
                        if (localCts.IsCancellationRequested) {
                            Status = SequenceEntityStatus.SKIPPED;
                            Logger.Debug($"Skipped {this}");
                        } else {
                            Status = SequenceEntityStatus.CREATED;
                            Logger.Debug($"Cancelled {this}");
                            throw ex;
                        }
                    } finally {
                        progress?.Report(new ApplicationStatus());
                        if (root != null && !(this is ISequenceContainer)) {
                            root?.RemoveRunningItem(this);
                        }
                    }
                }
            }
        }

        private async Task<bool> SkipToEndOfSequence(ISequenceRootContainer root) {
            var startContainer = root.Items[0] as ISequenceContainer;
            var targetContainer = root.Items[1] as ISequenceContainer;
            if (startContainer.Status == SequenceEntityStatus.RUNNING) {
                await startContainer.Interrupt();
                await Task.Delay(100);
            }
            if (targetContainer.Status == SequenceEntityStatus.RUNNING) {
                await targetContainer.Interrupt();
            }
            return true;
        }

        public void Skip() {
            this.Status = SequenceEntityStatus.SKIPPED;
            localCts?.Cancel();
        }
    }
}