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
            Cam = new CameraModel();
            updateCamera = new DispatcherTimer();
            updateCamera.Interval = TimeSpan.FromMilliseconds(300);
            updateCamera.Tick += updateCamera_Tick;
            
        }

        DispatcherTimer updateCamera;

        

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

    }
}
