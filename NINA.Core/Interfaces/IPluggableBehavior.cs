using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Core.Interfaces {

    /// <summary>
    /// Base interface for pluggable behaviors whose instances can be reused. This interface
    /// exists to simplify collecting all IPluggableBehaviors without knowing each generic parameter
    /// </summary>
    public interface IPluggableBehavior {
        string Name { get; }
        string ContentId { get; }
    }

    public interface IPluggableBehavior<T> : IPluggableBehavior {
    }
}
