#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace NINA.Core.Utility.WindowService {

    /// <summary>
    /// A window should be associated to a viewmodel by the DataTemplates.xaml
    /// </summary>
    public class WindowService : IWindowService {
        protected Dispatcher dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        protected CustomWindow window;

        public void Show(object content, string title = "", ResizeMode resizeMode = ResizeMode.NoResize, WindowStyle windowStyle = WindowStyle.None) {
            dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => {
                window = new CustomWindow() {
                    SizeToContent = SizeToContent.WidthAndHeight,
                    Title = title,
                    Background = Application.Current.TryFindResource("BackgroundBrush") as Brush,
                    ResizeMode = resizeMode,
                    WindowStyle = windowStyle,
                    MinHeight = 300,
                    MinWidth = 350,
                    Style = Application.Current.TryFindResource("NoResizeWindow") as Style,
                };
                window.CloseCommand = new RelayCommand((object o) => window.Close());
                window.Closed += (object sender, EventArgs e) => this.OnClosed?.Invoke(this, null);
                window.ContentRendered += (object sender, EventArgs e) => window.InvalidateVisual();
                window.Content = content;
                window.Owner = Application.Current.MainWindow;
                window.Show();
            }));
        }

        public void DelayedClose(TimeSpan t) {
            Task.Run(async () => {
                await CoreUtil.Wait(t);
                await this.Close();
            });
        }

        public async Task Close() {
            await dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                window?.Close();
            }));
        }

        public IDispatcherOperationWrapper ShowDialog(object content, string title = "", ResizeMode resizeMode = ResizeMode.NoResize, WindowStyle windowStyle = WindowStyle.None, ICommand closeCommand = null) {
            return new DispatcherOperationWrapper(dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                window = new CustomWindow() {
                    SizeToContent = SizeToContent.WidthAndHeight,
                    Title = title,
                    Background = Application.Current.TryFindResource("BackgroundBrush") as Brush,
                    ResizeMode = resizeMode,
                    WindowStyle = windowStyle,
                    Style = Application.Current.TryFindResource("NoResizeWindow") as Style,
                };
                if (closeCommand == null) {
                    window.CloseCommand = new RelayCommand((object o) => window.Close());
                } else {
                    window.CloseCommand = closeCommand;
                }
                window.Closed += (object sender, EventArgs e) => this.OnClosed?.Invoke(this, null);
                window.ContentRendered += (object sender, EventArgs e) => window.InvalidateVisual();

                window.SizeChanged += Win_SizeChanged;
                window.Content = content;
                var mainwindow = System.Windows.Application.Current.MainWindow;
                mainwindow.Opacity = 0.8;
                window.Owner = Application.Current.MainWindow;
                var result = window.ShowDialog();
                this.OnDialogResultChanged?.Invoke(this, new DialogResultEventArgs(result));
                mainwindow.Opacity = 1;
            })));
        }

        public event EventHandler OnDialogResultChanged;

        public event EventHandler OnClosed;

        private static void Win_SizeChanged(object sender, SizeChangedEventArgs e) {
            var mainwindow = System.Windows.Application.Current.MainWindow;
            var win = (System.Windows.Window)sender;
            win.Left = mainwindow.Left + (mainwindow.Width - win.ActualWidth) / 2; ;
            win.Top = mainwindow.Top + (mainwindow.Height - win.ActualHeight) / 2;
        }
    }

    public interface IWindowService {

        void Show(object content, string title = "", ResizeMode resizeMode = ResizeMode.NoResize, WindowStyle windowStyle = WindowStyle.None);

        IDispatcherOperationWrapper ShowDialog(object content, string title = "", ResizeMode resizeMode = ResizeMode.NoResize, WindowStyle windowStyle = WindowStyle.None, ICommand closeCommand = null);

        event EventHandler OnDialogResultChanged;

        event EventHandler OnClosed;

        void DelayedClose(TimeSpan t);

        Task Close();
    }

    public interface IDispatcherOperationWrapper {
        Dispatcher Dispatcher { get; }
        DispatcherPriority Priority { get; set; }
        DispatcherOperationStatus Status { get; }
        Task Task { get; }
        object Result { get; }

        TaskAwaiter GetAwaiter();

        DispatcherOperationStatus Wait();

        DispatcherOperationStatus Wait(TimeSpan timeout);

        bool Abort();

        event EventHandler Aborted;

        event EventHandler Completed;
    }

    public class DispatcherOperationWrapper : IDispatcherOperationWrapper {
        private readonly DispatcherOperation op;

        public DispatcherOperationWrapper(DispatcherOperation operation) {
            op = operation;
        }

        public Dispatcher Dispatcher => op.Dispatcher;

        public DispatcherPriority Priority {
            get => op.Priority;
            set => op.Priority = value;
        }

        public DispatcherOperationStatus Status => op.Status;
        public Task Task => op.Task;

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public TaskAwaiter GetAwaiter() {
            return op.GetAwaiter();
        }

        public DispatcherOperationStatus Wait() {
            return op.Wait();
        }

        [SecurityCritical]
        public DispatcherOperationStatus Wait(TimeSpan timeout) {
            return op.Wait(timeout);
        }

        public bool Abort() {
            return op.Abort();
        }

        public object Result => op.Result;

        public event EventHandler Aborted {
            add => op.Aborted += value;
            remove => op.Aborted -= value;
        }

        public event EventHandler Completed {
            add => op.Completed += value;
            remove => op.Completed -= value;
        }
    }

    public class DialogResultEventArgs : EventArgs {

        public DialogResultEventArgs(bool? dialogResult) {
            DialogResult = dialogResult;
        }

        public bool? DialogResult { get; set; }
    }
}