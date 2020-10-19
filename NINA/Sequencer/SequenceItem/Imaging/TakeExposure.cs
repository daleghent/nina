#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Profile;
using NINA.Sequencer.Container;
using NINA.Sequencer.Exceptions;
using NINA.Sequencer.Validations;
using NINA.Utility;
using NINA.Utility.Mediator;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel.ImageHistory;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.SequenceItem.Imaging {

    [ExportMetadata("Name", "Lbl_SequenceItem_Imaging_TakeExposure_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Imaging_TakeExposure_Description")]
    [ExportMetadata("Icon", "CameraSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Camera")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class TakeExposure : SequenceItem, IValidatable {
        private ICameraMediator cameraMediator;
        private IImagingMediator imagingMediator;
        private IImageSaveMediator imageSaveMediator;
        private IImageHistoryVM imageHistoryVM;

        [ImportingConstructor]
        public TakeExposure(ICameraMediator cameraMediator, IImagingMediator imagingMediator, IImageSaveMediator imageSaveMediator, IImageHistoryVM imageHistoryVM) {
            Gain = -1;
            Offset = -1;
            ImageType = Model.CaptureSequence.ImageTypes.LIGHT;
            this.cameraMediator = cameraMediator;
            this.imagingMediator = imagingMediator;
            this.imageSaveMediator = imageSaveMediator;
            this.imageHistoryVM = imageHistoryVM;
            CameraInfo = this.cameraMediator.GetInfo();
        }

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        public override string Detail { get => $"{ExposureTime}s"; }

        private double exposureTime;

        [JsonProperty]
        public double ExposureTime {
            get => exposureTime;
            set {
                exposureTime = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Detail));
            }
        }

        [JsonProperty]
        public int Gain { get; set; }

        [JsonProperty]
        public int Offset { get; set; }

        [JsonProperty]
        public BinningMode Binning { get; set; }

        [JsonProperty]
        public string ImageType { get; set; }

        [JsonProperty]
        public int ExposureCount { get; private set; }

        [JsonProperty]
        public string TargetName { get; private set; }

        private CameraInfo cameraInfo;

        public CameraInfo CameraInfo {
            get => cameraInfo;
            private set {
                cameraInfo = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<string> _imageTypes;

        public ObservableCollection<string> ImageTypes {
            get {
                if (_imageTypes == null) {
                    _imageTypes = new ObservableCollection<string>();

                    Type type = typeof(Model.CaptureSequence.ImageTypes);
                    foreach (var p in type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)) {
                        var v = p.GetValue(null);
                        _imageTypes.Add(v.ToString());
                    }
                }
                return _imageTypes;
            }
            set {
                _imageTypes = value;
                RaisePropertyChanged();
            }
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (Validate()) {
                var capture = new CaptureSequence() {
                    ExposureTime = ExposureTime,
                    Binning = Binning,
                    Gain = Gain,
                    Offset = Offset,
                    ImageType = ImageType,
                    ProgressExposureCount = ExposureCount,
                    TotalExposureCount = ExposureCount + 1,
                };

                var imageParams = new PrepareImageParameters(null, false);
                if (IsLightSequence()) {
                    imageParams = new PrepareImageParameters(true, true);
                }

                var exposureData = await imagingMediator.CaptureImage(capture, token, progress, TargetName);

                var imageData = await exposureData.ToImageData(token);

                var prepareTask = imagingMediator.PrepareImage(exposureData, imageParams, token);

                await imageSaveMediator.Enqueue(imageData, prepareTask, progress, token);

                if (IsLightSequence()) {
                    imageHistoryVM.Add(await imageData.Statistics);
                }

                ExposureCount++;
            } else {
                throw new SequenceItemSkippedException(string.Join(",", Issues));
            }
        }

        private bool IsLightSequence() {
            return ImageType == CaptureSequence.ImageTypes.SNAPSHOT || ImageType == CaptureSequence.ImageTypes.LIGHT;
        }

        public override object Clone() {
            return new TakeExposure(cameraMediator, imagingMediator, imageSaveMediator, imageHistoryVM) {
                Icon = Icon,
                Name = Name,
                Description = Description,
                ExposureTime = ExposureTime,
                ExposureCount = 0,
                Binning = Binning,
                Gain = Gain,
                Offset = Offset,
                ImageType = ImageType,
            };
        }

        public override void AfterParentChanged() {
            var info = RetrieveTargetName(this.Parent);
            TargetName = info;
            Validate();
        }

        private string RetrieveTargetName(ISequenceContainer parent) {
            if (parent != null) {
                var container = parent as IDeepSkyObjectContainer;
                if (container != null) {
                    return container.Target?.TargetName;
                } else {
                    return RetrieveTargetName(parent.Parent);
                }
            } else {
                return string.Empty;
            }
        }

        public bool Validate() {
            var i = new List<string>();
            CameraInfo = this.cameraMediator.GetInfo();
            if (!CameraInfo.Connected) {
                i.Add(Locale.Loc.Instance["LblCameraNotConnected"]);
            }
            Issues = i;
            return i.Count == 0;
        }

        public override TimeSpan GetEstimatedDuration() {
            return TimeSpan.FromSeconds(this.ExposureTime);
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(TakeExposure)}, ExposureTime {ExposureTime}, Gain {Gain}, Offset {Offset}, ImageType {ImageType}, Binning {Binning?.Name}";
        }
    }
}