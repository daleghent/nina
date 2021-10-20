#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility;
using NINA.Profile.Interfaces;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using NINA.Equipment.Equipment.MyCamera;
using NINA.ViewModel.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Core.Utility;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Image;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.ViewModel;
using NINA.Core.Locale;
using NINA.Image.Interfaces;

namespace NINA.ViewModel {

    internal class ThumbnailVM : DockableVM, IThumbnailVM {

        public ThumbnailVM(IProfileService profileService, IImagingMediator imagingMediator, IImageSaveMediator imageSaveMediator, IImageDataFactory imageDataFactory) : base(profileService) {
            Title = Loc.Instance["LblImageHistory"];
            CanClose = false;
            ImageGeometry = (GeometryGroup)System.Windows.Application.Current.Resources["HistorySVG"];

            this.imagingMediator = imagingMediator;
            this.imageSaveMediator = imageSaveMediator;
            this.imageDataFactory = imageDataFactory;

            this.imageSaveMediator.ImageSaved += ImageSaveMediator_ImageSaved;

            SelectCommand = new AsyncCommand<bool>((object o) => {
                return SelectImage((Thumbnail)o);
            });
            GradeImageCommand = new AsyncCommand<bool>(GradeImage);
        }

        private Task<bool> GradeImage(object arg) {
            return Task.Run(() => {
                if (arg is Thumbnail) {
                    var selected = arg as Thumbnail;
                    //Order is from "" -> "BAD" -> ""
                    switch (selected.Grade) {
                        case "": {
                                selected.ChangeGrade("BAD");
                                break;
                            }
                        case "BAD": {
                                selected.ChangeGrade("");
                                break;
                            }
                    }
                    return true;
                }
                return false;
            });
        }

        private void ImageSaveMediator_ImageSaved(object sender, ImageSavedEventArgs e) {
            AddThumbnail(e);
        }

        private Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

        private Task<bool> AddThumbnail(ImageSavedEventArgs msg) {
            return Task.Run(async () => {
                var factor = 100 / msg.Image.Width;

                var scaledBitmap = CreateResizedImage(msg.Image, (int)(msg.Image.Width * factor), (int)(msg.Image.Height * factor), 0);
                scaledBitmap.Freeze();

                await _dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                    var thumbnail = new Thumbnail(imageDataFactory) {
                        ThumbnailImage = scaledBitmap,
                        ImagePath = msg.PathToImage,
                        FileType = msg.FileType,
                        Duration = msg.Duration,
                        Mean = msg.Statistics.Mean,
                        HFR = msg.StarDetectionAnalysis.HFR,
                        Filter = msg.Filter,
                        IsBayered = msg.IsBayered
                    };
                    Thumbnails.Add(thumbnail);
                    SelectedThumbnail = thumbnail;
                }));
                return true;
            });
        }

        private static BitmapFrame CreateResizedImage(ImageSource source, int width, int height, int margin) {
            var rect = new System.Windows.Rect(margin, margin, width - margin * 2, height - margin * 2);

            var group = new DrawingGroup();
            RenderOptions.SetBitmapScalingMode(group, BitmapScalingMode.HighQuality);
            group.Children.Add(new ImageDrawing(source, rect));

            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
                drawingContext.DrawDrawing(group);

            var resizedImage = new RenderTargetBitmap(
                width, height,         // Resized dimensions
                96, 96,                // Default DPI values
                PixelFormats.Default); // Default pixel format
            resizedImage.Render(drawingVisual);

            var frame = BitmapFrame.Create(resizedImage);
            frame.Freeze();
            return frame;
        }

        private Thumbnail _selectedThumbnail;

        public Thumbnail SelectedThumbnail {
            get {
                return _selectedThumbnail;
            }
            set {
                _selectedThumbnail = value;
                RaisePropertyChanged();
            }
        }

        private ObservableLimitedSizedStack<Thumbnail> _thumbnails;
        private IImagingMediator imagingMediator;
        private IImageSaveMediator imageSaveMediator;
        private readonly IImageDataFactory imageDataFactory;

        public ICommand SelectCommand { get; set; }
        public ICommand GradeImageCommand { get; set; }

        private async Task<bool> SelectImage(Thumbnail thumbnail) {
            var iarr = await thumbnail.LoadOriginalImage(profileService);
            if (iarr != null) {
                await imagingMediator.PrepareImage(iarr, new PrepareImageParameters(), new System.Threading.CancellationToken());
                return true;
            } else {
                return false;
            }
        }

        public ObservableLimitedSizedStack<Thumbnail> Thumbnails {
            get {
                if (_thumbnails == null) {
                    _thumbnails = new ObservableLimitedSizedStack<Thumbnail>(50);
                }
                return _thumbnails;
            }
            set {
                _thumbnails = value;
                RaisePropertyChanged();
            }
        }
    }
}