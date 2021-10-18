using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.Image.ImageAnalysis {
    public interface IStarAnnotator {
        Task<BitmapSource> GetAnnotatedImage(StarDetectionParams p, StarDetectionResult result, BitmapSource imageToAnnotate, int maxStars = 200, CancellationToken token = default);
    }
}
