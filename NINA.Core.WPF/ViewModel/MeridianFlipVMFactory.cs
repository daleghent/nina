using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.WPF.Base.ViewModel {
    public class MeridianFlipVMFactory : IMeridianFlipVMFactory {
        private readonly IProfileService profileService;
        private readonly ITelescopeMediator telescopeMediator;
        private readonly IGuiderMediator guiderMediator;
        private readonly IImagingMediator imagingMediator;
        private readonly IDomeMediator domeMediator;
        private readonly IDomeFollower domeFollower;
        private readonly IApplicationStatusMediator applicationStatusMediator;
        private readonly IFilterWheelMediator filterWheelMediator;
        private readonly IImageHistoryVM history;
        private readonly IAutoFocusVMFactory autoFocusVMFactory;

        public MeridianFlipVMFactory(
                IProfileService profileService,
                ITelescopeMediator telescopeMediator,
                IGuiderMediator guiderMediator,
                IImagingMediator imagingMediator,
                IDomeMediator domeMediator,
                IDomeFollower domeFollower,
                IApplicationStatusMediator applicationStatusMediator,
                IFilterWheelMediator filterWheelMediator,
                IImageHistoryVM history,
                IAutoFocusVMFactory autoFocusVMFactory) {
            this.profileService = profileService;
            this.telescopeMediator = telescopeMediator;
            this.guiderMediator = guiderMediator;
            this.imagingMediator = imagingMediator;
            this.domeMediator = domeMediator;
            this.domeFollower = domeFollower;
            this.applicationStatusMediator = applicationStatusMediator;
            this.filterWheelMediator = filterWheelMediator;
            this.history = history;
            this.autoFocusVMFactory = autoFocusVMFactory;
        }

        public IMeridianFlipVM Create() {
            return new MeridianFlipVM(profileService, telescopeMediator, guiderMediator, imagingMediator, domeMediator, domeFollower, applicationStatusMediator, filterWheelMediator, history, autoFocusVMFactory);
        }
    }
}
