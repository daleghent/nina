using NINA.Profile;
using NINA.ViewModel.Equipment.Camera;
using NINA.ViewModel.Equipment.Dome;
using NINA.ViewModel.Equipment.FilterWheel;
using NINA.ViewModel.Equipment.FlatDevice;
using NINA.ViewModel.Equipment.Focuser;
using NINA.ViewModel.Equipment.Guider;
using NINA.ViewModel.Equipment.Rotator;
using NINA.ViewModel.Equipment.SafetyMonitor;
using NINA.ViewModel.Equipment.Switch;
using NINA.ViewModel.Equipment.Telescope;
using NINA.ViewModel.Equipment.WeatherData;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.ViewModel {

    internal class EquipmentVM : BaseVM, IEquipmentVM {

        public EquipmentVM(IProfileService profileService, ICameraVM cameraVM, IFilterWheelVM filterWheelVM, IFocuserVM focuserVM,
            IRotatorVM rotatorVM, ITelescopeVM telescopeVM, IDomeVM domeVM, IGuiderVM guiderVM, ISwitchVM switchVM,
            IFlatDeviceVM flatDeviceVM, IWeatherDataVM weatherDataVM, ISafetyMonitorVM safetyMonitorVM) : base(profileService) {
            CameraVM = cameraVM;
            FilterWheelVM = filterWheelVM;
            FocuserVM = focuserVM;
            RotatorVM = rotatorVM;
            TelescopeVM = telescopeVM;
            DomeVM = domeVM;
            GuiderVM = guiderVM;
            SwitchVM = switchVM;
            FlatDeviceVM = flatDeviceVM;
            WeatherDataVM = weatherDataVM;
            SafetyMonitorVM = safetyMonitorVM;
        }

        public ICameraVM CameraVM { get; }
        public IFilterWheelVM FilterWheelVM { get; }
        public IFocuserVM FocuserVM { get; }
        public IRotatorVM RotatorVM { get; }
        public ITelescopeVM TelescopeVM { get; }
        public IDomeVM DomeVM { get; }
        public IGuiderVM GuiderVM { get; }
        public ISwitchVM SwitchVM { get; }
        public IFlatDeviceVM FlatDeviceVM { get; }
        public IWeatherDataVM WeatherDataVM { get; }
        public ISafetyMonitorVM SafetyMonitorVM { get; }
    }
}