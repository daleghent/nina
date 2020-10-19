using NINA.Model.MyDome;
using NINA.ViewModel.Equipment.Dome;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator.Interfaces {
    public interface IDomeMediator : IDeviceMediator<IDomeVM, IDomeConsumer, DomeInfo> {
        Task WaitForDomeSynchronization(CancellationToken cancellationToken);
        Task<bool> OpenShutter(CancellationToken cancellationToken);
        Task<bool> CloseShutter(CancellationToken cancellationToken);
        Task<bool> EnableFollowing(CancellationToken cancellationToken);
        Task<bool> DisableFollowing(CancellationToken cancellationToken);
        Task<bool> Park(CancellationToken cancellationToken);
    }
}
