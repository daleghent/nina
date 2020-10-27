using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Profile {

    [Serializable()]
    [DataContract]
    public class DomeSettings : Settings, IDomeSettings {

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        protected override void SetDefaultValues() {
            Id = "No_Device";
            ScopePositionEastWest_mm = 0.0;
            ScopePositionNorthSouth_mm = 0.0;
            ScopePositionUpDown_mm = 0.0;
            DomeRadius_mm = 0.0;
            GemAxis_mm = 0.0;
            AzimuthTolerance_degrees = 1.0;
            UseDirectFollowing = true;
            FindHomeBeforePark = false;
            DomeSyncTimeoutSeconds = 120;
        }

        private string id = string.Empty;

        [DataMember]
        public string Id {
            get {
                return id;
            }
            set {
                if (id != value) {
                    id = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double scopePositionEastWest_mm = 0.0;

        [DataMember]
        public double ScopePositionEastWest_mm {
            get {
                return scopePositionEastWest_mm;
            }
            set {
                if (scopePositionEastWest_mm != value) {
                    scopePositionEastWest_mm = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double scopePositionNorthSouth_mm = 0.0;

        [DataMember]
        public double ScopePositionNorthSouth_mm {
            get {
                return scopePositionNorthSouth_mm;
            }
            set {
                if (scopePositionNorthSouth_mm != value) {
                    scopePositionNorthSouth_mm = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double scopePositionUpDown_mm = 0.0;

        [DataMember]
        public double ScopePositionUpDown_mm {
            get {
                return scopePositionUpDown_mm;
            }
            set {
                if (scopePositionUpDown_mm != value) {
                    scopePositionUpDown_mm = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double domeRadius_mm = 0.0;

        [DataMember]
        public double DomeRadius_mm {
            get {
                return domeRadius_mm;
            }
            set {
                if (domeRadius_mm != value) {
                    domeRadius_mm = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double gemAxis_mm = 0.0;

        [DataMember]
        public double GemAxis_mm {
            get {
                return gemAxis_mm;
            }
            set {
                if (gemAxis_mm != value) {
                    gemAxis_mm = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double lateralAxis_mm = 0.0;

        [DataMember]
        public double LateralAxis_mm {
            get {
                return lateralAxis_mm;
            }
            set {
                if (lateralAxis_mm != value) {
                    lateralAxis_mm = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double azimuthTolerance_degrees = 1.0;

        [DataMember]
        public double AzimuthTolerance_degrees {
            get {
                return azimuthTolerance_degrees;
            }
            set {
                if (azimuthTolerance_degrees != value) {
                    azimuthTolerance_degrees = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool useDirectSlaving = true;

        [DataMember]
        public bool UseDirectFollowing {
            get {
                return useDirectSlaving;
            }
            set {
                if (useDirectSlaving != value) {
                    useDirectSlaving = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool findHomeBeforePark = false;

        [DataMember]
        public bool FindHomeBeforePark {
            get {
                return findHomeBeforePark;
            }
            set {
                if (findHomeBeforePark != value) {
                    findHomeBeforePark = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int domeSyncTimeoutSeconds = 120;

        [DataMember]
        public int DomeSyncTimeoutSeconds {
            get {
                return domeSyncTimeoutSeconds;
            }
            set {
                if (domeSyncTimeoutSeconds != value) {
                    domeSyncTimeoutSeconds = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool synchronizeDuringMountSlew = false;

        [DataMember]
        public bool SynchronizeDuringMountSlew {
            get {
                return synchronizeDuringMountSlew;
            }
            set {
                if (synchronizeDuringMountSlew != value) {
                    synchronizeDuringMountSlew = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double manualSlewDegrees = 10.0;

        [DataMember]
        public double RotateDegrees {
            get {
                return manualSlewDegrees;
            }
            set {
                if (manualSlewDegrees != value) {
                    manualSlewDegrees = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}