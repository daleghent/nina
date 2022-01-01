#region "copyright"

/*
    Copyright © 2016 - 2022 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using ASCOM;
using ASCOM.DriverAccess;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Equipment.ASCOMFacades;
using NINA.Equipment.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MyFlatDevice {

    public class AscomCoverCalibrator : AscomDevice<CoverCalibrator, ICoverCalibratorFacade, CoverCalibratorFacadeProxy>, IFlatDevice, IDisposable {

        public AscomCoverCalibrator(string id, string name, IDeviceDispatcher deviceDispatcher) : base(id, name, deviceDispatcher, DeviceDispatcherType.FlatDevice) {
        }

        private int lastBrightness = 0;

        public CoverState CoverState {
            get {
                var state = device.CoverState;
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
            get => SupportsOnOff ? GetProperty<int>(nameof(Brightness), 0) > 0 : false;
            set {
                try {
                    if (SupportsOnOff) {
                        if (value) {
                            Logger.Debug("Switching cover calibrator on");
                            // switch the light on with the last saved value, if any
                            device.CalibratorOn((lastBrightness != 0) ? lastBrightness : MaxBrightness);
                        } else {
                            Logger.Debug("Switching cover calibrator off");
                            device.CalibratorOff();
                        }
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }
            }
        }

        public int Brightness {
            get => SupportsOnOff ? GetProperty<int>(nameof(Brightness), 0) : 0;
            set {
                try {
                    if (SupportsOnOff) {
                        Logger.Debug($"Setting cover calibrator brightness to {value}");
                        device.CalibratorOn(value);
                        lastBrightness = value; // save brightness for next time the user toggles the light on
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }
            }
        }

        public string PortName { get => string.Empty; set { } }

        public bool SupportsOpenClose => device.CoverState != ASCOM.DeviceInterface.CoverStatus.NotPresent;

        public bool SupportsOnOff => device.CalibratorState != ASCOM.DeviceInterface.CalibratorStatus.NotPresent;

        protected override string ConnectionLostMessage => Loc.Instance["LblFlatDeviceConnectionLost"];

        private void Initialize() {
            if (device.CalibratorState == ASCOM.DeviceInterface.CalibratorStatus.NotPresent) {
                MinBrightness = 0;
                MaxBrightness = 0;
            } else {
                try {
                    MinBrightness = 0;
                    MaxBrightness = device.MaxBrightness;
                } catch (PropertyNotImplementedException) {
                    MinBrightness = 0;
                    MaxBrightness = 0;
                }
            }
        }

        public async Task<bool> Open(CancellationToken ct, int delay = 300) {
            if (SupportsOpenClose) {
                device.OpenCover();
                while (CoverState != CoverState.Unknown && CoverState == CoverState.NeitherOpenNorClosed) {
                    await Task.Delay(delay);
                }
            }
            return CoverState == CoverState.Open;
        }

        public async Task<bool> Close(CancellationToken ct, int delay = 300) {
            if (SupportsOpenClose) {
                device.CloseCover();
                while (CoverState != CoverState.Unknown && CoverState == CoverState.NeitherOpenNorClosed) {
                    await Task.Delay(delay);
                }
            }
            return CoverState == CoverState.Closed;
        }

        protected override Task PreConnect() {
            lastBrightness = 0;
            return base.PreConnect();
        }

        protected override Task PostConnect() {
            Initialize();
            return Task.CompletedTask;
        }

        protected override CoverCalibrator GetInstance(string id) {
            return DeviceDispatcher.Invoke(DeviceDispatcherType, () => new CoverCalibrator(id));
        }
    }
}