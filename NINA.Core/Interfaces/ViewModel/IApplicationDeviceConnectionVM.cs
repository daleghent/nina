using System.Windows.Input;

namespace NINA.ViewModel.Interfaces {

    public interface IApplicationDeviceConnectionVM {
        ICommand ClosingCommand { get; }
        ICommand ConnectAllDevicesCommand { get; }
        ICommand DisconnectAllDevicesCommand { get; }

        void DisconnectEquipment();
    }
}