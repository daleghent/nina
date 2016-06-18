using ASCOM.DriverAccess;
using AstrophotographyBuddy.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AstrophotographyBuddy.Model {
    class FilterWheelModel :BaseINPC {
        public FilterWheelModel() {

        }

        private void init() {

        }

        private FilterWheel _fW;
        public FilterWheel FW {
            get {
                return _fW;
            }

            set {
                _fW = value;
                RaisePropertyChanged();
            }
        }

        

        private string _progId;
        public string ProgId {
            get {
                return _progId;
            }

            set {
                _progId = value;
                RaisePropertyChanged();
            }
        }

        public bool Connected {
            get {
                return _connected;
            }

            set {
                _connected = value;
                if (FW != null) {
                    FW.Connected = value;
                }
                RaisePropertyChanged();
            }
        }

        private bool _connected;

        public bool connect() {            
            bool con = false;
            string oldProgId = this.ProgId;
            ProgId = FilterWheel.Choose("ASCOM.Simulator.FilterWheel");
            if (!Connected || oldProgId != ProgId) {

                init();
                try {
                    FW = new FilterWheel(ProgId);
                    
                    //AscomCamera.Connected = true;
                    Connected = true;
                    getFWInfos();
                    con = true;
                }
                catch (ASCOM.DriverAccessCOMException ex) {
                    //CameraStateString = "Unable to connect to camera";
                    Connected = false;
                }
                catch (Exception ex) {
                    Connected = false;
                }

            }
            return con;
        }

        public void disconnect() {
            Connected = false;
            FW.Dispose();
            init();            
        }

        private void getFWInfos() {
            throw new NotImplementedException();
        }
    }
}
