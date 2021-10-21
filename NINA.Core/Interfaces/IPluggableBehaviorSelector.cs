using NINA.Core.Utility;
using System;

namespace NINA.Core.Interfaces {
    
    public interface IPluggableBehaviorSelector {
        Type GetInterfaceType();

        void AddBehavior(object behavior);
    }

    public interface IPluggableBehaviorSelector<T> : IPluggableBehaviorSelector {
        T GetBehavior();

        AsyncObservableCollection<T> Behaviors { get; set; }

        T SelectedBehavior { get; set; }
    }
}
