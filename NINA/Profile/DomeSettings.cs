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
                id = value;
                RaisePropertyChanged();
            }
        }

        private double scopePositionEastWest_mm = 0.0;

        [DataMember]
        public double ScopePositionEastWest_mm {
            get {
                return scopePositionEastWest_mm;
            }
            set {
                scopePositionEastWest_mm = value;
                RaisePropertyChanged();
            }
        }

        private double scopePositionNorthSouth_mm = 0.0;

        [DataMember]
        public double ScopePositionNorthSouth_mm {
            get {
                return scopePositionNorthSouth_mm;
            }
            set {
                scopePositionNorthSouth_mm = value;
                RaisePropertyChanged();
            }
        }

        private double scopePositionUpDown_mm = 0.0;

        [DataMember]
        public double ScopePositionUpDown_mm {
            get {
                return scopePositionUpDown_mm;
            }
            set {
                scopePositionUpDown_mm = value;
                RaisePropertyChanged();
            }
        }

        private double domeRadius_mm = 0.0;

        [DataMember]
        public double DomeRadius_mm {
            get {
                return domeRadius_mm;
            }
            set {
                domeRadius_mm = value;
                RaisePropertyChanged();
            }
        }

        private double gemAxis_mm = 0.0;

        [DataMember]
        public double GemAxis_mm {
            get {
                return gemAxis_mm;
            }
            set {
                gemAxis_mm = value;
                RaisePropertyChanged();
            }
        }

        private double azimuthTolerance_degrees = 1.0;

        [DataMember]
        public double AzimuthTolerance_degrees {
            get {
                return azimuthTolerance_degrees;
            }
            set {
                azimuthTolerance_degrees = value;
                RaisePropertyChanged();
            }
        }

        private bool useDirectSlaving = true;

        [DataMember]
        public bool UseDirectFollowing {
            get {
                return useDirectSlaving;
            }
            set {
                useDirectSlaving = value;
                RaisePropertyChanged();
            }
        }

        private bool findHomeBeforePark = false;

        [DataMember]
        public bool FindHomeBeforePark {
            get {
                return findHomeBeforePark;
            }
            set {
                findHomeBeforePark = value;
                RaisePropertyChanged();
            }
        }

        private int domeSyncTimeoutSeconds = 120;

        [DataMember]
        public int DomeSyncTimeoutSeconds {
            get {
                return domeSyncTimeoutSeconds;
            }
            set {
                domeSyncTimeoutSeconds = value;
                RaisePropertyChanged();
            }
        }
    }
}
