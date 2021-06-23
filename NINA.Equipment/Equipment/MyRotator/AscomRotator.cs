#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using ASCOM.DriverAccess;
using NINA.Core.Utility;
using NINA.Astrometry;
using System;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Locale;
using NINA.Equipment.Interfaces;

namespace NINA.Equipment.Equipment.MyRotator {

    internal class AscomRotator : AscomDevice<Rotator>, IRotator, IDisposable {

        public AscomRotator(string id, string name) : base(id, name) {
        }

        public bool CanReverse {
            get {
                return GetProperty(nameof(Rotator.CanReverse), false);
            }
        }

        public bool Reverse {
            get {
                if (CanReverse) {
                    return GetProperty(nameof(Rotator.Reverse), false);
                }
                return false;
            }
            set {
                if (CanReverse) {
                    SetProperty(nameof(Rotator.Reverse), value);
                }
            }
        }

        public bool IsMoving {
            get {
                return GetProperty(nameof(Rotator.IsMoving), false);
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
            get => AstroUtil.EuclidianModulus(MechanicalPosition + offset, 360);
        }

        public float MechanicalPosition {
            get {
                return GetProperty(nameof(Rotator.Position), float.NaN);
            }
        }

        public float StepSize {
            get {
                return GetProperty(nameof(Rotator.StepSize), float.NaN);
            }
        }

        protected override string ConnectionLostMessage => Loc.Instance["LblRotatorConnectionLost"];

        public void Halt() {
            if (IsMoving) {
                device?.Halt();
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
                    angle = AstroUtil.EuclidianModulus(angle, 360);
                }
                if (angle <= -360) {
                    angle = AstroUtil.EuclidianModulus(angle, -360);
                }

                Logger.Debug($"ASCOM - Move relative by {angle}° - Mechanical Position reported by rotator {MechanicalPosition}° and offset {offset}");
                device?.Move(angle);
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

        protected override Task PreConnect() {
            offset = 0;
            Synced = false;
            return Task.CompletedTask;
        }

        protected override Rotator GetInstance(string id) {
            return new Rotator(id);
        }
    }
}