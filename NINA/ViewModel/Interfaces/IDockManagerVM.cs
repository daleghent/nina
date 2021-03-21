using NINA.Utility;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace NINA.ViewModel.Interfaces {

    public interface IDockManagerVM {
        List<IDockableVM> AnchorableInfoPanels { get; }
        List<IDockableVM> Anchorables { get; }
        List<IDockableVM> AnchorableTools { get; }
        IAsyncCommand LoadAvalonDockLayoutCommand { get; }
        ICommand ResetDockLayoutCommand { get; }

        bool InitializeAvalonDockLayout(object o);

        void SaveAvalonDockLayout();
    }
}