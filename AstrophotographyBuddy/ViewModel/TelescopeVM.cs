using AstrophotographyBuddy.Model;
using AstrophotographyBuddy.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace AstrophotographyBuddy.ViewModel {
    class TelescopeVM : BaseVM {
        public TelescopeVM() {
            Name = "Telescope";
            ImageURI = @"/AstrophotographyBuddy;component/Resources/Telescope.png";
            Telescope = new TelescopeModel();
            ChooseTelescopeCommand = new RelayCommand(chooseTelescope);
            DisconnectCommand = new RelayCommand(disconnectTelescope);
            StepperMoveRateCommand = new RelayCommand(stepMoveRate);

            MoveCommand = new RelayCommand(move);
            StopMoveCommand = new RelayCommand(stopMove);
            StopSlewCommand = new RelayCommand(stopSlew);

            _updateTelescope = new DispatcherTimer();
            _updateTelescope.Interval = TimeSpan.FromMilliseconds(300);
            _updateTelescope.Tick += updateTelescope_Tick;
        }

        private void updateTelescope_Tick(object sender, EventArgs e) {            
            if (Telescope.Connected) {
                Telescope.updateValues();
            }            
        }

        private DispatcherTimer _updateTelescope;

       private TelescopeModel _telescope;
        public TelescopeModel Telescope {
            get {
                return _telescope;
            }
            set {
                _telescope = value;
                RaisePropertyChanged();
            }
        }

        private void chooseTelescope(object obj) {
            _updateTelescope.Stop();
            if (Telescope.connect()) {
                _updateTelescope.Start();
            }
        }

        private void disconnectTelescope(object obj) {
            System.Windows.MessageBoxResult result = System.Windows.MessageBox.Show("Disconnect Telescope?", "", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxImage.Question, System.Windows.MessageBoxResult.Cancel);
            if (result == System.Windows.MessageBoxResult.OK) {
                _updateTelescope.Stop();
                Telescope.disconnect();
            }
        }

        private void stepMoveRate(object obj) {
            string cmd = obj.ToString();
            if(cmd == "+") {
                Telescope.MovingRate++;
            } else {
                Telescope.MovingRate--;
            }
        }

        private void move(object obj) {
            string cmd = obj.ToString();
            if(cmd == "W") {                                
                Telescope.moveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary, -Telescope.MovingRate);
            }
            if (cmd == "O") {
                Telescope.moveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary, Telescope.MovingRate);
            }
            if (cmd == "N") {
                Telescope.moveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisSecondary, Telescope.MovingRate);
            }
            if (cmd == "S") {                
                Telescope.moveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisSecondary, -Telescope.MovingRate);
            }
        }

        private void stopMove(object obj) {
            string cmd = obj.ToString();
            if (cmd == "W") {
                Telescope.moveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary, 0);
            }
            if (cmd == "O") {
                Telescope.moveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary, 0);
            }
            if (cmd == "N") {
                Telescope.moveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisSecondary, 0);
            }
            if (cmd == "S") {
                Telescope.moveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisSecondary, 0);
            }
        }

        private void stopSlew(object obj) {
            Telescope.stopSlew();
        }

        private ICommand _chooseTelescopeCommand;
        public ICommand ChooseTelescopeCommand {
            get {
                return _chooseTelescopeCommand;
            }
            set {
                _chooseTelescopeCommand = value;
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

        private ICommand _moveCommand;
        public ICommand MoveCommand {
            get {
                return _moveCommand;
            }
            set {
                _moveCommand = value;
                RaisePropertyChanged();
            }
        }

        private ICommand _stopMoveCommand;
        public ICommand StopMoveCommand {
            get {
                return _stopMoveCommand;
            }
            set {
                _stopMoveCommand = value;
                RaisePropertyChanged();
            }
        }

        private ICommand _stopSlewCommand;
        public ICommand StopSlewCommand {
            get {
                return _stopSlewCommand;
            }
            set {
                _stopSlewCommand = value;
                RaisePropertyChanged();
            }
        }

        private ICommand _stepperMoveRateCommand;
        public ICommand StepperMoveRateCommand {
            get {
                return _stepperMoveRateCommand;
            }
            set {
                _stepperMoveRateCommand = value;
                RaisePropertyChanged();
            }
        }

    }
}
