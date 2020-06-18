using System.Windows.Input;
using NINA.Profile;
using NINA.ViewModel.Equipment.Camera;
using NINA.ViewModel.Equipment.Dome;
using NINA.ViewModel.Equipment.FilterWheel;
using NINA.ViewModel.Equipment.FlatDevice;
using NINA.ViewModel.Equipment.Focuser;
using NINA.ViewModel.Equipment.Guider;
using NINA.ViewModel.Equipment.Rotator;
using NINA.ViewModel.Equipment.Switch;
using NINA.ViewModel.Equipment.Telescope;
using NINA.ViewModel.Equipment.WeatherData;
using NINA.ViewModel.FlatWizard;
using NINA.ViewModel.FramingAssistant;
using NINA.ViewModel.Imaging;

namespace NINA.ViewModel.Interfaces {

    internal interface IApplicationVM {
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