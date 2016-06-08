using AstrophotographyBuddy.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AstrophotographyBuddy.ViewModel {
    class BaseVM :BaseINPC {

        public BaseVM() {
            Visibility = false;
        }

        private bool _visibility;
        public bool Visibility {
            get {
                return _visibility;
            }
            set {
                _visibility = value;
                RaisePropertyChanged();
            }
        }

        private String _name;
        public String Name {
            get {
                return _name;
            }
            set {
                _name = value;
                RaisePropertyChanged();
            }
        }
    }
}
