#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Model.ImageData;
using NINA.Utility.Enum;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.Utility.Mediator.Interfaces {

    public interface IImageSaveMediator : IMediator<IImageSaveController> {

        Task Enqueue(IImageData imageData, Task<IRenderedImage> prepareTask, IProgress<ApplicationStatus> progress, CancellationToken token);

        event EventHandler<ImageSavedEventArgs> ImageSaved;

        void OnImageSaved(ImageSavedEventArgs e);

        void Shutdown();
    }

    public class ImageSavedEventArgs : EventArgs {
        public ImageMetaData MetaData { get; set; }
        public BitmapSource Image { get; set; }
        public IImageStatistics Statistics { get; set; }
        public IStarDetectionAnalysis StarDetectionAnalysis { get; set; }
        public Uri PathToImage { get; set; }
        public FileTypeEnum FileType { get; set; }
        public bool IsBayered { get; internal set; }
        public double Duration { get; internal set; }
        public string Filter { get; internal set; }
    }
}