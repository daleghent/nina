#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Accord.Imaging.Filters;
using FluentAssertions;
using Moq;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Model;
using NINA.Image.FileFormat.XISF;
using NINA.Image.ImageAnalysis;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.PlateSolving.Interfaces;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.ViewModel.AutoFocus;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINATest.Autofocus {

    [TestFixture]
    public class AutofocusVMTest {
        private Mock<IProfileService> profileServiceMock;
        private Mock<ICameraMediator> cameraMediatorMock;
        private Mock<IFilterWheelMediator> fwMediatorMock;
        private Mock<IFocuserMediator> focuserMediatorMock;
        private Mock<IGuiderMediator> guiderMediatorMock;
        private Mock<IImagingMediator> imagingMediatorMock;
        private IRenderedImage renderedTestImage;
        private IRenderedImage blurred1xRenderedImage;
        private IRenderedImage blurred2xRenderedImage;
        private IRenderedImage blurred3xRenderedImage;

        [OneTimeSetUp]
        public async Task OneTimeSetUp() {
            profileServiceMock = new Mock<IProfileService>();
            cameraMediatorMock = new Mock<ICameraMediator>();
            fwMediatorMock = new Mock<IFilterWheelMediator>();
            focuserMediatorMock = new Mock<IFocuserMediator>();
            guiderMediatorMock = new Mock<IGuiderMediator>();
            imagingMediatorMock = new Mock<IImagingMediator>();

            var testImageData = await XISF.Load(new Uri(Path.Combine(TestContext.CurrentContext.TestDirectory, "Autofocus", "TestImage_Jelly.xisf")), false, default);
            renderedTestImage = testImageData.RenderImage();

            var bmp = ImageUtility.BitmapFromSource(renderedTestImage.Image);
            var position1Bmp = new Blur().Apply(bmp);
            var position2Bmp = new Blur().Apply(position1Bmp);
            var position3Bmp = new Blur().Apply(position2Bmp);

            var source1 = ImageUtility.ConvertBitmap(position1Bmp);
            source1.Freeze();
            var source2 = ImageUtility.ConvertBitmap(position2Bmp);
            source2.Freeze();
            var source3 = ImageUtility.ConvertBitmap(position3Bmp);
            source3.Freeze();
            blurred1xRenderedImage = await RenderedImage.FromBitmapSource(source1);
            blurred2xRenderedImage = await RenderedImage.FromBitmapSource(source2);
            blurred3xRenderedImage = await RenderedImage.FromBitmapSource(source3);
        }

        [SetUp]
        public void Setup() {
            profileServiceMock.Reset();
            cameraMediatorMock.Reset();
            fwMediatorMock.Reset();
            focuserMediatorMock.Reset();
            guiderMediatorMock.Reset();
            imagingMediatorMock.Reset();
        }

        [Test]
        public async Task StartAutofocus_Trendlines_IdealRun_FiveMeasurePoints_FocusStillAtInitialPosition() {
            var ct = new CancellationToken();

            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusMethod).Returns(AFMethodEnum.STARHFR);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusCurveFitting).Returns(AFCurveFittingEnum.TRENDLINES);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusNumberOfFramesPerPoint).Returns(1);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusInnerCropRatio).Returns(1);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusOuterCropRatio).Returns(1);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusInitialOffsetSteps).Returns(2);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusStepSize).Returns(100);

            profileServiceMock.SetupGet(x => x.ActiveProfile.ImageSettings.StarSensitivity).Returns(StarSensitivityEnum.Normal);
            profileServiceMock.SetupGet(x => x.ActiveProfile.ImageSettings.NoiseReduction).Returns(NoiseReductionEnum.None);

            int initialPosition = 5000;
            focuserMediatorMock.Setup(x => x.GetInfo()).Returns(new FocuserInfo() { Connected = true, Position = initialPosition });
            int position = initialPosition;
            focuserMediatorMock
                .Setup(x => x.MoveFocuserRelative(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int y, CancellationToken c) => { position = position + y; return position; });
            focuserMediatorMock
                .Setup(x => x.MoveFocuser(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int y, CancellationToken c) => { position = y; return position; });

            imagingMediatorMock
                .SetupSequence(x => x.CaptureAndPrepareImage(It.IsAny<CaptureSequence>(), It.IsAny<PrepareImageParameters>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>()))
                .ReturnsAsync(renderedTestImage)
                .ReturnsAsync(blurred2xRenderedImage)
                .ReturnsAsync(blurred1xRenderedImage)
                .ReturnsAsync(renderedTestImage)
                .ReturnsAsync(blurred1xRenderedImage)
                .ReturnsAsync(blurred2xRenderedImage)
                .ReturnsAsync(renderedTestImage);

            var sut = new AutoFocusVM(profileServiceMock.Object, cameraMediatorMock.Object, fwMediatorMock.Object, focuserMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object);

            var imagingFilter = new FilterInfo();

            var report = await sut.StartAutoFocus(imagingFilter, ct, new Progress<ApplicationStatus>());

            position.Should().Be(5000);
            report.CalculatedFocusPoint.Position.Should().Be(5000);
            report.MeasurePoints.Should().HaveCount(5);
            imagingMediatorMock.Verify(x => x.CaptureAndPrepareImage(It.IsAny<CaptureSequence>(), It.IsAny<PrepareImageParameters>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>()), Times.Exactly(7));
        }

        [Test]
        public async Task StartAutofocus_Trendlines_WithCameraDownloadFailures_FiveMeasurePoints_FocusStillAtInitialPosition() {
            var ct = new CancellationToken();

            var testImageData = await XISF.Load(new Uri(Path.Combine(TestContext.CurrentContext.TestDirectory, "Autofocus", "TestImage_Jelly.xisf")), false, ct);
            var renderedTestImage = testImageData.RenderImage();

            var bmp = ImageUtility.BitmapFromSource(renderedTestImage.Image);
            var position1Bmp = new Blur().Apply(bmp);
            var position2Bmp = new Blur().Apply(position1Bmp);

            var source1 = ImageUtility.ConvertBitmap(position1Bmp);
            source1.Freeze();
            var source2 = ImageUtility.ConvertBitmap(position2Bmp);
            source2.Freeze();
            var blurredRenderedImage1 = await RenderedImage.FromBitmapSource(source1);
            var blurredRenderedImage2 = await RenderedImage.FromBitmapSource(source2);

            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusMethod).Returns(AFMethodEnum.STARHFR);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusCurveFitting).Returns(AFCurveFittingEnum.TRENDLINES);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusNumberOfFramesPerPoint).Returns(1);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusInnerCropRatio).Returns(1);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusOuterCropRatio).Returns(1);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusInitialOffsetSteps).Returns(2);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusStepSize).Returns(100);

            profileServiceMock.SetupGet(x => x.ActiveProfile.ImageSettings.StarSensitivity).Returns(StarSensitivityEnum.Normal);
            profileServiceMock.SetupGet(x => x.ActiveProfile.ImageSettings.NoiseReduction).Returns(NoiseReductionEnum.None);

            int initialPosition = 5000;
            focuserMediatorMock.Setup(x => x.GetInfo()).Returns(new FocuserInfo() { Connected = true, Position = initialPosition });
            int position = initialPosition;
            focuserMediatorMock
                .Setup(x => x.MoveFocuserRelative(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int y, CancellationToken c) => { position = position + y; return position; });
            focuserMediatorMock
                .Setup(x => x.MoveFocuser(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int y, CancellationToken c) => { position = y; return position; });

            imagingMediatorMock
                .SetupSequence(x => x.CaptureAndPrepareImage(It.IsAny<CaptureSequence>(), It.IsAny<PrepareImageParameters>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>()))
                .ReturnsAsync(renderedTestImage)
                .ReturnsAsync((IRenderedImage)null)
                .ReturnsAsync(blurredRenderedImage2)
                .ReturnsAsync(blurredRenderedImage1)
                .ReturnsAsync(renderedTestImage)
                .ReturnsAsync((IRenderedImage)null)
                .ReturnsAsync((IRenderedImage)null)
                .ReturnsAsync(blurredRenderedImage1)
                .ReturnsAsync(blurredRenderedImage2)
                .ReturnsAsync(renderedTestImage);

            var sut = new AutoFocusVM(profileServiceMock.Object, cameraMediatorMock.Object, fwMediatorMock.Object, focuserMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object);

            var imagingFilter = new FilterInfo();

            var report = await sut.StartAutoFocus(imagingFilter, ct, new Progress<ApplicationStatus>());

            position.Should().Be(5000);
            report.CalculatedFocusPoint.Position.Should().Be(5000);
            report.MeasurePoints.Should().HaveCount(5);
            imagingMediatorMock.Verify(x => x.CaptureAndPrepareImage(It.IsAny<CaptureSequence>(), It.IsAny<PrepareImageParameters>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>()), Times.Exactly(10));
        }

        [Test]
        public async Task StartAutofocus_TrendHyperbolic_ShiftedRightRun_FiveMeasurePoints_FocusStillAtInitialPosition() {
            var ct = new CancellationToken();

            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusMethod).Returns(AFMethodEnum.STARHFR);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusCurveFitting).Returns(AFCurveFittingEnum.TRENDHYPERBOLIC);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusNumberOfFramesPerPoint).Returns(1);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusInnerCropRatio).Returns(1);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusOuterCropRatio).Returns(1);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusInitialOffsetSteps).Returns(2);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusStepSize).Returns(100);

            profileServiceMock.SetupGet(x => x.ActiveProfile.ImageSettings.StarSensitivity).Returns(StarSensitivityEnum.Normal);
            profileServiceMock.SetupGet(x => x.ActiveProfile.ImageSettings.NoiseReduction).Returns(NoiseReductionEnum.None);

            int initialPosition = 5100;
            focuserMediatorMock.Setup(x => x.GetInfo()).Returns(new FocuserInfo() { Connected = true, Position = initialPosition });
            int position = initialPosition;
            focuserMediatorMock
                .Setup(x => x.MoveFocuserRelative(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int y, CancellationToken c) => { position = position + y; return position; });
            focuserMediatorMock
                .Setup(x => x.MoveFocuser(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int y, CancellationToken c) => { position = y; return position; });

            imagingMediatorMock
                .SetupSequence(x => x.CaptureAndPrepareImage(It.IsAny<CaptureSequence>(), It.IsAny<PrepareImageParameters>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>()))
                .ReturnsAsync(blurred1xRenderedImage)
                .ReturnsAsync(blurred3xRenderedImage)
                .ReturnsAsync(blurred2xRenderedImage)
                .ReturnsAsync(blurred1xRenderedImage)
                .ReturnsAsync(renderedTestImage)
                .ReturnsAsync(blurred1xRenderedImage)
                .ReturnsAsync(blurred2xRenderedImage)
                .ReturnsAsync(renderedTestImage);

            var sut = new AutoFocusVM(profileServiceMock.Object, cameraMediatorMock.Object, fwMediatorMock.Object, focuserMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object);

            var imagingFilter = new FilterInfo();

            var report = await sut.StartAutoFocus(imagingFilter, ct, new Progress<ApplicationStatus>());

            position.Should().Be(5001);
            report.CalculatedFocusPoint.Position.Should().Be(5001);
            report.MeasurePoints.Should().HaveCount(6);
            imagingMediatorMock.Verify(x => x.CaptureAndPrepareImage(It.IsAny<CaptureSequence>(), It.IsAny<PrepareImageParameters>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>()), Times.Exactly(8));
        }
    }
}