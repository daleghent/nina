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
            //FilterType = "L";
            //Binning = "1x1";
            ExposureCount = 1;
        }

        public override string ToString() {
            return "Model";
        }

        public SequenceModel(double exposureTime, string imageType, FilterWheelModel.FilterInfo filterType, CameraModel.BinningMode binning, int exposureCount) {
            ExposureTime = exposureTime;
            ImageType = imageType;
            FilterType = filterType;
            Binning = binning;
            ExposureCount = exposureCount;
        }

        private double _exposureTime;
        private string _imageType;
        private FilterWheelModel.FilterInfo _filterType;
        private CameraModel.BinningMode _binning;
        private int _exposureCount;
        private bool _active;

        public bool Active {
            get {
                return _active;
            }
            set {
                _active = value;
                RaisePropertyChanged();
            }
        }

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

        public FilterWheelModel.FilterInfo FilterType {
            get {
                return _filterType;
            }

            set {
                _filterType = value;
                RaisePropertyChanged();
            }
        }

        public CameraModel.BinningMode Binning {
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
