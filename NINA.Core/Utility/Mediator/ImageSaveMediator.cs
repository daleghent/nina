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
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator {

    public class ImageSaveMediator : IImageSaveMediator {
        protected IImageSaveController handler;

        public Task Enqueue(IImageData imageData, Task<IRenderedImage> prepareTask, IProgress<ApplicationStatus> progress, CancellationToken token) {
            return handler.Enqueue(imageData, prepareTask, progress, token);
        }

        public void RegisterHandler(IImageSaveController handler) {
            if (this.handler != null) {
                throw new Exception("Handler already registered!");
            }
            this.handler = handler;
        }

        public event EventHandler<ImageSavedEventArgs> ImageSaved;

        public void OnImageSaved(ImageSavedEventArgs e) {
            ImageSaved?.Invoke(handler, e);
        }

        public void Shutdown() {
            this.handler?.Shutdown();
        }
    }
}