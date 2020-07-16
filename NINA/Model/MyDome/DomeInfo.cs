using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Model.MyDome {
    public class DomeInfo : DeviceInfo {
        private ShutterState shutterStatus;
        public ShutterState ShutterStatus {
            get => shutterStatus;
            set { shutterStatus = value; RaisePropertyChanged(); }
        }

        private bool driverCanFollow;
        public bool DriverCanFollow {
            get => driverCanFollow;
            set { driverCanFollow = value; RaisePropertyChanged(); }
        }

        private bool canSetShutter;
        public bool CanSetShutter {
            get => canSetShutter;
            set { canSetShutter = value; RaisePropertyChanged(); }
        }

        private bool canSetPark;
        public bool CanSetPark {
            get => canSetPark;
            set { canSetPark = value; RaisePropertyChanged(); }
        }

        private bool canSetAzimuth;
        public bool CanSetAzimuth {
            get => canSetAzimuth;
            set { canSetAzimuth = value; RaisePropertyChanged(); }
        }

        private bool canSyncAzimuth;
        public bool CanSyncAzimuth {
            get => canSyncAzimuth;
            set { canSyncAzimuth = value; RaisePropertyChanged(); }
        }

        private bool canPark;
        public bool CanPark {
            get => canPark;
            set { canPark = value; RaisePropertyChanged(); }
        }

        private bool canFindHome;
        public bool CanFindHome {
            get => canFindHome;
            set { canFindHome = value; RaisePropertyChanged(); }
        }

        private bool atPark;
        public bool AtPark {
            get => atPark;
            set { atPark = value; RaisePropertyChanged(); }
        }

        private bool atHome;
        public bool AtHome {
            get => atHome;
            set { atHome = value; RaisePropertyChanged(); }
        }

        private bool driverFollowing;
        public bool DriverFollowing {
            get => driverFollowing;
            set { driverFollowing = value; RaisePropertyChanged(); }
        }

        private bool slewing;
        public bool Slewing {
            get => slewing;
            set { slewing = value; RaisePropertyChanged(); }
        }

        private double azimuth;
        public double Azimuth {
            get => azimuth;
            set { azimuth = value; RaisePropertyChanged(); }
        }
    }
}
