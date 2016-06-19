using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AstrophotographyBuddy.ViewModel {
    class OptionsVM :BaseVM {
        public OptionsVM() {
            Name = "Options";

            ImageFilePattern = "$$IMAGETYPE$$\\$$DATE$$_$$FILTER$$_$$SENSORTEMP$$_$$FRAMENR$$";

            HashSet<ImagePattern> p = new HashSet<ImagePattern>();
            p.Add(new ImagePattern("$$FILTER$$", "Filtername", "L"));
            p.Add(new ImagePattern("$$DATE$$", "Date with format YYYY-MM-DD", "2016-01-01-12-00-00"));
            p.Add(new ImagePattern("$$FRAMENR$$", "# of the Frame with format ####", "0001"));
            p.Add(new ImagePattern("$$IMAGETYPE$$", "Light, Flat, Dark, Bias", "Light"));
            p.Add(new ImagePattern("$$BINNING$$", "Binning of the camera", "1x1"));
            p.Add(new ImagePattern("$$SENSORTEMP$$", "Temperature of the Camera", "-15"));
            ImagePatterns = p;
        }

        private string _imageFilePath;
        public string ImageFilePath {
            get {
                return _imageFilePath;
            }
            set {
                _imageFilePath = value;
            }
        }

        private string _imageFilePattern;
        public string ImageFilePattern {
            get {
                return _imageFilePattern;
            }
            set {
                _imageFilePattern = value;
                RaisePropertyChanged();
            }
        }

        private HashSet<ImagePattern> _imagePatterns;
        public HashSet<ImagePattern> ImagePatterns {
            get {
                return _imagePatterns;
            }
            set {
                _imagePatterns = value;
                RaisePropertyChanged();
            }
        }

        public class ImagePattern {
            public ImagePattern(string k, string d, string v) {
                Key = k;
                Description = d;
                Value = v;
            }
            private string _key;
            private string _description;
            private string _value;
            public string Value {
                get {
                    return _value;
                }
                set {
                    _value = value;
                }
            }
            public string Key {
                get {
                    return _key;
                }

                set {
                    _key = value;
                }
            }

            public string Description {
                get {
                    return _description;
                }

                set {
                    _description = value;
                }
            }
        }


    }
}
