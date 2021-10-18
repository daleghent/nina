using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Image.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Image.ImageAnalysis {
    public interface IContrastDetection {

        Task<ContrastDetectionResult> Measure(IRenderedImage image, ContrastDetectionParams p, IProgress<ApplicationStatus> progress, CancellationToken token);
    }

    public class ContrastDetectionParams {
        public StarSensitivityEnum Sensitivity { get; set; }
        public NoiseReductionEnum NoiseReduction { get; set; }
        public ContrastDetectionMethodEnum Method { get; set; }
        public bool UseROI { get; set; }
        public double InnerCropRatio { get; set; }
    }

    public class ContrastDetectionResult {
        public double AverageContrast { get; set; }
        public double ContrastStdev { get; set; }
    }
}
