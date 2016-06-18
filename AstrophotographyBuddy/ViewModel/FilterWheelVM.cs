using AstrophotographyBuddy.Model;
using AstrophotographyBuddy.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AstrophotographyBuddy.ViewModel {
    class FilterWheelVM: BaseVM {
        public FilterWheelVM() {
            Name = "Filter Wheel";
            FW = new FilterWheelModel();
            ChooseFWCommand = new RelayCommand(chooseFW);
            DisconnectCommand = new RelayCommand(disconnectFW);
        }

        private FilterWheelModel _fW;
        public FilterWheelModel FW {
            get {
                return _fW;
            }
            set {
                _fW = value;
                RaisePropertyChanged();
            }
        }

        private void chooseFW(object obj) {            
            if (FW.connect()) {
                
            }
        }

        private void disconnectFW(object obj) {
            FW.disconnect();
        }

        private ICommand _chooseFWCommand;
        public ICommand ChooseFWCommand {
            get {
                return _chooseFWCommand;
            }
            set {
                _chooseFWCommand = value;
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
