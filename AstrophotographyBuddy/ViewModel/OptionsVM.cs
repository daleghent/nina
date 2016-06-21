using AstrophotographyBuddy.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AstrophotographyBuddy.ViewModel {
    class OptionsVM :BaseVM {
        public OptionsVM() {
            Name = "Options";
            PreviewFileCommand = new RelayCommand(previewFile);
            OpenFileDiagCommand = new RelayCommand(openFileDiag);
            TestPHDConnectionCommand = new AsyncCommand<bool>(() => testPHDConnection());
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

        private void openFileDiag(object o) {
            var diag = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = diag.ShowDialog();
            if(result == System.Windows.Forms.DialogResult.OK) {
                ImageFilePath = diag.SelectedPath + "\\";
            }
        }

        private ICommand _openFileDiagCommand;
        public ICommand OpenFileDiagCommand {
            get {
                return _openFileDiagCommand;
            }
            set {
                _openFileDiagCommand = value;
                RaisePropertyChanged();
            }
        }

        private void previewFile(object o) {
            System.Windows.MessageBox.Show(Utility.Utility.getImageFileString(ImagePatterns), "Example File Name", System.Windows.MessageBoxButton.OK);
        }

        private async Task<bool> testPHDConnection() {
            await Task.Run(() =>  Utility.Utility.Connect(Settings.PHD2ServerUrl, Settings.PHD2ServerPort, "Hello"));
            return true;
        }

        private ICommand _previewFileCommand;
        public ICommand PreviewFileCommand {
            get {
                return _previewFileCommand;
            }
            set {
                _previewFileCommand = value;
                RaisePropertyChanged();
            }
        }

        private Utility.AsyncCommand<bool> _testPHDConnectionCommand;
        public Utility.AsyncCommand<bool> TestPHDConnectionCommand {
            get {
                return _testPHDConnectionCommand;
            }
            set {
                _testPHDConnectionCommand = value;
                RaisePropertyChanged();
            }
        }

        public string ImageFilePath {
            get {
                return Settings.ImageFilePath;
            }
            set {
                Settings.ImageFilePath = value;
                RaisePropertyChanged();
            }
        }

        
        public string ImageFilePattern {
            get {
                return Settings.ImageFilePattern;                
            }
            set {
                Settings.ImageFilePattern  = value;
                RaisePropertyChanged();
            }
        }
        
        public string PHD2ServerUrl {
            get {
                return Settings.PHD2ServerUrl;
            }
            set {
                Settings.PHD2ServerUrl = value;
                RaisePropertyChanged();
            }
        }

        public int PHD2ServerPort {
            get {
                return Settings.PHD2ServerPort;
            }
            set {
                Settings.PHD2ServerPort = value;
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
