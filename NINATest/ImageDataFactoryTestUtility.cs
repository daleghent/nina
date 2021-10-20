using Moq;
using NINA.Image.ImageAnalysis;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.Image.RawConverter;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINATest {
    public class ImageDataFactoryTestUtility {

        public ImageDataFactoryTestUtility() {
            this.ProfileServiceMock = new Mock<IProfileService>();
            this.StarAnnotator = new StarAnnotator();
            this.StarDetectionMock = new Mock<IStarDetection>();
            this.ImageDataFactory = new ImageDataFactory(this.ProfileServiceMock.Object, this.StarDetectionMock.Object, this.StarAnnotator);
            this.ExposureDataFactory = new ExposureDataFactory(this.ImageDataFactory, this.ProfileServiceMock.Object, this.StarDetectionMock.Object, this.StarAnnotator);
        }

        public IExposureDataFactory ExposureDataFactory { get; set; }
        public IImageDataFactory ImageDataFactory { get; set; }
        public IStarAnnotator StarAnnotator { get; set; }
        public Mock<IStarDetection> StarDetectionMock { get; private set; }
        public Mock<IProfileService> ProfileServiceMock { get; private set; }
    }
}
