using NINA.Utility;
using NINA.Utility.WindowService;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel {

    public interface IVersionCheckVM {
        ICommand CancelDownloadCommand { get; set; }
        string Changelog { get; set; }
        IAsyncCommand DownloadCommand { get; set; }
        bool Downloading { get; set; }
        int Progress { get; set; }
        IAsyncCommand ShowDownloadCommand { get; set; }
        bool UpdateAvailable { get; set; }
        string UpdateAvailableText { get; set; }
        ICommand UpdateCommand { get; set; }
        bool UpdateReady { get; set; }
        IWindowServiceFactory WindowServiceFactory { get; set; }

        Task<bool> CheckUpdate();
    }
}