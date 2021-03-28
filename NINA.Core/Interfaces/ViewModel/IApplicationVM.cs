using NINA.Core.Enum;
using System.Windows.Input;

namespace NINA.ViewModel.Interfaces {

    public interface IApplicationVM {
        ICommand CheckASCOMPlatformVersionCommand { get; }
        ICommand CheckProfileCommand { get; }
        ICommand CheckUpdateCommand { get; }
        ICommand ClosingCommand { get; }
        ICommand ExitCommand { get; }
        ICommand MaximizeWindowCommand { get; }
        ICommand MinimizeWindowCommand { get; }
        ICommand OpenManualCommand { get; }
        int TabIndex { get; set; }
        string Title { get; }
        string Version { get; }

        void ChangeTab(ApplicationTab tab);
    }
}