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

            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["PHD2SVG"];            
            MaxY = 4;      
        }

        public PHD2Client PHD2Client {
            get {
                return Utility.Utility.PHDClient;
            }
        }

        public double Interval {
            get {
                return MaxY / 4;
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
                RaisePropertyChanged("Interval");
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
                RaisePropertyChanged("Interval");
            }
        }

        

        private double _maxY;
        private double _minY;




    }
}
