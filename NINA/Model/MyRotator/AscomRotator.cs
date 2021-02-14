#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using ASCOM.DriverAccess;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Notification;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyRotator {

    internal class AscomRotator : BaseINPC, IRotator, IDisposable {

        public AscomRotator(string id, string name) {
            this.Id = id;
            this.Name = name;
        }

        private Rotator rotator;

        public string Category { get; } = "ASCOM";

        public bool CanReverse {
            get {
                if (Connected) {
                    return rotator.CanReverse;
                } else {
                    return false;
                }
            }
        }

        public bool Reverse {
            get {
                if (CanReverse) {
                    return rotator.Reverse;
                } else {
                    return false;
                }
            }
            set {
                if (CanReverse) {
                    rotator.Reverse = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool IsMoving {
            get {
                if (Connected) {
                    return rotator.IsMoving;
                } else {
                    return false;
                }
            }
        }

        private bool synced;

        public bool Synced {
            get => synced;
            private set {
                synced = value;
                RaisePropertyChanged();
            }
        }

        private float offset = 0;

        public float Position {
            get => Astrometry.EuclidianModulus(MechanicalPosition + offset, 360);
        }

        public float MechanicalPosition {
            get {
                if (Connected) {
                    return rotator.Position;
                } else {
                    return float.NaN;
                }
            }
        }

        public float StepSize {
            get {
                if (Connected) {
                    return rotator.StepSize;
                } else {
                    return float.NaN;
                }
            }
        }

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
                        val = rotator.Connected;
                        if (_connected != val) {
                            Notification.ShowWarning(Locale.Loc.Instance["LblRotatorConnectionLost"]);
                            Disconnect();
                        }
                    } catch (Exception ex) {
                        Logger.Error(ex);
                        Notification.ShowWarning(Locale.Loc.Instance["LblRotatorConnectionLost"]);
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
                    rotator.Connected = value;
                    _connected = value;
                } catch (Exception ex) {
                    Logger.Error(ex);
                    _connected = false;
                }
            }
        }

        public string Description {
            get {
                return rotator.Description;
            }
        }

        public string DriverInfo {
            get {
                return rotator.DriverInfo;
            }
        }

        public string DriverVersion {
            get {
                return rotator.DriverVersion;
            }
        }

        public async Task<bool> Connect(CancellationToken token) {
            return await Task<bool>.Run(() => {
                try {
                    offset = 0;
                    rotator = new Rotator(Id);
                    Connected = true;
                    Synced = false;
                    if (Connected) {
                        RaiseAllPropertiesChanged();
                    }
                } catch (ASCOM.DriverAccessCOMException ex) {
                    Utility.Utility.HandleAscomCOMException(ex);
                } catch (System.Runtime.InteropServices.COMException ex) {
                    Utility.Utility.HandleAscomCOMException(ex);
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError("Unable to connect to rotator " + ex.Message);
                }
                return Connected;
            });
        }

        public void Disconnect() {
            Connected = false;
            Dispose();
        }

        public void Dispose() {
            rotator?.Dispose();
            rotator = null;
        }

        public void Halt() {
            if (IsMoving) {
                rotator?.Halt();
            }
        }

        public void Sync(float skyAngle) {
            offset = skyAngle - MechanicalPosition;
            RaisePropertyChanged(nameof(Position));
            Synced = true;
            Logger.Debug($"ASCOM - Mechanical Position is {MechanicalPosition}° - Sync Position to Sky Angle {skyAngle}° using offset {offset}");
        }

        public void Move(float angle) {
            if (Connected) {
                if (angle >= 360) {
                    angle = Astrometry.EuclidianModulus(angle, 360);
                }
                if (angle <= -360) {
                    angle = Astrometry.EuclidianModulus(angle, -360);
                }

                Logger.Debug($"ASCOM - Move relative by {angle}° - Mechanical Position reported by rotator {MechanicalPosition}° and offset {offset}");
                rotator?.Move(angle);
            }
        }

        public void MoveAbsoluteMechanical(float targetPosition) {
            if (Connected) {
                var movement = targetPosition - MechanicalPosition;
                Move(movement);
            }
        }

        public void MoveAbsolute(float targetPosition) {
            if (Connected) {
                Move(targetPosition - Position);
            }
        }

        public void SetupDialog() {
            if (HasSetupDialog) {
                try {
                    bool dispose = false;
                    if (rotator == null) {
                        rotator = new Rotator(Id);
                        dispose = true;
                    }
                    rotator.SetupDialog();
                    if (dispose) {
                        rotator.Dispose();
                        rotator = null;
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
                }
            }
        }
    }
}