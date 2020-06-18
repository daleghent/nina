using System.Windows.Input;

namespace NINA.ViewModel.Interfaces {

    internal interface IApplicationDeviceConnectionVM {
        ICommand ClosingCommand { get; }
        ICommand ConnectAllDevicesCommand { get; }
        ICommand DisconnectAllDevicesCommand { get; }

        void DisconnectEquipment();
    }
}