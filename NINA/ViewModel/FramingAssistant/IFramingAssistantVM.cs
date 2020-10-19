using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Linq;
using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Utility;
using NINA.Utility.SkySurvey;

namespace NINA.ViewModel.FramingAssistant {

    public interface IFramingAssistantVM {
        double BoundHeight { get; set; }
        double BoundWidth { get; set; }
        int CameraHeight { get; set; }
        double CameraPixelSize { get; set; }
        AsyncObservableCollection<FramingRectangle> CameraRectangles { get; set; }
        int CameraWidth { get; set; }
        ICommand CancelLoadImageCommand { get; }
        ICommand CancelLoadImageFromFileCommand { get; }
        ICommand ClearCacheCommand { get; }
        ICommand CoordsFromPlanetariumCommand { get; set; }
        int DecDegrees { get; set; }
        int DecMinutes { get; set; }
        int DecSeconds { get; set; }
        IDeepSkyObjectSearchVM DeepSkyObjectSearchVM { get; }
        int DownloadProgressValue { get; set; }
        ICommand DragMoveCommand { get; }
        ICommand DragStartCommand { get; }
        ICommand DragStopCommand { get; }
        DeepSkyObject DSO { get; set; }
        double FieldOfView { get; set; }
        double FocalLength { get; set; }
        int FontSize { get; set; }
        SkySurveySource FramingAssistantSource { get; set; }
        int HorizontalPanels { get; set; }
        XElement ImageCacheInfo { get; set; }
        SkySurveyImage ImageParameter { get; set; }
        IAsyncCommand LoadImageCommand { get; }
        ICommand MouseWheelCommand { get; }
        bool NegativeDec { get; set; }
        double Opacity { get; set; }
        double OverlapPercentage { get; set; }
        int RAHours { get; set; }
        int RAMinutes { get; set; }
        int RASeconds { get; set; }
        IAsyncCommand RecenterCommand { get; }
        FramingRectangle Rectangle { get; set; }
        ICommand RefreshSkyMapAnnotationCommand { get; }
        double Rotation { get; set; }
        ICommand ScrollViewerSizeChangedCommand { get; }
        XElement SelectedImageCacheInfo { get; set; }
        ICommand SetSequencerTargetCommand { get; }
        SkyMapAnnotator SkyMapAnnotator { get; set; }
        ISkySurveyFactory SkySurveyFactory { get; set; }
        IAsyncCommand SlewToCoordinatesCommand { get; }
        ApplicationStatus Status { get; set; }
        int VerticalPanels { get; set; }

        void Dispose();

        Task<bool> SetCoordinates(DeepSkyObject dso);

        void UpdateDeviceInfo(CameraInfo cameraInfo);
    }
}