using NINA.Astrometry;
using NINA.Core.Model;
using NINA.Core.Utility.WindowService;
using NINA.WPF.Base.ViewModel;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.WPF.Base.Interfaces.ViewModel {
    public interface IMeridianFlipVM {
        ICommand CancelCommand { get; set; }
        TimeSpan RemainingTime { get; set; }
        ApplicationStatus Status { get; set; }
        AutomatedWorkflow Steps { get; set; }
        IWindowServiceFactory WindowServiceFactory { get; set; }
        Task<bool> MeridianFlip(Coordinates targetCoordinates, TimeSpan timeToFlip, CancellationToken cancellationToken = default);
    }
}
