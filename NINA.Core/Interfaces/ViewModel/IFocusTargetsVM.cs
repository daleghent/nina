using System.Collections.ObjectModel;
using NINA.Model;
using NINA.Model.MyTelescope;
using NINA.Utility;

namespace NINA.ViewModel.Interfaces {

    public interface IFocusTargetsVM : IDockableVM {
        ObservableCollection<FocusTarget> FocusTargets { get; set; }
        FocusTarget SelectedFocusTarget { get; set; }
        IAsyncCommand SlewToCoordinatesCommand { get; }
        bool TelescopeConnected { get; set; }

        void Dispose();

        void UpdateDeviceInfo(TelescopeInfo deviceInfo);
    }
}