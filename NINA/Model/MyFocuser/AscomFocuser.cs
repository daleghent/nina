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

        public IAscomFocuserProvider FocuserProvider { get; set; } = new AscomFocuserProvider();

        public bool IsMoving {
            get {
                if (Connected) {
                    return instance.IsMoving;
                } else {
                    return false;
                }
            }
        }

        public int MaxIncrement {
            get {
                if (Connected) {
                    return Math.Abs(instance.MaxIncrement);
                } else {
                    return -1;
                }
            }
        }

        public int MaxStep {
            get {
                if (Connected) {
                    return Math.Abs(instance.MaxStep);
                } else {
                    return -1;
                }
            }
        }

        private bool _canGetPosition;

        public int Position {
            get {
                int pos = -1;
                try {
                    if (Connected && _canGetPosition) {
                        pos = Math.Abs(instance.Position);
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
            }
        }

        private bool _canGetStepSize;

        public double StepSize {
            get {
                double stepSize = double.NaN;
                try {
                    if (Connected && _canGetStepSize) {
                        stepSize = instance.StepSize;
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
                    return instance.TempCompAvailable;
                } else {
                    return false;
                }
            }
        }

        public bool TempComp {
            get {
                if (Connected && instance.TempCompAvailable) {
                    return instance.TempComp;
                } else {
                    return false;
                }
            }
            set {
                if (Connected && instance.TempCompAvailable) {
                    instance.TempComp = value;
                }
            }
        }

        private bool _hasTemperature;

        public double Temperature {
            get {
                double temperature = double.NaN;
                try {
                    if (Connected && _hasTemperature) {
                        temperature = instance.Temperature;
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

        public async Task Move(int position, CancellationToken ct, int waitInMs = 1000) {
            await instance.MoveAsync(position, ct, waitInMs);
        }

        private bool _canHalt;

        public void Halt() {
            if (Connected && _canHalt) {
                try {
                    instance.Halt();
                } catch (MethodNotImplementedException) {
                    _canHalt = false;
                } catch (Exception ex) {
                    Logger.Error(ex);
                }
            }
        }

        protected override string ConnectionLostMessage => Locale.Loc.Instance["LblFocuserConnectionLost"];

        private IFocuserV3Ex GetFocuser(bool connect) {
            return FocuserProvider.GetFocuser(Id, connect);
        }

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

        private IFocuserV3Ex instance;

        protected override Focuser GetInstance(string id) {
            instance = GetFocuser(true);
            return instance.GetASCOMInstance() as Focuser;
        }
    }
}