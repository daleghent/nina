#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using NINA.Core.Model;
using NINA.Image.ImageAnalysis;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;

namespace NINA.Image.ImageData {

    public class RenderedImage : IRenderedImage {
        protected readonly IProfileService profileService;
        protected readonly IStarDetection starDetection;
        protected readonly IStarAnnotator starAnnotator;

        public IImageData RawImageData { get; private set; }

        public BitmapSource Image { get; private set; }

        public RenderedImage(BitmapSource image, IImageData rawImageData, IProfileService profileService, IStarDetection starDetection, IStarAnnotator starAnnotator) {
            this.Image = image;
            this.RawImageData = rawImageData;
            this.profileService = profileService;
            this.starDetection = starDetection;
            this.starAnnotator = starAnnotator;
        }

        public static async Task<IRenderedImage> FromBitmapSource(BitmapSource source, IExposureDataFactory exposureDataFactory, IProfileService profileService, IStarDetection starDetection, IStarAnnotator starAnnotator, bool calculateStatistics = false) {
            var exposureData = await exposureDataFactory.CreateImageArrayExposureDataFromBitmapSource(source);
            var rawImageData = await exposureData.ToImageData();
            return Create(source, rawImageData, profileService, starDetection, starAnnotator, calculateStatistics: calculateStatistics);
        }

        public static RenderedImage Create(BitmapSource source, IImageData rawImageData, IProfileService profileService, IStarDetection starDetection, IStarAnnotator starAnnotator, bool calculateStatistics = false) {
            return new RenderedImage(source, rawImageData, profileService, starDetection, starAnnotator);
        }

        public virtual IRenderedImage ReRender() {
            return new RenderedImage(this.RawImageData.RenderBitmapSource(), this.RawImageData, profileService, starDetection, starAnnotator);
        }

        public IDebayeredImage Debayer(bool saveColorChannels = false, bool saveLumChannel = false, SensorType bayerPattern = SensorType.RGGB) {
            return DebayeredImage.Debayer(this, profileService, starDetection, starAnnotator, saveColorChannels: saveColorChannels, saveLumChannel: saveLumChannel, bayerPattern: bayerPattern);
        }

        public virtual async Task<IRenderedImage> Stretch(double factor, double blackClipping, bool unlinked) {
            var stretchedImage = await ImageUtility.Stretch(this, factor, blackClipping);
            return new RenderedImage(stretchedImage, this.RawImageData, profileService, starDetection, starAnnotator);
        }

        public async Task<IRenderedImage> DetectStars(
            bool annotateImage,
            StarSensitivityEnum sensitivity,
            NoiseReductionEnum noiseReduction,
            CancellationToken cancelToken = default,
            IProgress<ApplicationStatus> progress = default(Progress<ApplicationStatus>)) {
            var starDetectionParams = new StarDetectionParams() {
                Sensitivity = sensitivity,
                NoiseReduction = noiseReduction
            };
            var starDetectionResult = await starDetection.Detect(this, this.Image.Format, starDetectionParams, progress, cancelToken);
            var image = this.Image;
            if (annotateImage) {
                // TODO: Apply ROI and MaxAFStars settings from the profile so measured HFRs match up with those during autofocus
                var maxStars = profileService.ActiveProfile.ImageSettings.AnnotateUnlimitedStars ? -1 : 200;
                image = await starAnnotator.GetAnnotatedImage(starDetectionParams, starDetectionResult, this.Image, maxStars: maxStars, token: cancelToken);
            }

            this.RawImageData.StarDetectionAnalysis.HFR = starDetectionResult.AverageHFR;
            this.RawImageData.StarDetectionAnalysis.HFRStDev = starDetectionResult.HFRStdDev;
            this.RawImageData.StarDetectionAnalysis.DetectedStars = starDetectionResult.DetectedStars;
            return new RenderedImage(image, this.RawImageData, profileService, starDetection, starAnnotator);
        }

        public async Task<BitmapSource> GetThumbnail() {
            BitmapSource image = null;
            await _dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                var factor = 300 / this.Image.Width;
                image = new WriteableBitmap(new TransformedBitmap(this.Image, new ScaleTransform(factor, factor)));
                image.Freeze();
            }));
            return image;
        }

        private static Dispatcher _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
    }
}