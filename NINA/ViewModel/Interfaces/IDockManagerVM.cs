using NINA.Utility;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace NINA.ViewModel.Interfaces {

    public interface IDockManagerVM {
        ObservableCollection<IDockableVM> AnchorableInfoPanels { get; }
        ObservableCollection<IDockableVM> Anchorables { get; }
        ObservableCollection<IDockableVM> AnchorableTools { get; }
        IAsyncCommand LoadAvalonDockLayoutCommand { get; }
        ICommand ResetDockLayoutCommand { get; }

        bool LoadAvalonDockLayout(object o);

        void SaveAvalonDockLayout();
    }
}