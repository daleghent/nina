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
using NINA.Sequencer.Container;
using NINA.Sequencer.DragDrop;
using NINA.Sequencer.Exceptions;
using NINA.Sequencer.Validations;
using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace NINA.Sequencer.SequenceItem {

    [JsonObject(MemberSerialization.OptIn)]
    public abstract class SequenceItem : BaseINPC, ISequenceItem {
        private SequenceEntityStatus status = SequenceEntityStatus.CREATED;
        public string Category { get; set; }
        public string Description { get; set; }
        public ICommand DetachCommand => new RelayCommand((o) => Detach());
        public GeometryGroup Icon { get; set; }
        public ICommand MoveDownCommand => new RelayCommand((o) => MoveDown());
        public ICommand MoveUpCommand => new RelayCommand((o) => MoveUp());
        public ICommand AddCloneToParentCommand => new RelayCommand((o) => AddCloneToParent());

        private string name;

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

        public virtual ICommand ResetProgressCommand => new RelayCommand((o) => ResetProgressCascaded());

        public SequenceEntityStatus Status {
            get => status;
            set {
                status = value;
                RaisePropertyChanged();
            }
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

        public void MoveDown() {
            Parent?.MoveDown(this);
        }

        public void MoveUp() {
            Parent?.MoveUp(this);
        }

        public void AddCloneToParent() {
            Parent?.Add((ISequenceItem)this.Clone());
        }

        public virtual void ResetProgress() {
            this.Status = SequenceEntityStatus.CREATED;
        }

        public virtual void ResetProgressCascaded() {
            ResetProgress();
            this.Parent?.ResetProgressCascaded();
        }

        public async Task Run(IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (Status == SequenceEntityStatus.CREATED) {
                Status = SequenceEntityStatus.RUNNING;
                try {
                    Logger.Info($"Starting {this}");
                    if (this is IValidatable && !(this is ISequenceContainer)) {
                        var validatable = this as IValidatable;
                        if (!validatable.Validate()) {
                            throw new SequenceItemSkippedException(string.Join(",", validatable.Issues));
                        }
                    }

                    await this.Execute(progress, token);
                    Logger.Info($"Finishing {this}");
                    Status = SequenceEntityStatus.FINISHED;
                } catch (SequenceItemSkippedException ex) {
                    Logger.Warning($"{this} - " + ex.Message);
                    Status = SequenceEntityStatus.SKIPPED;
                } catch (OperationCanceledException ex) {
                    Status = SequenceEntityStatus.CREATED;
                    Logger.Debug($"Cancelled {this}");
                    throw ex;
                } catch (Exception ex) {
                    Status = SequenceEntityStatus.FAILED;
                    Logger.Error($"{this} - ", ex);
                    //Todo Error policy - e.g. Continue; Throw and cancel; Retry;
                } finally {
                    progress?.Report(new ApplicationStatus());
                }
            }
        }

        public void Skip() {
            this.Status = SequenceEntityStatus.SKIPPED;
        }
    }
}