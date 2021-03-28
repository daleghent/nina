using NINA.Model;
using NINA.Model.MyTelescope;
using NINA.Utility.Astrometry;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace NINA.ViewModel.FramingAssistant {
    public interface ISkyMapAnnotator {
        bool AnnotateConstellationBoundaries { get; set; }
        bool AnnotateConstellations { get; set; }
        bool AnnotateDSO { get; set; }
        bool AnnotateGrid { get; set; }
        List<FramingConstellationBoundary> ConstellationBoundariesInViewPort { get; }
        List<FramingConstellation> ConstellationsInViewport { get; }
        ICommand DragCommand { get; }
        List<FramingDSO> DSOInViewport { get; }
        bool DynamicFoV { get; set; }
        FrameLineMatrix2 FrameLineMatrix { get; }
        bool Initialized { get; }
        BitmapSource SkyMapOverlay { get; set; }
        ViewportFoV ViewportFoV { get; }

        void CalculateConstellationBoundaries();
        void CalculateFrameLineMatrix();
        ViewportFoV ChangeFoV(double vFoVDegrees);
        void ClearFrameLineMatrix();
        void Dispose();
        Dictionary<string, DeepSkyObject> GetDeepSkyObjectsForViewport();
        Task Initialize(Coordinates centerCoordinates, double vFoVDegrees, double imageWidth, double imageHeight, double imageRotation, CancellationToken ct);
        Coordinates ShiftViewport(Vector delta);
        void UpdateDeviceInfo(TelescopeInfo deviceInfo);
        void UpdateSkyMap();
    }
}