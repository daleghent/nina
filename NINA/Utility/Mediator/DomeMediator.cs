using NINA.Model.MyDome;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel.Equipment.Dome;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator {

    internal class DomeMediator : DeviceMediator<IDomeVM, IDomeConsumer, DomeInfo>, IDomeMediator {

        public Task<bool> OpenShutter(CancellationToken cancellationToken) {
            return handler.OpenShutter(cancellationToken);
        }

        public Task<bool> EnableFollowing(CancellationToken cancellationToken) {
            return handler.EnableFollowing(cancellationToken);
        }

        public Task WaitForDomeSynchronization(CancellationToken cancellationToken) {
            return handler.WaitForDomeSynchronization(cancellationToken);
        }

        public Task<bool> CloseShutter(CancellationToken cancellationToken) {
            return handler.CloseShutter(cancellationToken);
        }

        public Task<bool> Park(CancellationToken cancellationToken) {
            return handler.Park(cancellationToken);
        }

        public Task<bool> DisableFollowing(CancellationToken cancellationToken) {
            return handler.DisableFollowing(cancellationToken);
        }

        public Task<bool> SlewToAzimuth(double degrees, CancellationToken cancellationToken) {
            return handler.SlewToAzimuth(degrees, cancellationToken);
        }
    }
}