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
using ASCOM.DriverAccess;
using NINA.Utility;
using NINA.Utility.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyFlatDevice {

    public class AscomCoverCalibrator : BaseINPC, IFlatDevice, IDisposable {

        public AscomCoverCalibrator(string id, string name) {
            this.Id = id;
            this.Name = name;
        }

        private CoverCalibrator coverCalibrator;
        private int lastBrightness = 0;

        public string Category { get; } = "ASCOM";

        public bool HasSetupDialog {
            get {
                return true;
            }
        }

        public string Id { get; }

        public string Name { get; }

        private bool _connected;

        public bool Connected {
            get {
                if (_connected) {
                    bool val = false;
                    try {
                        val = coverCalibrator.Connected;
                        if (_connected != val) {
                            Notification.ShowWarning(Locale.Loc.Instance["LblFlatDeviceConnectionLost"]);
                            Disconnect();
                        }
                    } catch (Exception ex) {
                        Logger.Error(ex);
                        Notification.ShowWarning(Locale.Loc.Instance["LblFlatDeviceConnectionLost"]);
                        try {
                            Disconnect();
                        } catch (Exception disconnectEx) {
                            Logger.Error(disconnectEx);
                        }
                    }
                    return val;
                } else {
                    return false;
                }
            }
            private set {
                try {
                    coverCalibrator.Connected = value;
                    _connected = value;
                } catch (Exception ex) {
                    Logger.Error(ex);
                    _connected = false;
                }
            }
        }

        public string Description {
            get {
                return coverCalibrator.Description;
            }
        }

        public string DriverInfo {
            get {
                return coverCalibrator.DriverInfo;
            }
        }

        public string DriverVersion {
            get {
                return coverCalibrator.DriverVersion;
            }
        }

        public CoverState CoverState {
            get {
                var state = coverCalibrator.CoverState;
                switch (state) {
                    case ASCOM.DeviceInterface.CoverStatus.Unknown:
                        return CoverState.Unknown;

                    case ASCOM.DeviceInterface.CoverStatus.NotPresent:
                        return CoverState.Unknown;

                    case ASCOM.DeviceInterface.CoverStatus.Moving:
                        return CoverState.NeitherOpenNorClosed;

                    case ASCOM.DeviceInterface.CoverStatus.Closed:
                        return CoverState.Closed;

                    case ASCOM.DeviceInterface.CoverStatus.Open:
                        return CoverState.Open;

                    case ASCOM.DeviceInterface.CoverStatus.Error:
                        return CoverState.Unknown;

                    default:
                        return CoverState.Unknown;
                }
            }
        }

        public int MaxBrightness { get; private set; }

        public int MinBrightness { get; private set; }

        public bool LightOn {
            get => coverCalibrator.Brightness > 0;
            set {
                try {
                    if (value) {
                        Logger.Debug("Switching cover calibrator on");
                        // switch the light on with the last saved value, if any
                        coverCalibrator.CalibratorOn((lastBrightness != 0)?lastBrightness:MaxBrightness);
                    } else {
                        Logger.Debug("Switching cover calibrator off");
                        coverCalibrator.CalibratorOff();
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }
            }
        }

        public double Brightness {
            get => (double)coverCalibrator.Brightness / MaxBrightness;
            set {
                try {
                    var converted = (int)(value * MaxBrightness);
                    Logger.Debug($"Setting cover calibrator brightness to {value}% = {converted}");
                    coverCalibrator.CalibratorOn(converted);
                    lastBrightness = converted; // save brightness for next time the user toggles the light on
                } catch (Exception ex) {
                    Logger.Error(ex);
                }
            }
        }

        public string PortName { get => string.Empty; set { } }

        public bool SupportsOpenClose => coverCalibrator.CoverState != ASCOM.DeviceInterface.CoverStatus.NotPresent;

        public async Task<bool> Connect(CancellationToken token) {
            return await Task<bool>.Run(() => {
                try {
                    coverCalibrator = new CoverCalibrator(Id);
                    Connected = true;
                    Initialize();
                    if (Connected) {
                        RaiseAllPropertiesChanged();
                    }
                } catch (ASCOM.DriverAccessCOMException ex) {
                    Utility.Utility.HandleAscomCOMException(ex);
                } catch (System.Runtime.InteropServices.COMException ex) {
                    Utility.Utility.HandleAscomCOMException(ex);
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError("Unable to connect to flat device " + ex.Message);
                }
                return Connected;
            });
        }

        private void Initialize() {
            if (coverCalibrator.CalibratorState == ASCOM.DeviceInterface.CalibratorStatus.NotPresent) {
                MinBrightness = 1;
                MaxBrightness = 1;
            } else {
                try {
                    MinBrightness = 0;
                    MaxBrightness = coverCalibrator.MaxBrightness;
                } catch (PropertyNotImplementedException) {
                    MinBrightness = 1;
                    MaxBrightness = 1;
                }
            }
        }

        public void Disconnect() {
            Connected = false;
            Dispose();
        }

        public void Dispose() {
            coverCalibrator?.Dispose();
            coverCalibrator = null;
        }

        public void SetupDialog() {
            if (HasSetupDialog) {
                try {
                    bool dispose = false;
                    if (coverCalibrator == null) {
                        coverCalibrator = new CoverCalibrator(Id);
                        dispose = true;
                    }
                    coverCalibrator.SetupDialog();
                    if (dispose) {
                        coverCalibrator.Dispose();
                        coverCalibrator = null;
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
                }
            }
        }

        public Task<bool> Open(CancellationToken ct, int delay = 300) {
            if (SupportsOpenClose) {
                coverCalibrator.OpenCover();
            }
            return Task.FromResult(true);
        }

        public Task<bool> Close(CancellationToken ct, int delay = 300) {
            if (SupportsOpenClose) {
                coverCalibrator.CloseCover();
            }
            return Task.FromResult(true);
        }
    }
}