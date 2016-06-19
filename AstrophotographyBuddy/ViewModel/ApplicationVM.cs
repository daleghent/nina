using AstrophotographyBuddy.Model;
using AstrophotographyBuddy.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AstrophotographyBuddy.ViewModel {
    class ApplicationVM : BaseVM {

        public ApplicationVM() {
            PrevViewCommand = new RelayCommand(getPrevView);
            NextViewCommand = new RelayCommand(getNextView);
            ToggleViewCommand = new RelayCommand(toggleView);
            Visibility = true;
            Views = new ObservableCollection<BaseVM>();
            //Views.Add(this);
            Name = "Menu";
            _activeView = this;
            
            
            var cam = this.CameraVM;
            var fw = this.FilterWheelVM;
            this.ImagingVM.Cam = cam.Cam;
            this.ImagingVM.FW = fw.FW;
            this.FrameFocusVM.Cam = cam.Cam;
           // var a = this.TelescopeVM;

            //addListeners();
        }

        private void addListeners() {
        }
        
        // protected void syncModel(object sender, PropertyChangedEventArgs e) {
        //this.ImagingVM.Cam = this.CameraVM.Cam;            
        //  }

        private ObservableCollection<BaseVM> _views;
        private BaseVM _activeView;

        private CameraVM _cameraVM;
        public CameraVM CameraVM {
            get {
                if(_cameraVM == null) {
                    _cameraVM = new CameraVM();
                    Views.Add(_cameraVM);

                }
                return _cameraVM;
            }
            set {
                _cameraVM = value;
                RaisePropertyChanged();
            }
        }

        private ImagingVM _imagingVM;
        public ImagingVM ImagingVM {
            get {
                if (_imagingVM == null) {
                    _imagingVM = new ImagingVM();
                    Views.Add(_imagingVM);
                }
                return _imagingVM;
            }
            set {
                _imagingVM = value;
                RaisePropertyChanged();
            }
        }

        private TelescopeVM _telescopeVM;
        public TelescopeVM TelescopeVM {
            get {
                if (_telescopeVM == null) {
                    _telescopeVM = new TelescopeVM();
                    Views.Add(_telescopeVM);
                }
                return _telescopeVM;
            }
            set {
                _telescopeVM = value;
                RaisePropertyChanged();
            }
        }

        private OptionsVM _optionsVM;
        public OptionsVM OptionsVM {
            get {
                if (_optionsVM == null) {
                    _optionsVM = new OptionsVM();
                    Views.Add(_optionsVM);
                }
                return _optionsVM;
            }
            set {
                _optionsVM = value;
                RaisePropertyChanged();
            }
        }

        private FilterWheelVM _filterWheelVM;
        public FilterWheelVM FilterWheelVM {
            get {
                if (_filterWheelVM == null) {
                    _filterWheelVM = new FilterWheelVM();
                    Views.Add(_filterWheelVM);
                }
                return _filterWheelVM;
            }
            set {
                _filterWheelVM = value;
                RaisePropertyChanged();
            }
        }

        private FrameFocusVM _frameFocusVM;
        public FrameFocusVM FrameFocusVM {
            get {
                if (_frameFocusVM == null) {
                    _frameFocusVM = new FrameFocusVM();
                    Views.Add(_frameFocusVM);
                }
                return _frameFocusVM;
            }
            set {
                _frameFocusVM = value;
                RaisePropertyChanged();
            }
        }

        private void toggleView(object o) {
            if (o != null) {
                BaseVM a = _activeView;
                BaseVM b;
                if (o.ToString() == this.Name) {
                    b = this;
                }
                else {
                    b = getViewByName(o.ToString());
                }

                if (a != null && b != null && a != b) {
                    a.Visibility = false;
                    b.Visibility = true;
                    _activeView = b;
                }
            }

        }

        private void getNextView(object o) {
            BaseVM a = _activeView;
            a.Visibility = false;
            int idx = Views.IndexOf(a);
            idx = (idx + 1) % Views.Count;
            Views[idx].Visibility = true;
            _activeView = Views[idx];         
        }

        private void getPrevView(object o) {
            BaseVM a = _activeView;
            a.Visibility = false;
            int idx = Views.IndexOf(a);
            idx = (idx - 1) % Views.Count;
            if (idx < 0) idx = Views.Count - 1;
            Views[idx].Visibility = true;
            _activeView = Views[idx];
        }

       

        private BaseVM getViewByName(String name) {
            var a = (from b in Views where b.Name == name select b);
            if (a.Count() > 0) {
                return a.First();
            }
            else {
                return null;
            }
        }

                private ICommand _nextViewCommand;
        public ICommand NextViewCommand {
            get {
                return _nextViewCommand;
            }
            set {
                _nextViewCommand = value;
                RaisePropertyChanged();
            }
        }

        private ICommand _pevViewCommand;
        public ICommand PrevViewCommand {
            get {
                return _pevViewCommand;
            }
            set {
                _pevViewCommand = value;
                RaisePropertyChanged();
            }
        }

        private ICommand _toggleViewCommand;
        public ICommand ToggleViewCommand {
            get {
                return _toggleViewCommand;
            }

            set {
                _toggleViewCommand = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<BaseVM> Views {
            get {
                return _views;
            }

            set {
                _views = value;
                RaisePropertyChanged();
            }
        }
    }
}
