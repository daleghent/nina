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
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MyDome {

    public static class ShutterStateExtensions {

        public static ShutterState FromASCOM(this ASCOM.DeviceInterface.ShutterState shutterState) {
            switch (shutterState) {
                case ASCOM.DeviceInterface.ShutterState.shutterOpen:
                    return ShutterState.ShutterOpen;

                case ASCOM.DeviceInterface.ShutterState.shutterClosed:
                    return ShutterState.ShutterClosed;

                case ASCOM.DeviceInterface.ShutterState.shutterOpening:
                    return ShutterState.ShutterOpening;

                case ASCOM.DeviceInterface.ShutterState.shutterClosing:
                    return ShutterState.ShutterClosing;

                case ASCOM.DeviceInterface.ShutterState.shutterError:
                    return ShutterState.ShutterError;
            }
            throw new ArgumentOutOfRangeException($"{shutterState} is not an expected value");
        }

        public static ASCOM.DeviceInterface.ShutterState ToASCOM(this ShutterState shutterState) {
            switch (shutterState) {
                case ShutterState.ShutterOpen:
                    return ASCOM.DeviceInterface.ShutterState.shutterOpen;

                case ShutterState.ShutterClosed:
                    return ASCOM.DeviceInterface.ShutterState.shutterClosed;

                case ShutterState.ShutterOpening:
                    return ASCOM.DeviceInterface.ShutterState.shutterOpening;

                case ShutterState.ShutterClosing:
                    return ASCOM.DeviceInterface.ShutterState.shutterClosing;

                case ShutterState.ShutterError:
                    return ASCOM.DeviceInterface.ShutterState.shutterError;

                case ShutterState.ShutterNone:
                    return ASCOM.DeviceInterface.ShutterState.shutterError;
            }
            throw new ArgumentOutOfRangeException($"{shutterState} is not an expected value");
        }

        public static bool CanOpen(this ShutterState shutterState) {
            return !(shutterState == ShutterState.ShutterOpen || shutterState == ShutterState.ShutterOpening);
        }

        public static bool CanClose(this ShutterState shutterState) {
            return !(shutterState == ShutterState.ShutterClosed || shutterState == ShutterState.ShutterClosing);
        }
    }

    internal class AscomDome : AscomDevice<Dome>, IDome, IDisposable {

        public AscomDome(string domeId, string domeName) : base(domeId, domeName) {
        }

        private T TryGetProperty<T>(Func<T> supplier, T defaultT, ref bool isImplemented) {
            try {
                if (isImplemented && Connected) {
                    return supplier();
                } else {
                    return defaultT;
                }
            } catch (PropertyNotImplementedException) {
                isImplemented = false;
                return defaultT;
            } catch (Exception) {
                return defaultT;
            }
        }

        private bool canSlaveImplemented = true;
        public bool DriverCanFollow => TryGetProperty(() => device.CanSlave, false, ref canSlaveImplemented);

        private bool canSetShutterImplemented = true;
        public bool CanSetShutter => TryGetProperty(() => device.CanSetShutter, false, ref canSetShutterImplemented);

        private bool canSetParkImplemented = true;
        public bool CanSetPark => TryGetProperty(() => device.CanSetPark, false, ref canSetParkImplemented);

        private bool canSetAzimuthImplemented = true;
        public bool CanSetAzimuth => TryGetProperty(() => device.CanSetAzimuth, false, ref canSetAzimuthImplemented);

        private bool canParkImplemented = true;
        public bool CanPark => TryGetProperty(() => device.CanPark, false, ref canParkImplemented);

        private bool canFindHomeImplemented = true;
        public bool CanFindHome => TryGetProperty(() => device.CanFindHome, false, ref canFindHomeImplemented);

        private bool azimuthImplemented = true;
        public double Azimuth => TryGetProperty(() => device.Azimuth, -1, ref azimuthImplemented);

        private bool atParkImplemented = true;
        public bool AtPark => TryGetProperty(() => device.AtPark, false, ref atParkImplemented);

        private bool atHomeImplemented = true;
        public bool AtHome => TryGetProperty(() => device.AtPark, false, ref atHomeImplemented);

        private bool slavedImplemented = true;

        public bool DriverFollowing {
            get {
                return TryGetProperty<bool>(() => device.Slaved, false, ref slavedImplemented);
            }
            set {
                if (Connected) {
                    device.Slaved = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool slewingImplemented = true;
        public bool Slewing => TryGetProperty(() => device.Slewing, false, ref slewingImplemented);

        private bool shutterStatusImplemented = true;

        public ShutterState ShutterStatus => TryGetProperty(() => device.ShutterStatus.FromASCOM(), ShutterState.ShutterNone, ref shutterStatusImplemented);

        public bool CanSyncAzimuth => Connected && device.CanSyncAzimuth;

        protected override string ConnectionLostMessage => Loc.Instance["LblDomeConnectionLost"];

        private void Init() {
        }

        public async Task SlewToAzimuth(double azimuth, CancellationToken ct) {
            if (Connected) {
                if (CanSetAzimuth) {
                    ct.Register(StopSlewing);
                    await Task.Run(async () => {
                        device?.SlewToAzimuth(azimuth);
                        while (device != null && device.Slewing && !ct.IsCancellationRequested) {
                            await Task.Delay(1000, ct);
                        }
                    }, ct);
                } else {
                    Notification.ShowWarning(Loc.Instance["LblDomeCannotSlew"]);
                }
            } else {
                Notification.ShowWarning(Loc.Instance["LblDomeNotConnected"]);
            }
        }

        public void StopSlewing() {
            if (Connected) {
                // ASCOM only allows you to stop all movement, which includes both shutter and slewing. If the shutter was opening or closing
                // when this command is received, try and continue the operation afterwards
                Task.Run(() => {
                    var priorShutterStatus = ShutterStatus;
                    device?.AbortSlew();
                    if (priorShutterStatus == ShutterState.ShutterClosing) {
                        device?.CloseShutter();
                    } else if (priorShutterStatus == ShutterState.ShutterOpening) {
                        device?.OpenShutter();
                    }
                });
            } else {
                Notification.ShowWarning(Loc.Instance["LblDomeNotConnected"]);
            }
        }

        public void StopShutter() {
            // ASCOM only allows you to stop both slew and shutter movement together. We also don't have a way of determining whether a
            // slew is in progress or what the target azimuth is, so we can't recover for a StopShutter operation
            StopAll();
        }

        public void StopAll() {
            if (Connected) {
                // Fire and forget
                Task.Run(() => device?.AbortSlew());
            } else {
                Notification.ShowWarning(Loc.Instance["LblDomeNotConnected"]);
            }
        }

        public async Task OpenShutter(CancellationToken ct) {
            if (Connected) {
                if (CanSetShutter) {
                    ct.Register(() => device?.AbortSlew());
                    if (ShutterStatus == ShutterState.ShutterError) {
                        // If shutter is in the error state, you must close it before re-opening
                        await CloseShutter(ct);
                    }
                    await Task.Run(() => device?.OpenShutter(), ct);
                    while (device != null && ShutterStatus == ShutterState.ShutterOpening && !ct.IsCancellationRequested) {
                        await Task.Delay(1000, ct);
                    };
                } else {
                    Notification.ShowWarning(Loc.Instance["LblDomeCannotSetShutter"]);
                }
            } else {
                Notification.ShowWarning(Loc.Instance["LblDomeNotConnected"]);
            }
        }

        public async Task CloseShutter(CancellationToken ct) {
            if (Connected) {
                if (CanSetShutter) {
                    ct.Register(() => device?.AbortSlew());
                    await Task.Run(() => device?.CloseShutter(), ct);
                    while (device != null && ShutterStatus == ShutterState.ShutterClosing && !ct.IsCancellationRequested) {
                        await Task.Delay(1000, ct);
                    };
                } else {
                    Notification.ShowWarning(Loc.Instance["LblDomeCannotSetShutter"]);
                }
            } else {
                Notification.ShowWarning(Loc.Instance["LblDomeNotConnected"]);
            }
        }

        public async Task FindHome(CancellationToken ct) {
            if (Connected) {
                if (CanFindHome) {
                    // ASCOM domes make no promise that a slew operation can take place if one is already in progress, so we do a hard abort up front to ensure FindHome works
                    device?.AbortSlew();
                    await Task.Delay(1000, ct);

                    ct.Register(() => device.AbortSlew());
                    await Task.Run(() => device.FindHome(), ct);

                    // Introduce an initial delay to give the dome a change to start slewing before we wait for it to complete
                    await Task.Delay(1000, ct);
                    while (device != null && device.Slewing && !ct.IsCancellationRequested) {
                        await Task.Delay(1000, ct);
                    }
                    // Introduce a final delay, in case the Dome driver settles after finding the home position by backtracking
                    await Task.Delay(2000, ct);
                } else {
                    Notification.ShowWarning(Loc.Instance["LblDomeCannotFindHome"]);
                }
            } else {
                Notification.ShowWarning(Loc.Instance["LblDomeNotConnected"]);
            }
        }

        public async Task Park(CancellationToken ct) {
            if (Connected) {
                if (CanPark) {
                    // ASCOM domes make no promise that a slew operation can take place if one is already in progress, so we do a hard abort up front to ensure Park works
                    device?.AbortSlew();
                    await Task.Delay(1000, ct);

                    ct.Register(() => device?.AbortSlew());
                    await Task.Run(() => device?.Park(), ct);
                    if (CanSetShutter) {
                        await Task.Run(() => device?.CloseShutter(), ct);
                    }
                    while (device != null && device.Slewing && !ct.IsCancellationRequested) {
                        await Task.Delay(1000, ct);
                    }
                } else {
                    Notification.ShowWarning(Loc.Instance["LblDomeCannotPark"]);
                }
            } else {
                Notification.ShowWarning(Loc.Instance["LblDomeNotConnected"]);
            }
        }

        public void SetPark() {
            if (Connected) {
                if (CanSetPark) {
                    device.SetPark();
                } else {
                    Notification.ShowWarning(Loc.Instance["LblDomeCannotSetPark"]);
                }
            } else {
                Notification.ShowWarning(Loc.Instance["LblDomeNotConnected"]);
            }
        }

        public void SyncToAzimuth(double azimuth) {
            if (Connected) {
                if (CanSyncAzimuth) {
                    device.SyncToAzimuth(azimuth);
                } else {
                    Notification.ShowWarning(Loc.Instance["LblDomeCannotSyncAzimuth"]);
                }
            } else {
                Notification.ShowWarning(Loc.Instance["LblDomeNotConnected"]);
            }
        }

        protected override Task PostConnect() {
            Init();
            return Task.CompletedTask;
        }

        protected override Dome GetInstance(string id) {
            return new Dome(id);
        }
    }
}