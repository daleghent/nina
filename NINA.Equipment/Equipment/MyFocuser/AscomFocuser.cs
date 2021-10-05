#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using ASCOM;
using ASCOM.DeviceInterface;
using ASCOM.DriverAccess;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MyFocuser {

    internal class AscomFocuser : AscomDevice<Focuser>, IFocuser, IDisposable {

        public AscomFocuser(string focuser, string name) : base(focuser, name) {
        }

        public Focuser Device => device;

        public bool IsMoving {
            get {
                return GetProperty(nameof(Focuser.IsMoving), false);
            }
        }

        public int MaxIncrement {
            get {
                return GetProperty(nameof(Focuser.MaxIncrement), -1);
            }
        }

        public int MaxStep {
            get {
                return GetProperty(nameof(Focuser.MaxStep), -1);
            }
        }

        private bool _isAbsolute;

        //Used for relative focusers
        private int internalPosition;

        public int Position {
            get {
                if (_isAbsolute) {
                    return GetProperty(nameof(Focuser.Position), -1); ;
                } else {
                    return internalPosition;
                }
            }
        }

        public double StepSize {
            get {
                return GetProperty(nameof(Focuser.StepSize), double.NaN);
            }
        }

        public bool TempCompAvailable {
            get {
                return GetProperty(nameof(Focuser.TempCompAvailable), false);
            }
        }

        public bool TempComp {
            get {
                if (TempCompAvailable) {
                    return GetProperty(nameof(Focuser.TempComp), false);
                } else {
                    return false;
                }
            }
            set {
                if (Connected && TempCompAvailable) {
                    SetProperty(nameof(Focuser.TempComp), value);
                }
            }
        }

        public double Temperature {
            get {
                return GetProperty(nameof(Focuser.Temperature), double.NaN);
            }
        }

        public Task Move(int position, CancellationToken ct, int waitInMs = 1000) {
            if (_isAbsolute) {
                return MoveInternalAbsolute(position, ct, waitInMs);
            } else {
                return MoveInternalRelative(position, ct, waitInMs);
            }
        }

        private async Task MoveInternalAbsolute(int position, CancellationToken ct, int waitInMs = 1000) {
            if (Connected) {
                var reEnableTempComp = TempComp;
                if (reEnableTempComp) {
                    TempComp = false;
                }

                while (position != device.Position && !ct.IsCancellationRequested) {
                    device.Move(position);
                    while (IsMoving && !ct.IsCancellationRequested) {
                        await CoreUtil.Wait(TimeSpan.FromMilliseconds(waitInMs), ct);
                    }
                }

                if (reEnableTempComp) {
                    TempComp = true;
                }
            }
        }

        private async Task MoveInternalRelative(int position, CancellationToken ct, int waitInMs = 1000) {
            if (Connected) {
                var reEnableTempComp = TempComp;
                if (reEnableTempComp) {
                    TempComp = false;
                }

                var relativeOffsetRemaining = position - this.Position;
                while (relativeOffsetRemaining != 0 && !ct.IsCancellationRequested) {
                    var moveAmount = Math.Min(MaxStep, Math.Abs(relativeOffsetRemaining));
                    if (relativeOffsetRemaining < 0) {
                        moveAmount *= -1;
                    }
                    device.Move(moveAmount);
                    while (IsMoving && !ct.IsCancellationRequested) {
                        await CoreUtil.Wait(TimeSpan.FromMilliseconds(waitInMs), ct);
                    }
                    relativeOffsetRemaining -= moveAmount;
                    internalPosition += moveAmount;
                }

                if (reEnableTempComp) {
                    TempComp = true;
                }
            }
        }

        private bool _canHalt;

        public void Halt() {
            if (Connected && _canHalt) {
                try {
                    device.Halt();
                } catch (MethodNotImplementedException) {
                    _canHalt = false;
                } catch (Exception ex) {
                    Logger.Error(ex);
                }
            }
        }

        protected override string ConnectionLostMessage => Loc.Instance["LblFocuserConnectionLost"];

        private void Initialize() {
            internalPosition = device.MaxStep / 2;
            _isAbsolute = device.Absolute;
            if (!_isAbsolute) { Logger.Info("The focuser is a relative focuser. Simulating absoute focuser behavior"); }
            _canHalt = true;
        }

        protected override Task PostConnect() {
            Initialize();
            return Task.CompletedTask;
        }

        protected override Focuser GetInstance(string id) {
            return new Focuser(id);
        }
    }
}