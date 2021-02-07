#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Model.ImageData;
using NINA.Profile;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using NINA.ViewModel.Interfaces;
using Nito.AsyncEx;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.ViewModel {

    public class ImageSaveController : BaseVM, IImageSaveController {
        private IImageSaveMediator imageSaveMediator;
        private Task worker;
        private CancellationTokenSource workerCTS;

        public ImageSaveController(IProfileService profileService, IImageSaveMediator imageSaveMediator) : base(profileService) {
            this.imageSaveMediator = imageSaveMediator;
            this.imageSaveMediator.RegisterHandler(this);

            // This queue size could be adjustable for systems with lots of memory available
            queue = new AsyncProducerConsumerQueue<PrepareSaveItem>(1);
            workerCTS = new CancellationTokenSource();
            worker = Task.Run(DoWork);
        }

        private AsyncProducerConsumerQueue<PrepareSaveItem> queue;

        public Task Enqueue(IImageData imageData, Task<IRenderedImage> prepareTask, IProgress<ApplicationStatus> progress, CancellationToken token) {
            return queue.EnqueueAsync(new PrepareSaveItem(imageData, prepareTask));
        }

        private async Task DoWork() {
            while (!workerCTS.IsCancellationRequested) {
                try {
                    var item = await queue.DequeueAsync(workerCTS.Token);

                    var path = await item.Data.PrepareSave(new FileSaveInfo(profileService), default);

                    var preparedData = await item.PrepareTask;

                    path = preparedData.RawImageData.FinalizeSave(path, profileService.ActiveProfile.ImageFileSettings.FilePattern);
                    var stats = await preparedData.RawImageData.Statistics;

                    imageSaveMediator.OnImageSaved(
                        new ImageSavedEventArgs() {
                            MetaData = preparedData.RawImageData.MetaData,
                            PathToImage = new Uri(path),
                            Image = preparedData.Image,
                            FileType = profileService.ActiveProfile.ImageFileSettings.FileType,
                            Statistics = stats,
                            StarDetectionAnalysis = preparedData.RawImageData.StarDetectionAnalysis,
                            Duration = preparedData.RawImageData.MetaData.Image.ExposureTime,
                            IsBayered = preparedData.RawImageData.Properties.IsBayered,
                            Filter = preparedData.RawImageData.MetaData.FilterWheel.Filter
                        }
                    );
                } catch (OperationCanceledException) {
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
                }
            }
        }

        public void Shutdown() {
            workerCTS.Cancel();
        }

        private class PrepareSaveItem {

            public PrepareSaveItem(IImageData data, Task<IRenderedImage> prepareTask) {
                Data = data;
                PrepareTask = prepareTask;
            }

            public IImageData Data { get; }
            public Task<IRenderedImage> PrepareTask { get; }
        }
    }
}