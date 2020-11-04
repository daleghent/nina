#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using Moq;
using NINA.Model;
using NINA.Model.ImageData;
using NINA.Model.MyCamera;
using NINA.Sequencer;
using NINA.Sequencer.Exceptions;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Utility.Mediator;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel.ImageHistory;
using Nito.AsyncEx;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Security.Tokens;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINATest.Sequencer.SequenceItem.Imaging {

    [TestFixture]
    internal class TakeExposureTest {
        private Mock<ICameraMediator> cameraMediatorMock;
        private Mock<IImagingMediator> imagingMediatorMock;
        private Mock<IImageSaveMediator> imageSaveMediatorMock;
        private Mock<IImageHistoryVM> historyMock;

        [SetUp]
        public void Setup() {
            cameraMediatorMock = new Mock<ICameraMediator>();
            imagingMediatorMock = new Mock<IImagingMediator>();
            imageSaveMediatorMock = new Mock<IImageSaveMediator>();
            historyMock = new Mock<IImageHistoryVM>();
        }

        [Test]
        public void Clone_ItemClonedProperly() {
            var sut = new TakeExposure(cameraMediatorMock.Object, imagingMediatorMock.Object, imageSaveMediatorMock.Object, historyMock.Object);
            sut.Name = "SomeName";
            sut.Description = "SomeDescription";
            sut.Icon = new System.Windows.Media.GeometryGroup();
            var item2 = (TakeExposure)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Name.Should().BeSameAs(sut.Name);
            item2.Description.Should().BeSameAs(sut.Description);
            item2.Icon.Should().BeSameAs(sut.Icon);
            item2.Binning.Should().NotBeNull();
            item2.ExposureCount.Should().Be(0);
            item2.ExposureTime.Should().Be(sut.ExposureCount);
            item2.Gain.Should().Be(sut.Gain);
            item2.Offset.Should().Be(sut.Offset);
            item2.ImageType.Should().Be(sut.ImageType);
        }

        [Test]
        public void Validate_NoIssues() {
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = true });

            var sut = new TakeExposure(cameraMediatorMock.Object, imagingMediatorMock.Object, imageSaveMediatorMock.Object, historyMock.Object);
            var valid = sut.Validate();

            valid.Should().BeTrue();

            sut.Issues.Should().BeEmpty();
        }

        [Test]
        public void Validate_NotConnected_OneIssue() {
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = false });

            var sut = new TakeExposure(cameraMediatorMock.Object, imagingMediatorMock.Object, imageSaveMediatorMock.Object, historyMock.Object);
            var valid = sut.Validate();

            valid.Should().BeFalse();

            sut.Issues.Should().HaveCount(1);
        }

        [Test]
        [TestCase(100, 10, 20, 1, "LIGHT", true, true, 1)]
        [TestCase(5.2, 1, 3, 2, "SNAPSHOT", true, true, 1)]
        [TestCase(100, 10, 20, 1, "DARK", null, false, 0)]
        [TestCase(100, 10, 20, 1, "FLAT", null, false, 0)]
        public async Task Execute_NoIssues_LogicCalled(double exposuretime, int gain, int offset, short binning, string imageType, bool? expectedStretch, bool? expectedDetect, int historycalls) {
            var imageMock = new Mock<IExposureData>();
            var imageDataMock = new Mock<IImageData>();
            var stats = new Mock<IImageStatistics>();
            imageDataMock.SetupGet(x => x.Statistics).Returns(new AsyncLazy<IImageStatistics>(() => Task.FromResult(stats.Object)));
            imageMock.Setup(x => x.ToImageData(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(imageDataMock.Object));
            var imageTask = Task.FromResult(imageMock.Object);
            var prepareTask = Task.FromResult(new Mock<IRenderedImage>().Object);
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = true });
            imagingMediatorMock.Setup(x => x.CaptureImage(It.IsAny<CaptureSequence>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<string>())).Returns(imageTask);
            imagingMediatorMock.Setup(x => x.PrepareImage(It.IsAny<IImageData>(), It.IsAny<PrepareImageParameters>(), It.IsAny<CancellationToken>())).Returns(prepareTask);

            var sut = new TakeExposure(cameraMediatorMock.Object, imagingMediatorMock.Object, imageSaveMediatorMock.Object, historyMock.Object);
            sut.ExposureTime = exposuretime;
            sut.Gain = gain;
            sut.Offset = offset;
            sut.Binning = new BinningMode(binning, binning);
            sut.ImageType = imageType;

            await sut.Execute(default, default);

            imagingMediatorMock.Verify(
                x => x.CaptureImage(
                    It.Is<CaptureSequence>(
                        cs =>
                            cs.ExposureTime == exposuretime
                            && cs.Binning.X == binning
                            && cs.Gain == gain
                            && cs.Offset == offset
                            && cs.ImageType == imageType
                            && cs.ProgressExposureCount == 0
                            && cs.TotalExposureCount == 1
                    ),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IProgress<ApplicationStatus>>(),
                    It.IsAny<string>()
                ), Times.Once);

            imagingMediatorMock.Verify(x => x.PrepareImage(It.Is<IImageData>(e => e == imageDataMock.Object), It.Is<PrepareImageParameters>(y => y.AutoStretch == expectedStretch && y.DetectStars == expectedDetect), It.IsAny<CancellationToken>()), Times.Once);

            imageSaveMediatorMock.Verify(x => x.Enqueue(It.Is<IImageData>(d => d == imageDataMock.Object), It.Is<Task<IRenderedImage>>(t => t == prepareTask), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
            historyMock.Verify(x => x.Add(It.Is<IImageStatistics>(s => s == stats.Object)), Times.Exactly(historycalls));
        }

        [Test]
        public Task Execute_HasIssues_LogicNotCalled() {
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = false });

            var sut = new TakeExposure(cameraMediatorMock.Object, imagingMediatorMock.Object, imageSaveMediatorMock.Object, historyMock.Object);
            Func<Task> act = () => { return sut.Execute(default, default); };

            return act.Should().ThrowAsync<SequenceItemSkippedException>(string.Join(",", sut.Issues));
        }

        [Test]
        [TestCase(1)]
        [TestCase(100)]
        [TestCase(0)]
        [TestCase(0.12)]
        public void GetEstimatedDuration_BasedOnParameters_ReturnsCorrectEstimate(double exposuretime) {
            var sut = new TakeExposure(cameraMediatorMock.Object, imagingMediatorMock.Object, imageSaveMediatorMock.Object, historyMock.Object);
            sut.ExposureTime = exposuretime;

            var duration = sut.GetEstimatedDuration();

            duration.Should().Be(TimeSpan.FromSeconds(exposuretime));
        }
    }
}