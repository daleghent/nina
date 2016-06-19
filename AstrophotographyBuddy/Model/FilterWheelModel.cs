using ASCOM.DriverAccess;
using AstrophotographyBuddy.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        private string _description;
        private string _name;
        private string _driverInfo;
        private string _driverVersion;
        private short _interfaceVersion;
        private int[] _focusOffsets;
        private string[] _names;
        private short _position;
        private ArrayList _supportedActions;



        public string Description {
            get {
                return _description;
            }

            set {
                _description = value;
                RaisePropertyChanged();
            }
        }

        public string Name {
            get {
                return _name;
            }

            set {
                _name = value;
                RaisePropertyChanged();
            }
        }

        public string DriverInfo {
            get {
                return _driverInfo;
            }

            set {
                _driverInfo = value;
                RaisePropertyChanged();
            }
        }

        public string DriverVersion {
            get {
                return _driverVersion;
            }

            set {
                _driverVersion = value;
                RaisePropertyChanged();
            }
        }

        public short InterfaceVersion {
            get {
                return _interfaceVersion;
            }

            set {
                _interfaceVersion = value;
                RaisePropertyChanged();
            }
        }

        public int[] FocusOffsets {
            get {
                return _focusOffsets;
            }

            set {
                _focusOffsets = value;
                RaisePropertyChanged();
            }
        }

        public string[] Names {
            get {
                return _names;
            }

            set {
                _names = value;
                RaisePropertyChanged();
            }
        }

        public short Position {
            get {
                if(FW != null && FW.Connected) {
                    return FW.Position;
                } else {
                    return -1;
                }
                
            }

            set {                
                if (FW != null) {
                    try {                 
                        FW.Position = value;                        
                    } catch (Exception ex) {
                    
                    }
                    
                }                
                RaisePropertyChanged();
            }
        }

        public ArrayList SupportedActions {
            get {
                return _supportedActions;
            }

            set {
                _supportedActions = value;
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
            Position = -1;
            Filters.Clear();
            FW.Dispose();
            init();            
        }


        private void getFWInfos() {
            try {
                Description = FW.Description;
                Name = FW.Name;
            } catch (Exception ex) {

            }

            try {
                DriverInfo = FW.DriverInfo;
                DriverVersion = FW.DriverVersion;
                InterfaceVersion = FW.InterfaceVersion;
            }catch (Exception ex) {

            }
            
            try {
                FocusOffsets = FW.FocusOffsets;
                Names = FW.Names;
                Position = FW.Position;

                var l = new ObservableCollection<FilterInfo>();
                for (int i = 0; i < Names.Length; i++) {                    
                    l.Add(new FilterInfo(Names[i], FocusOffsets[i], (short)i));
                }
                Filters = l;
                
            } catch (Exception ex) {

            }

            
            try {
                SupportedActions = FW.SupportedActions;
            } catch (Exception ex) {

            }
        }

        private ObservableCollection<FilterInfo> _filters;
        public ObservableCollection<FilterInfo> Filters {
            get {                
                return _filters;
            }
            set {
                _filters = value;
                RaisePropertyChanged();
            }
        }

        public class FilterInfo :BaseINPC {
            private string _name;
            private int _focusOffset;
            private short _position;

            public string Name {
                get {
                    return _name;
                }

                set {
                    _name = value;
                    RaisePropertyChanged();
                }
            }

            public int FocusOffset {
                get {
                    return _focusOffset;
                }

                set {
                    _focusOffset = value;
                    RaisePropertyChanged();
                }
            }

            public short Position {
                get {
                    return _position;
                }

                set {
                    _position = value;
                    RaisePropertyChanged();
                }
            }

            public FilterInfo(string n, int offset, short position) {
                Name = n;
                FocusOffset = offset;
                Position = position;
            }              
        }
    }
}
