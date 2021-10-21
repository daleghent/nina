using NINA.Core.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.Image.ImageAnalysis {
    public interface IStarAnnotator : IPluggableBehavior<IStarAnnotator> {
        Task<BitmapSource> GetAnnotatedImage(StarDetectionParams p, StarDetectionResult result, BitmapSource imageToAnnotate, int maxStars = 200, CancellationToken token = default);
    }
}
