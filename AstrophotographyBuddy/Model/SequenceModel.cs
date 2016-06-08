using AstrophotographyBuddy.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AstrophotographyBuddy.Model {
    class SequenceModel :BaseINPC {

        public SequenceModel() {
            ExposureTime = 1;
            ImageType = "Light";
            FilterType = "L";
            Binning = "1x1";
            ExposureCount = 1;
        }

        public override string ToString() {
            return "Model";
        }

        public SequenceModel(double exposureTime, string imageType, string filterType, string binning, int exposureCount) {
            ExposureTime = exposureTime;
            ImageType = imageType;
            FilterType = filterType;
            Binning = binning;
            ExposureCount = exposureCount;
        }

        private double _exposureTime;
        private string _imageType;
        private string _filterType;
        private string _binning;
        private int _exposureCount;

        public double ExposureTime {
            get {
                return _exposureTime;
            }

            set {
                _exposureTime = value;
                RaisePropertyChanged();
            }
        }

        public string ImageType {
            get {
                return _imageType;
            }

            set {
                _imageType = value;
                RaisePropertyChanged();
            }
        }

        public string FilterType {
            get {
                return _filterType;
            }

            set {
                _filterType = value;
                RaisePropertyChanged();
            }
        }

        public string Binning {
            get {
                return _binning;
            }

            set {
                _binning = value;
                RaisePropertyChanged();
            }
        }

        public int ExposureCount {
            get {
                return _exposureCount;
            }

            set {
                _exposureCount = value;
                RaisePropertyChanged();
            }
        }
    }
}
