using NINA.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility {
    /// <summary>
    /// This class is a container for objects that have global scope and need to be instantiated at application startup. Bindings must be resolvable from IoCBindings
    /// </summary>
    public class GlobalObjects {
        private readonly PluggableBehaviorManager pluggableBehaviorManager;

        public GlobalObjects(PluggableBehaviorManager pluggableBehaviorManager) {
            this.pluggableBehaviorManager = pluggableBehaviorManager;
        }
    }
}
