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

        private Dome _dome;

        public bool HasSetupDialog => true;

        private string _id;
        public string Id {
            get => _id;
            set {
                _id = value;
                RaisePropertyChanged();
            }
        }

        private string _name;
        public string Name {
            get => _name;
            set {
                _name = value;
                RaisePropertyChanged();
            }
        }

        public string Category => "ASCOM";

        private bool _connected;

        public bool Connected {
            get {
                if (_connected) {
                    bool val = false;
                    try {
                        val = _dome.Connected;
                        if (_connected != val) {
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
                    _dome.Connected = value;
                    _connected = value;
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(Locale.Loc.Instance["LblDomeReconnect"] + Environment.NewLine + ex.Message);
                    _connected = false;
                }
                RaisePropertyChanged();
            }
        }

        private T TryGetProperty<T>(Func<T> supplier, T defaultT) {
            try {
                if (Connected) {
                    return supplier();
                } else {
                    return defaultT;
                }
            } catch (Exception) {
                return defaultT;
            }
        }

        public string Description => Connected ? _dome?.Description ?? string.Empty : string.Empty;

        public string DriverInfo => Connected ? _dome?.DriverInfo ?? string.Empty : string.Empty;

        public string DriverVersion => Connected ? _dome?.DriverVersion ?? string.Empty : string.Empty;

        public bool DriverCanSlave => Connected && _dome.CanSlave;

        public bool CanSetShutter => Connected && _dome.CanSetShutter;

        public bool CanSetPark => Connected && _dome.CanSetPark;

        public bool CanSetAzimuth => Connected && _dome.CanSetAzimuth;

        public bool CanPark => Connected && _dome.CanPark;

        public bool CanFindHome => Connected && _dome.CanFindHome;

        public double Azimuth => TryGetProperty(() => _dome.Azimuth, -1);

        public bool AtPark => Connected && _dome.AtPark;

        public bool AtHome => Connected && _dome.AtHome;

        public bool DriverSlaved => TryGetProperty(() => _dome.Slaved, false);

        public bool Slewing => TryGetProperty(() => _dome.Slewing, false);

        public ShutterState ShutterStatus => TryGetProperty(() => _dome.ShutterStatus.FromASCOM(), ShutterState.ShutterNone); 

        public bool CanSyncAzimuth => Connected && _dome.CanSyncAzimuth;

        public async Task<bool> Connect(CancellationToken token) {
            return await Task.Run(() => {
                try {
                    _dome = new Dome(Id);
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
            _dome?.Dispose();
        }

        public void Disconnect() {
            Connected = false;
            _dome?.Dispose();
            _dome = null;
        }

        public void SetupDialog() {
            if (HasSetupDialog) {
                try {
                    bool dispose = false;
                    if (_dome == null) {
                        _dome = new Dome(Id);
                    }
                    _dome.SetupDialog();
                    if (dispose) {
                        _dome.Dispose();
                        _dome = null;
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
                    ct.Register(() => _dome.AbortSlew());
                    await Task.Run(() => _dome.SlewToAzimuth(azimuth), ct);
                } else {
                    Notification.ShowWarning(Locale.Loc.Instance["LblDomeCannotSlew"]);
                }
            } else {
                Notification.ShowWarning(Locale.Loc.Instance["LblDomeNotConnected"]);
            }
        }

        public void StopSlewing() {
            if (Connected) {
                _dome.AbortSlew();
            } else {
                Notification.ShowWarning(Locale.Loc.Instance["LblDomeNotConnected"]);
            }
        }

        public void StopShutter() {
            // ASCOM only allows you to stop both slew and shutter movement together
            StopSlewing();
        }

        public async Task OpenShutter(CancellationToken ct) {
            if (Connected) {
                if (CanSetShutter) {
                    ct.Register(() => _dome.AbortSlew());
                    if (ShutterStatus == ShutterState.ShutterError) {
                        // If shutter is in the error state, you must close it before re-opening
                        await Task.Run(() => {
                            _dome.CloseShutter();
                            _dome.OpenShutter();
                            }, ct);
                    } else if (ShutterStatus != ShutterState.ShutterOpen) {
                        await Task.Run(() => _dome.OpenShutter(), ct);
                    }
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
                    if (ShutterStatus != ShutterState.ShutterClosed) {
                        ct.Register(() => _dome.AbortSlew());
                        await Task.Run(() => _dome.CloseShutter(), ct);
                    }
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
                    ct.Register(() => _dome.AbortSlew());
                    await Task.Run(() => _dome.FindHome(), ct);
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
                    ct.Register(() => _dome.AbortSlew());
                    await Task.Run(() => _dome.Park(), ct);
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
                    _dome.SetPark();
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
                    _dome.SyncToAzimuth(azimuth);
                } else {
                    Notification.ShowWarning(Locale.Loc.Instance["LblDomeCannotSyncAzimuth"]);
                }
            } else {
                Notification.ShowWarning(Locale.Loc.Instance["LblDomeNotConnected"]);
            }
        }

        public Task StartRotateCW(CancellationToken ct) {
            return Task.Run(async () => {
                while (!ct.IsCancellationRequested) {
                    var targetAzimuth = (Azimuth + 90.0) % 360.0;
                    await SlewToAzimuth(targetAzimuth, ct);
                }
            }, ct);
        }

        public Task StartRotateCCW(CancellationToken ct) {
            return Task.Run(async () => {
                while (!ct.IsCancellationRequested) {
                    var targetAzimuth = (Azimuth + 270.0) % 360.0;
                    await SlewToAzimuth(targetAzimuth, ct);
                }
            }, ct);
        }
    }
}
