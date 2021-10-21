using NINA.Core.Interfaces;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility {
    public class PluggableAutoFocusVMFactory : IAutoFocusVMFactory {

        private readonly IPluggableBehaviorSelector<IAutoFocusVMFactory> autoFocusVMFactorySelector;
        public PluggableAutoFocusVMFactory(IPluggableBehaviorSelector<IAutoFocusVMFactory> autoFocusVMFactorySelector) {
            this.autoFocusVMFactorySelector = autoFocusVMFactorySelector;
        }
        
        public string Name => "HIDDEN";

        public string ContentId => GetType().FullName;

        public IAutoFocusVM Create() {
            return autoFocusVMFactorySelector.GetBehavior().Create();
        }
    }
}
