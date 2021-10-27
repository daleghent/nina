using NINA.Core.Enum;
using NINA.Core.Interfaces;
using NINA.Core.Model;
using NINA.Image.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Image.ImageAnalysis {

    public interface IStarDetection : IPluggableBehavior<IStarDetection> {

        Task<StarDetectionResult> Detect(IRenderedImage image, System.Windows.Media.PixelFormat pf, StarDetectionParams p, IProgress<ApplicationStatus> progress, CancellationToken token);
        IStarDetectionAnalysis CreateAnalysis();
        void UpdateAnalysis(IStarDetectionAnalysis analysis, StarDetectionParams p, StarDetectionResult result);
    }

    public class StarDetectionParams {
        public StarSensitivityEnum Sensitivity { get; set; }
        public NoiseReductionEnum NoiseReduction { get; set; }
        public bool IsAutoFocus { get; set; }
        public bool UseROI { get; set; } = false;
        public double InnerCropRatio { get; set; } = 1.0d;
        public double OuterCropRatio { get; set; } = 1.0d;
        public int NumberOfAFStars { get; set; } = 0;
        public List<Accord.Point> MatchStarPositions { get; set; } = new List<Accord.Point>();
    }

    public class DetectedStar {
        public double HFR { get; set; }
        public Accord.Point Position { get; set; }
        public double AverageBrightness { get; set; }
        public double MaxBrightness { get; set; }
        public double Background { get; set; }
        public Rectangle BoundingBox { get; set; }
    }

    public class StarDetectionResult {
        public double AverageHFR { get; set; }
        public int DetectedStars { get; set; }
        public double HFRStdDev { get; set; }
        public List<DetectedStar> StarList { get; set; }
        public List<Accord.Point> BrightestStarPositions { get; set; }
        public StarDetectionParams Params { get; set; }
    }
}