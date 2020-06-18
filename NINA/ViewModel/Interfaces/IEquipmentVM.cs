using NINA.ViewModel.Equipment.Camera;
using NINA.ViewModel.Equipment.Dome;
using NINA.ViewModel.Equipment.FilterWheel;
using NINA.ViewModel.Equipment.FlatDevice;
using NINA.ViewModel.Equipment.Focuser;
using NINA.ViewModel.Equipment.Guider;
using NINA.ViewModel.Equipment.Rotator;
using NINA.ViewModel.Equipment.Switch;
using NINA.ViewModel.Equipment.Telescope;
using NINA.ViewModel.Equipment.WeatherData;

namespace NINA.ViewModel.Interfaces {

    internal interface IEquipmentVM {
        ICameraVM CameraVM { get; }
        IDomeVM DomeVM { get; }
        IFilterWheelVM FilterWheelVM { get; }
        IFlatDeviceVM FlatDeviceVM { get; }
        IFocuserVM FocuserVM { get; }
        IGuiderVM GuiderVM { get; }
        IRotatorVM RotatorVM { get; }
        ISwitchVM SwitchVM { get; }
        ITelescopeVM TelescopeVM { get; }
        IWeatherDataVM WeatherDataVM { get; }
    }
}