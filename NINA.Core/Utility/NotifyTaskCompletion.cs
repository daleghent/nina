#region "copyright"

/*
    Copyright � 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace NINA.Core.Utility {

    public sealed class NotifyTaskCompletion<TResult> : INotifyPropertyChanged {

        public NotifyTaskCompletion(Task<TResult> task) {
            Task = task;
            if (!task.IsCompleted)
                TaskCompletion = WatchTaskAsync(task);
        }

        public Task TaskCompletion { get; private set; }

        private async Task WatchTaskAsync(Task task) {
            try {
                await task;
            } catch (Exception ex) {
                Logger.Error(ex);
            }
            var propertyChanged = PropertyChanged;
            if (propertyChanged == null)
                return;
            propertyChanged(this, new PropertyChangedEventArgs(nameof(Status)));
            propertyChanged(this, new PropertyChangedEventArgs(nameof(IsCompleted)));
            propertyChanged(this, new PropertyChangedEventArgs(nameof(IsNotCompleted)));
            if (task.IsCanceled) {
                propertyChanged(this, new PropertyChangedEventArgs(nameof(IsCanceled)));
            } else if (task.IsFaulted) {
                propertyChanged(this, new PropertyChangedEventArgs(nameof(IsFaulted)));
                propertyChanged(this, new PropertyChangedEventArgs(nameof(Exception)));
                propertyChanged(this,
                  new PropertyChangedEventArgs(nameof(InnerException)));
                propertyChanged(this, new PropertyChangedEventArgs(nameof(ErrorMessage)));
            } else {
                propertyChanged(this,
                  new PropertyChangedEventArgs(nameof(IsSuccessfullyCompleted)));
                propertyChanged(this, new PropertyChangedEventArgs(nameof(Result)));
            }
        }

        public Task<TResult> Task { get; private set; }

        public TResult Result => (Task.Status == TaskStatus.RanToCompletion) ?
Task.Result : default(TResult);

        public TaskStatus Status => Task.Status;
        public bool IsCompleted => Task.IsCompleted;
        public bool IsNotCompleted => !Task.IsCompleted;

        public bool IsSuccessfullyCompleted => Task.Status ==
TaskStatus.RanToCompletion;

        public bool IsCanceled => Task.IsCanceled;
        public bool IsFaulted => Task.IsFaulted;
        public AggregateException Exception => Task.Exception;

        public Exception InnerException => (Exception == null) ?
null : Exception.InnerException;

        public string ErrorMessage => (InnerException == null) ?
null : InnerException.Message;

        public event PropertyChangedEventHandler PropertyChanged;
    }
}