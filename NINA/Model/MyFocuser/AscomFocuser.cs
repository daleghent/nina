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
using NINA.Utility;
using NINA.Utility.Notification;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyFocuser {

    internal class AscomFocuser : AscomDevice<Focuser>, IFocuser, IDisposable {

        public AscomFocuser(string focuser, string name) : base(focuser, name) {
        }

        public Focuser Device => device;

        public virtual bool Absolute {
            get {
                if (Connected) {
                    return device.Absolute;
                }
                return true;
            }
        }

        public bool IsMoving {
            get {
                if (Connected) {
                    return device.IsMoving;
                } else {
                    return false;
                }
            }
        }

        public int MaxIncrement {
            get {
                if (Connected) {
                    return Math.Abs(device.MaxIncrement);
                } else {
                    return -1;
                }
            }
        }

        public int MaxStep {
            get {
                if (Connected) {
                    return Math.Abs(device.MaxStep);
                } else {
                    return -1;
                }
            }
        }

        private bool _canGetPosition;

        //Used for relative focusers
        private int internalPosition = 5000;

        public int Position {
            get {
                if (Absolute) {
                    int pos = -1;
                    try {
                        if (Connected && _canGetPosition) {
                            pos = Math.Abs(device.Position);
                        }
                    } catch (PropertyNotImplementedException) {
                        _canGetPosition = false;
                    } catch (System.NotImplementedException) {
                        _canGetPosition = false;
                    } catch (DriverException ex) {
                        Logger.Error(ex);
                    } catch (Exception ex) {
                        Logger.Error(ex);
                    }
                    return pos;
                } else {
                    return internalPosition;
                }
            }
        }

        private bool _canGetStepSize;

        public double StepSize {
            get {
                double stepSize = double.NaN;
                try {
                    if (Connected && _canGetStepSize) {
                        stepSize = device.StepSize;
                    }
                } catch (PropertyNotImplementedException) {
                    _canGetStepSize = false;
                } catch (System.NotImplementedException) {
                    _canGetStepSize = false;
                } catch (DriverException ex) {
                    Logger.Error(ex);
                } catch (Exception ex) {
                    Logger.Error(ex);
                }
                return stepSize;
            }
        }

        public bool TempCompAvailable {
            get {
                if (Connected) {
                    return device.TempCompAvailable;
                } else {
                    return false;
                }
            }
        }

        public bool TempComp {
            get {
                if (Connected && device.TempCompAvailable) {
                    return device.TempComp;
                } else {
                    return false;
                }
            }
            set {
                if (Connected && device.TempCompAvailable) {
                    device.TempComp = value;
                }
            }
        }

        private bool _hasTemperature;

        public double Temperature {
            get {
                double temperature = double.NaN;
                try {
                    if (Connected && _hasTemperature) {
                        temperature = device.Temperature;
                    }
                } catch (PropertyNotImplementedException) {
                    _hasTemperature = false;
                } catch (System.NotImplementedException) {
                    _hasTemperature = false;
                } catch (DriverException ex) {
                    Logger.Error(ex);
                } catch (Exception ex) {
                    Logger.Error(ex);
                }
                return temperature;
            }
        }

        public Task Move(int position, CancellationToken ct, int waitInMs = 1000) {
            if (Absolute) {
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

                while (position != device.Position) {
                    device.Move(position);
                    while (IsMoving) {
                        await Utility.Utility.Wait(TimeSpan.FromMilliseconds(waitInMs), ct);
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
                while (relativeOffsetRemaining != 0) {
                    var moveAmount = Math.Min(MaxStep, Math.Abs(relativeOffsetRemaining));
                    if (relativeOffsetRemaining < 0) {
                        moveAmount *= -1;
                    }
                    device.Move(moveAmount);
                    while (IsMoving) {
                        await Utility.Utility.Wait(TimeSpan.FromMilliseconds(waitInMs), ct);
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

        protected override string ConnectionLostMessage => Locale.Loc.Instance["LblFocuserConnectionLost"];

        private void Initialize() {
            _canGetPosition = true;
            _canGetStepSize = true;
            _hasTemperature = true;
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