using System.Windows.Input;
using NINA.Utility;

namespace NINA.ViewModel.Interfaces {

    public interface IThumbnailVM : IDockableVM {
        ICommand SelectCommand { get; set; }
        Thumbnail SelectedThumbnail { get; set; }
        ObservableLimitedSizedStack<Thumbnail> Thumbnails { get; set; }
    }
}