using System.Collections.ObjectModel;
using System.Windows.Input;

namespace NINA.ViewModel.Interfaces {

    public interface IDockManagerVM {
        ObservableCollection<IDockableVM> AnchorableInfoPanels { get; }
        ObservableCollection<IDockableVM> Anchorables { get; }
        ObservableCollection<IDockableVM> AnchorableTools { get; }
        ICommand LoadAvalonDockLayoutCommand { get; }
        ICommand ResetDockLayoutCommand { get; }

        void LoadAvalonDockLayout(object o);

        void SaveAvalonDockLayout();
    }
}