using NINA.Model.MySwitch;
using NINA.Model.MyDome;
using NINA.Model.MyFilterWheel;
using NINA.Model.MyFocuser;
using NINA.Model.MyGuider;
using NINA.Model.MyRotator;
using NINA.Model.MyTelescope;
using NINA.Utility.Mediator.Interfaces;
using NINA.Model.MyCamera;

namespace NINA.Utility {

    internal class AllDeviceConsumer : BaseINPC, IAllDeviceConsumer {
        private readonly ICameraMediator cameraMediator;
        private readonly IFocuserMediator focuserMediator;
        private readonly IRotatorMediator rotatorMediator;
        private readonly ITelescopeMediator telescopeMediator;
        private readonly IDomeMediator domeMediator;
        private readonly IFilterWheelMediator filterWheelMediator;
        private readonly IGuiderMediator guiderMediator;
        private readonly ISwitchMediator switchMediator;

        public AllDeviceConsumer(ICameraMediator cameraMediator, IFocuserMediator focuserMediator, IRotatorMediator rotatorMediator, ITelescopeMediator telescopeMediator,
            IDomeMediator domeMediator, IFilterWheelMediator filterWheelMediator, IGuiderMediator guiderMediator, ISwitchMediator switchMediator) {
            this.cameraMediator = cameraMediator;
            this.focuserMediator = focuserMediator;
            this.rotatorMediator = rotatorMediator;
            this.telescopeMediator = telescopeMediator;
            this.domeMediator = domeMediator;
            this.filterWheelMediator = filterWheelMediator;
            this.guiderMediator = guiderMediator;
            this.switchMediator = switchMediator;

            cameraMediator.RegisterConsumer(this);
            focuserMediator.RegisterConsumer(this);
            rotatorMediator.RegisterConsumer(this);
            telescopeMediator.RegisterConsumer(this);
            domeMediator.RegisterConsumer(this);
            filterWheelMediator.RegisterConsumer(this);
            guiderMediator.RegisterConsumer(this);
            switchMediator.RegisterConsumer(this);
        }

        public void Dispose() {
            cameraMediator.RemoveConsumer(this);
            focuserMediator.RemoveConsumer(this);
            rotatorMediator.RemoveConsumer(this);
            telescopeMediator.RemoveConsumer(this);
            domeMediator.RemoveConsumer(this);
            filterWheelMediator.RemoveConsumer(this);
            guiderMediator.RemoveConsumer(this);
            switchMediator.RemoveConsumer(this);
        }

        private CameraInfo cameraInfo;

        public CameraInfo CameraInfo {
            get => cameraInfo;
            private set {
                cameraInfo = value;
                RaisePropertyChanged();
            }
        }

        public void UpdateDeviceInfo(CameraInfo deviceInfo) {
            CameraInfo = deviceInfo;
        }

        private FocuserInfo focuserInfo;

        public FocuserInfo FocuserInfo {
            get => focuserInfo;
            private set {
                focuserInfo = value;
                RaisePropertyChanged();
            }
        }

        public void UpdateDeviceInfo(FocuserInfo deviceInfo) {
            FocuserInfo = deviceInfo;
        }

        private RotatorInfo rotatorInfo;

        public RotatorInfo RotatorInfo {
            get => rotatorInfo;
            private set {
                rotatorInfo = value;
                RaisePropertyChanged();
            }
        }

        public void UpdateDeviceInfo(RotatorInfo deviceInfo) {
            RotatorInfo = deviceInfo;
        }

        private TelescopeInfo telescopeInfo;

        public TelescopeInfo TelescopeInfo {
            get => telescopeInfo;
            private set {
                telescopeInfo = value;
                RaisePropertyChanged();
            }
        }

        public void UpdateDeviceInfo(TelescopeInfo deviceInfo) {
            TelescopeInfo = deviceInfo;
        }

        private DomeInfo domeInfo;

        public DomeInfo DomeInfo {
            get => domeInfo;
            private set {
                domeInfo = value;
                RaisePropertyChanged();
            }
        }

        public void UpdateDeviceInfo(DomeInfo deviceInfo) {
            DomeInfo = deviceInfo;
        }

        private FilterWheelInfo filterWheelInfo;

        public FilterWheelInfo FilterWheelInfo {
            get => filterWheelInfo;
            private set {
                filterWheelInfo = value;
                RaisePropertyChanged();
            }
        }

        public void UpdateDeviceInfo(FilterWheelInfo deviceInfo) {
            FilterWheelInfo = deviceInfo;
        }

        private GuiderInfo guiderInfo;

        public GuiderInfo GuiderInfo {
            get => guiderInfo;
            private set {
                guiderInfo = value;
                RaisePropertyChanged();
            }
        }

        public void UpdateDeviceInfo(GuiderInfo deviceInfo) {
            GuiderInfo = deviceInfo;
        }

        private SwitchInfo switchInfo;

        public SwitchInfo SwitchInfo {
            get => switchInfo;
            private set {
                switchInfo = value;
                RaisePropertyChanged();
            }
        }

        public void UpdateDeviceInfo(SwitchInfo deviceInfo) {
            SwitchInfo = deviceInfo;
        }
    }
}