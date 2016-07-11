using AstrophotographyBuddy.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AstrophotographyBuddy.ViewModel {
    class PHD2VM :BaseVM {
        public PHD2VM() {
            Name = "PHD2";
            ImageURI = @"/AstrophotographyBuddy;component/Resources/PHD2.png";
            MaxY = 4;      
        }

        public PHD2Client PHD2Client {
            get {
                return Utility.Utility.PHDClient;
            }
        }

        public double MaxY {
            get {
                return _maxY;
            }

            set {
                _maxY = value;
                _minY = -value;
                RaisePropertyChanged("MinY");
                RaisePropertyChanged();
            }
        }

        public double MinY {
            get {
                return _minY;
            }

            set {
                _minY = value;
                _maxY = -value;
                RaisePropertyChanged();
                RaisePropertyChanged("MaxY");
            }
        }

        

        private double _maxY;
        private double _minY;




    }
}
