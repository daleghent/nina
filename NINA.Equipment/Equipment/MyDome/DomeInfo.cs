#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace NINA.Model.MyDome {

    public class DomeInfo : DeviceInfo {
        private ShutterState shutterStatus;

        public ShutterState ShutterStatus {
            get => shutterStatus;
            set { if (shutterStatus != value) { shutterStatus = value; RaisePropertyChanged(); } }
        }

        private bool driverCanFollow;

        public bool DriverCanFollow {
            get => driverCanFollow;
            set { if (driverCanFollow != value) { driverCanFollow = value; RaisePropertyChanged(); } }
        }

        private bool canSetShutter;

        public bool CanSetShutter {
            get => canSetShutter;
            set { if (canSetShutter != value) { canSetShutter = value; RaisePropertyChanged(); } }
        }

        private bool canSetPark;

        public bool CanSetPark {
            get => canSetPark;
            set { if (canSetPark != value) { canSetPark = value; RaisePropertyChanged(); } }
        }

        private bool canSetAzimuth;

        public bool CanSetAzimuth {
            get => canSetAzimuth;
            set { if (canSetAzimuth != value) { canSetAzimuth = value; RaisePropertyChanged(); } }
        }

        private bool canSyncAzimuth;

        public bool CanSyncAzimuth {
            get => canSyncAzimuth;
            set { if (canSyncAzimuth != value) { canSyncAzimuth = value; RaisePropertyChanged(); } }
        }

        private bool canPark;

        public bool CanPark {
            get => canPark;
            set { if (canPark != value) { canPark = value; RaisePropertyChanged(); } }
        }

        private bool canFindHome;

        public bool CanFindHome {
            get => canFindHome;
            set { if (canFindHome != value) { canFindHome = value; RaisePropertyChanged(); } }
        }

        private bool atPark;

        public bool AtPark {
            get => atPark;
            set { if (atPark != value) { atPark = value; RaisePropertyChanged(); } }
        }

        private bool atHome;

        public bool AtHome {
            get => atHome;
            set { if (atHome != value) { atHome = value; RaisePropertyChanged(); } }
        }

        private bool driverFollowing;

        public bool DriverFollowing {
            get => driverFollowing;
            set { if (driverFollowing != value) { driverFollowing = value; RaisePropertyChanged(); } }
        }

        private bool slewing;

        public bool Slewing {
            get => slewing;
            set { if (slewing != value) { slewing = value; RaisePropertyChanged(); } }
        }

        private double azimuth;

        public double Azimuth {
            get => azimuth;
            set { if (azimuth != value) { azimuth = value; RaisePropertyChanged(); } }
        }
    }
}