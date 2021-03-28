using NINA.Model;
using NINA.Model.ImageData;
using NINA.Model.MyCamera;
using NINA.Utility;
using NINA.Utility.ImageAnalysis;
using NINA.Utility.Mediator;
using NINA.Utility.WindowService;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace NINA.ViewModel.Interfaces {

    public interface IImageControlVM : IDockableVM {
        bool AutoStretch { get; set; }
        BahtinovImage BahtinovImage { get; }
        ObservableRectangle BahtinovRectangle { get; set; }
        ICommand CancelPlateSolveImageCommand { get; }
        bool DetectStars { get; set; }
        ICommand DragMoveCommand { get; }
        double DragResizeBoundary { get; }
        ICommand DragStartCommand { get; }
        ICommand DragStopCommand { get; }
        BitmapSource Image { get; set; }
        IAsyncCommand InspectAberrationCommand { get; }
        bool IsLiveViewEnabled { get; }
        IAsyncCommand PlateSolveImageCommand { get; }
        AsyncCommand<bool> PrepareImageCommand { get; }
        IRenderedImage RenderedImage { get; set; }
        bool ShowBahtinovAnalyzer { get; set; }
        bool ShowCrossHair { get; set; }
        bool ShowSubSampler { get; set; }
        ApplicationStatus Status { get; set; }
        ICommand SubSampleDragMoveCommand { get; }
        ICommand SubSampleDragStartCommand { get; }
        ICommand SubSampleDragStopCommand { get; }
        ObservableRectangle SubSampleRectangle { get; set; }
        IWindowServiceFactory WindowServiceFactory { get; set; }

        void Dispose();

        Task<IRenderedImage> PrepareImage(IImageData data, PrepareImageParameters parameters, CancellationToken cancelToken);

        void UpdateDeviceInfo(CameraInfo cameraInfo);
    }
}