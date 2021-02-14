using ASCOM;
using ASCOM.DriverAccess;
using NINA.Utility;
using NINA.Utility.Notification;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyDome {

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

    internal class AscomDome : BaseINPC, IDome, IDisposable {

        public AscomDome(string domeId, string domeName) {
            Id = domeId;
            Name = domeName;
        }

        private Dome dome;

        public bool HasSetupDialog => true;

        private string id;

        public string Id {
            get => id;
            set {
                id = value;
                RaisePropertyChanged();
            }
        }

        private string name;

        public string Name {
            get => name;
            set {
                name = value;
                RaisePropertyChanged();
            }
        }

        public string Category => "ASCOM";

        private bool connected;

        public bool Connected {
            get {
                if (connected) {
                    bool val = false;
                    try {
                        val = dome.Connected;
                        if (connected != val) {
                            Notification.ShowWarning(Locale.Loc.Instance["LblDomeConnectionLost"]);
                            Disconnect();
                        }
                    } catch (Exception) {
                        Disconnect();
                    }
                    return val;
                } else {
                    return false;
                }
            }
            private set {
                try {
                    dome.Connected = value;
                    connected = value;
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(Locale.Loc.Instance["LblDomeConnectionLost"] + Environment.NewLine + ex.Message);
                    connected = false;
                }
                RaisePropertyChanged();
            }
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

        public string Description => Connected ? dome?.Description ?? string.Empty : string.Empty;

        public string DriverInfo => Connected ? dome?.DriverInfo ?? string.Empty : string.Empty;

        public string DriverVersion => Connected ? dome?.DriverVersion ?? string.Empty : string.Empty;

        private bool canSlaveImplemented = true;
        public bool DriverCanFollow => TryGetProperty(() => dome.CanSlave, false, ref canSlaveImplemented);

        private bool canSetShutterImplemented = true;
        public bool CanSetShutter => TryGetProperty(() => dome.CanSetShutter, false, ref canSetShutterImplemented);

        private bool canSetParkImplemented = true;
        public bool CanSetPark => TryGetProperty(() => dome.CanSetPark, false, ref canSetParkImplemented);

        private bool canSetAzimuthImplemented = true;
        public bool CanSetAzimuth => TryGetProperty(() => dome.CanSetAzimuth, false, ref canSetAzimuthImplemented);

        private bool canParkImplemented = true;
        public bool CanPark => TryGetProperty(() => dome.CanPark, false, ref canParkImplemented);

        private bool canFindHomeImplemented = true;
        public bool CanFindHome => TryGetProperty(() => dome.CanFindHome, false, ref canFindHomeImplemented);

        private bool azimuthImplemented = true;
        public double Azimuth => TryGetProperty(() => dome.Azimuth, -1, ref azimuthImplemented);

        private bool atParkImplemented = true;
        public bool AtPark => TryGetProperty(() => dome.AtPark, false, ref atParkImplemented);

        private bool atHomeImplemented = true;
        public bool AtHome => TryGetProperty(() => dome.AtPark, false, ref atHomeImplemented);

        private bool slavedImplemented = true;
        public bool DriverFollowing {
            get {
                return TryGetProperty<bool>(() => dome.Slaved, false, ref slavedImplemented);
            }
            set {
                if (Connected) {
                    dome.Slaved = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool slewingImplemented = true;
        public bool Slewing => TryGetProperty(() => dome.Slewing, false, ref slewingImplemented);

        private bool shutterStatusImplemented = true;

        public ShutterState ShutterStatus => TryGetProperty(() => dome.ShutterStatus.FromASCOM(), ShutterState.ShutterNone, ref shutterStatusImplemented);

        public bool CanSyncAzimuth => Connected && dome.CanSyncAzimuth;

        public async Task<bool> Connect(CancellationToken token) {
            return await Task.Run(() => {
                try {
                    dome = new Dome(Id);
                    Connected = true;
                    if (Connected) {
                        Init();
                        RaiseAllPropertiesChanged();
                    }
                } catch (DriverAccessCOMException ex) {
                    Utility.Utility.HandleAscomCOMException(ex);
                } catch (System.Runtime.InteropServices.COMException ex) {
                    Utility.Utility.HandleAscomCOMException(ex);
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(Locale.Loc.Instance["LblDomeASCOMConnectFailed"] + ex.Message);
                }
                return Connected;
            });
        }

        public void Dispose() {
            dome?.Dispose();
        }

        public void Disconnect() {
            Connected = false;
            dome?.Dispose();
            dome = null;
        }

        public void SetupDialog() {
            if (HasSetupDialog) {
                try {
                    bool dispose = false;
                    if (dome == null) {
                        dome = new Dome(Id);
                        dispose = true;
                    }
                    dome.SetupDialog();
                    if (dispose) {
                        dome.Dispose();
                        dome = null;
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
                }
            }
        }

        private void Init() {
        }

        public async Task SlewToAzimuth(double azimuth, CancellationToken ct) {
            if (Connected) {
                if (CanSetAzimuth) {
                    ct.Register(StopSlewing);
                    await Task.Run(async () => {
                        dome?.SlewToAzimuth(azimuth);
                        while (dome != null && dome.Slewing && !ct.IsCancellationRequested) {
                            await Task.Delay(1000, ct);
                        }
                    }, ct);
                } else {
                    Notification.ShowWarning(Locale.Loc.Instance["LblDomeCannotSlew"]);
                }
            } else {
                Notification.ShowWarning(Locale.Loc.Instance["LblDomeNotConnected"]);
            }
        }

        public void StopSlewing() {
            if (Connected) {
                // ASCOM only allows you to stop all movement, which includes both shutter and slewing. If the shutter was opening or closing
                // when this command is received, try and continue the operation afterwards
                Task.Run(() => {
                    var priorShutterStatus = ShutterStatus;
                    dome?.AbortSlew();
                    if (priorShutterStatus == ShutterState.ShutterClosing) {
                        dome?.CloseShutter();
                    } else if (priorShutterStatus == ShutterState.ShutterOpening) {
                        dome?.OpenShutter();
                    }
                });
            } else {
                Notification.ShowWarning(Locale.Loc.Instance["LblDomeNotConnected"]);
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
                Task.Run(() => dome?.AbortSlew());
            } else {
                Notification.ShowWarning(Locale.Loc.Instance["LblDomeNotConnected"]);
            }
        }

        public async Task OpenShutter(CancellationToken ct) {
            if (Connected) {
                if (CanSetShutter) {
                    ct.Register(() => dome?.AbortSlew());
                    if (ShutterStatus == ShutterState.ShutterError) {
                        // If shutter is in the error state, you must close it before re-opening
                        await CloseShutter(ct);
                    }
                    await Task.Run(() => dome?.OpenShutter(), ct);
                    while (dome != null && ShutterStatus == ShutterState.ShutterOpening && !ct.IsCancellationRequested) {
                        await Task.Delay(1000, ct);
                    };
                } else {
                    Notification.ShowWarning(Locale.Loc.Instance["LblDomeCannotSetShutter"]);
                }
            } else {
                Notification.ShowWarning(Locale.Loc.Instance["LblDomeNotConnected"]);
            }
        }

        public async Task CloseShutter(CancellationToken ct) {
            if (Connected) {
                if (CanSetShutter) {
                    ct.Register(() => dome?.AbortSlew());
                    await Task.Run(() => dome?.CloseShutter(), ct);
                    while (dome != null && ShutterStatus == ShutterState.ShutterClosing && !ct.IsCancellationRequested) {
                        await Task.Delay(1000, ct);
                    };
                } else {
                    Notification.ShowWarning(Locale.Loc.Instance["LblDomeCannotSetShutter"]);
                }
            } else {
                Notification.ShowWarning(Locale.Loc.Instance["LblDomeNotConnected"]);
            }
        }

        public async Task FindHome(CancellationToken ct) {
            if (Connected) {
                if (CanFindHome) {
                    // ASCOM domes make no promise that a slew operation can take place if one is already in progress, so we do a hard abort up front to ensure FindHome works
                    dome?.AbortSlew();
                    await Task.Delay(1000, ct);

                    ct.Register(() => dome.AbortSlew());
                    await Task.Run(() => dome.FindHome(), ct);

                    // Introduce an initial delay to give the dome a change to start slewing before we wait for it to complete
                    await Task.Delay(1000, ct);
                    while (dome != null && dome.Slewing && !ct.IsCancellationRequested) {
                        await Task.Delay(1000, ct);
                    }
                    // Introduce a final delay, in case the Dome driver settles after finding the home position by backtracking
                    await Task.Delay(2000, ct);
                } else {
                    Notification.ShowWarning(Locale.Loc.Instance["LblDomeCannotFindHome"]);
                }
            } else {
                Notification.ShowWarning(Locale.Loc.Instance["LblDomeNotConnected"]);
            }
        }

        public async Task Park(CancellationToken ct) {
            if (Connected) {
                if (CanPark) {
                    // ASCOM domes make no promise that a slew operation can take place if one is already in progress, so we do a hard abort up front to ensure Park works
                    dome?.AbortSlew();
                    await Task.Delay(1000, ct);

                    ct.Register(() => dome?.AbortSlew());
                    await Task.Run(() => dome?.Park(), ct);
                    if (CanSetShutter) {
                        await Task.Run(() => dome?.CloseShutter(), ct);
                    }
                    while (dome != null && dome.Slewing && !ct.IsCancellationRequested) {
                       await Task.Delay(1000, ct);
                    }
                } else {
                    Notification.ShowWarning(Locale.Loc.Instance["LblDomeCannotPark"]);
                }
            } else {
                Notification.ShowWarning(Locale.Loc.Instance["LblDomeNotConnected"]);
            }
        }

        public void SetPark() {
            if (Connected) {
                if (CanSetPark) {
                    dome.SetPark();
                } else {
                    Notification.ShowWarning(Locale.Loc.Instance["LblDomeCannotSetPark"]);
                }
            } else {
                Notification.ShowWarning(Locale.Loc.Instance["LblDomeNotConnected"]);
            }
        }

        public void SyncToAzimuth(double azimuth) {
            if (Connected) {
                if (CanSyncAzimuth) {
                    dome.SyncToAzimuth(azimuth);
                } else {
                    Notification.ShowWarning(Locale.Loc.Instance["LblDomeCannotSyncAzimuth"]);
                }
            } else {
                Notification.ShowWarning(Locale.Loc.Instance["LblDomeNotConnected"]);
            }
        }
    }
}