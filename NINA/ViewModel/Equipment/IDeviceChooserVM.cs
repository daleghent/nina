using NINA.Model;

namespace NINA.ViewModel.Equipment {
    public interface IDeviceChooserVM {
        IDevice SelectedDevice { get; set; }

        void GetEquipment();
    }
}
