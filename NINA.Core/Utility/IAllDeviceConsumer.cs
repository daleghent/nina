using NINA.Model.MyCamera;
using NINA.Model.MyDome;
using NINA.Model.MyFilterWheel;
using NINA.Model.MyFocuser;
using NINA.Model.MyGuider;
using NINA.Model.MyRotator;
using NINA.Model.MySwitch;
using NINA.Model.MyTelescope;
using NINA.Utility.Mediator.Interfaces;

namespace NINA.Utility {

    public interface IAllDeviceConsumer : ICameraConsumer, IFocuserConsumer, IRotatorConsumer, ITelescopeConsumer, IDomeConsumer, IFilterWheelConsumer, IGuiderConsumer, ISwitchConsumer {
        CameraInfo CameraInfo { get; }
        DomeInfo DomeInfo { get; }
        FilterWheelInfo FilterWheelInfo { get; }
        FocuserInfo FocuserInfo { get; }
        GuiderInfo GuiderInfo { get; }
        RotatorInfo RotatorInfo { get; }
        SwitchInfo SwitchInfo { get; }
        TelescopeInfo TelescopeInfo { get; }
    }
}