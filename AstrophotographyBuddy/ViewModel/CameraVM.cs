using ASCOM.DriverAccess;
using AstrophotographyBuddy.Utility;
using AstrophotographyBuddy.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace AstrophotographyBuddy {
    class CameraVM : BaseVM {

        public CameraVM() {
            Name = "Camera";
            //ConnectCameraCommand = new RelayCommand(connectCamera);
            ChooseCameraCommand = new RelayCommand(chooseCamera);
            DisconnectCommand = new RelayCommand(disconnectCamera);
            CoolCamCommand = new RelayCommand(coolCamera);
            CancelCoolCamCommand = new RelayCommand(cancelCoolCamera);
            Cam = new CameraModel();
            updateCamera = new DispatcherTimer();
            updateCamera.Interval = TimeSpan.FromMilliseconds(1000);
            updateCamera.Tick += updateCamera_Tick;

            CoolCameraTimer = new DispatcherTimer();
            CoolCameraTimer.Tick += coolCamera_Tick;
            CoolCameraTimer.Interval = TimeSpan.FromMilliseconds(300);
            CoolingRunning = false;
        }

        private void coolCamera_Tick(object sender, EventArgs e) {           

            double currentTemp = Cam.CCDTemperature;
            double deltaTemp = currentTemp - TargetTemp;

            
            DateTime now = DateTime.Now;
            TimeSpan delta = now.Subtract(deltaT);

            double tempStep = deltaTemp / (Duration * ((1000*60)/ (double)delta.TotalMilliseconds));

            double newTemp = Cam.SetCCDTemperature - tempStep;
            if((deltaTemp < 0 && newTemp > TargetTemp) || (deltaTemp > 0 && newTemp < TargetTemp)) {
                newTemp = TargetTemp;
            }
            Cam.SetCCDTemperature = newTemp;

            Duration = Duration - ((double)delta.TotalMilliseconds / (1000 * 60));

            CoolingProgress = 1 - (Duration / _initalDuration);

            deltaT = DateTime.Now;

            if (Duration <= 0) {
                Duration = 0;
                Cam.SetCCDTemperature = TargetTemp;
                CoolingRunning = false;
                CoolCameraTimer.Stop();
                
            }
        }

        private double _initalDuration;
        private double _coolingProgress;
        public double CoolingProgress {
            get {
                return _coolingProgress;
            }

            set {
                _coolingProgress = value;
                RaisePropertyChanged();
            }
        }


        private DateTime deltaT;

        private bool _coolingRunning;
        public bool CoolingRunning {
            get {
                return _coolingRunning;
            } set {
                _coolingRunning = value;
                RaisePropertyChanged();
            }
        }

        private void coolCamera(object o) {
            Cam.CoolerOn = true;
            if (Duration == 0) {                            
                Cam.SetCCDTemperature = TargetTemp;
            } else {
                deltaT = DateTime.Now;
                double currentTemp = Cam.CCDTemperature;
                Cam.SetCCDTemperature = currentTemp;
                _initalDuration = Duration;
                CoolCameraTimer.Start();
                CoolingRunning = true;
            }

        }

        private void cancelCoolCamera(object o) {
            CoolCameraTimer.Stop();
            CoolingRunning = false;
            Cam.SetCCDTemperature = Cam.CCDTemperature;
        }
            

        DispatcherTimer updateCamera;

        private DispatcherTimer _coolCameraTimer;
        public DispatcherTimer CoolCameraTimer {
            get {
                return _coolCameraTimer;
            }
            private set {
                _coolCameraTimer = value;
                RaisePropertyChanged();
            }
        }



        private double _targetTemp;
        public double TargetTemp {
            get {
                return _targetTemp;
            }
            set {
                _targetTemp = value;
                RaisePropertyChanged();
            }
        }

        private double _duration;
        public double Duration {
            get {
                return _duration;
            }
            set {
                _duration = value;
                RaisePropertyChanged();
            }
        }


        private CameraModel _cam;
        public CameraModel Cam {
            get {
                return _cam;
            }
            set {
                _cam = value;
                RaisePropertyChanged();
            }
        }

        /*private void connectCamera(object obj) {
            Cam.AscomCamera = new Camera(Cam.ProgId);
        }*/

        private void chooseCamera(object obj) {
            updateCamera.Stop();
            if (Cam.connect()) {                
                updateCamera.Start();
            }
            
        }

        private void disconnectCamera(object obj) {
            updateCamera.Stop();
            CoolCameraTimer.Stop();   
            Cam.disconnect();            
        }

        void updateCamera_Tick(object sender, EventArgs e) {
           if(Cam.Connected) {
                Cam.updateValues();
            }
            
        }

        /*private ICommand _connectCameraCommand;
        public ICommand ConnectCameraCommand {
            get {
                return _connectCameraCommand;
            }
            set {
                _connectCameraCommand = value;
                RaisePropertyChanged();
            }
        } */

        private ICommand _coolCamCommand;
        public ICommand CoolCamCommand {
            get {
                return _coolCamCommand;
            }
            set {
                _coolCamCommand = value;
                RaisePropertyChanged();
            }
        }

        private ICommand _chooseCameraCommand;
        public ICommand ChooseCameraCommand {
            get {
                return _chooseCameraCommand; 
            } set {
                _chooseCameraCommand = value;
                RaisePropertyChanged();
            }
        }

        private ICommand _disconnectCommand;
        public ICommand DisconnectCommand {
            get {
                return _disconnectCommand;
            }
            set {
                _disconnectCommand = value;
                RaisePropertyChanged();
            }
        }

        private ICommand _cancelCoolCommand;
        public ICommand CancelCoolCamCommand {
            get { return _cancelCoolCommand; }
            private set { _cancelCoolCommand = value; RaisePropertyChanged(); } }

        
    }
}
