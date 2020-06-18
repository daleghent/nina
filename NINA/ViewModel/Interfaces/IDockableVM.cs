using System.Windows.Input;
using System.Windows.Media;

namespace NINA.ViewModel {

    public interface IDockableVM {
        bool CanClose { get; set; }
        string ContentId { get; }
        ICommand HideCommand { get; }
        GeometryGroup ImageGeometry { get; set; }
        bool IsClosed { get; set; }
        bool IsVisible { get; set; }
        string Title { get; set; }

        void Hide(object o);
    }
}