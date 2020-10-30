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
using NINA.Model.ImageData;
using NINA.PlateSolving;
using NINA.Profile;
using NINA.Sequencer.Container;
using NINA.Sequencer.Utility;
using NINA.Sequencer.Validations;
using NINA.Utility.Astrometry;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.WindowService;
using NINA.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.SequenceItem.Platesolving {

    [ExportMetadata("Name", "Lbl_SequenceItem_Platesolving_Center_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Platesolving_Center_Description")]
    [ExportMetadata("Icon", "PlatesolveSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Telescope")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class Center : SequenceItem, IValidatable {
        protected IProfileService profileService;
        protected ITelescopeMediator telescopeMediator;
        protected IImagingMediator imagingMediator;
        protected IFilterWheelMediator filterWheelMediator;
        protected PlateSolvingStatusVM plateSolveStatusVM = new PlateSolvingStatusVM();

        [ImportingConstructor]
        public Center(IProfileService profileService, ITelescopeMediator telescopeMediator, IImagingMediator imagingMediator, IFilterWheelMediator filterWheelMediator) {
            this.profileService = profileService;
            this.telescopeMediator = telescopeMediator;
            this.imagingMediator = imagingMediator;
            this.filterWheelMediator = filterWheelMediator;
            Coordinates = new InputCoordinates();
        }

        private bool inherited;

        [JsonProperty]
        public bool Inherited {
            get => inherited;
            set {
                inherited = value;
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public InputCoordinates Coordinates { get; set; }

        public IWindowServiceFactory WindowServiceFactory { get; set; } = new WindowServiceFactory();
        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        protected virtual async Task<PlateSolveResult> DoCenter(IProgress<ApplicationStatus> progress, CancellationToken token) {
            await telescopeMediator.SlewToCoordinatesAsync(Coordinates.Coordinates, token);

            var plateSolver = PlateSolverFactory.GetPlateSolver(profileService.ActiveProfile.PlateSolveSettings);
            var blindSolver = PlateSolverFactory.GetBlindSolver(profileService.ActiveProfile.PlateSolveSettings);

            var solver = new CenteringSolver(plateSolver, blindSolver, imagingMediator, telescopeMediator, filterWheelMediator);
            var parameter = new CenterSolveParameter() {
                Attempts = profileService.ActiveProfile.PlateSolveSettings.NumberOfAttempts,
                Binning = profileService.ActiveProfile.PlateSolveSettings.Binning,
                Coordinates = Coordinates?.Coordinates ?? telescopeMediator.GetCurrentPosition(),
                DownSampleFactor = profileService.ActiveProfile.PlateSolveSettings.DownSampleFactor,
                FocalLength = profileService.ActiveProfile.TelescopeSettings.FocalLength,
                MaxObjects = profileService.ActiveProfile.PlateSolveSettings.MaxObjects,
                PixelSize = profileService.ActiveProfile.CameraSettings.PixelSize,
                ReattemptDelay = TimeSpan.FromMinutes(profileService.ActiveProfile.PlateSolveSettings.ReattemptDelay),
                Regions = profileService.ActiveProfile.PlateSolveSettings.Regions,
                SearchRadius = profileService.ActiveProfile.PlateSolveSettings.SearchRadius,
                Threshold = profileService.ActiveProfile.PlateSolveSettings.Threshold,
                NoSync = profileService.ActiveProfile.TelescopeSettings.NoSync
            };

            var seq = new CaptureSequence(
                profileService.ActiveProfile.PlateSolveSettings.ExposureTime,
                CaptureSequence.ImageTypes.SNAPSHOT,
                profileService.ActiveProfile.PlateSolveSettings.Filter,
                new Model.MyCamera.BinningMode(profileService.ActiveProfile.PlateSolveSettings.Binning, profileService.ActiveProfile.PlateSolveSettings.Binning),
                1
            );
            return await solver.Center(seq, parameter, plateSolveStatusVM.Progress, progress, token);
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            var service = WindowServiceFactory.Create();
            service.Show(plateSolveStatusVM, plateSolveStatusVM.Title, System.Windows.ResizeMode.CanResize, System.Windows.WindowStyle.ToolWindow);
            try {
                var result = await DoCenter(progress, token);
                if (result.Success == false) {
                    throw new Exception(Locale.Loc.Instance["LblPlatesolveFailed"]);
                }
            } finally {
                service.DelayedClose(TimeSpan.FromSeconds(10));
            }
        }

        public override void AfterParentChanged() {
            var coordinates = ItemUtility.RetrieveContextCoordinates(this.Parent).Item1;
            if (coordinates != null) {
                Coordinates.Coordinates = coordinates;
                Inherited = true;
            } else {
                Inherited = false;
            }
            Validate();
        }

        public override object Clone() {
            return new Center(profileService, telescopeMediator, imagingMediator, filterWheelMediator) {
                Icon = Icon,
                Name = Name,
                Category = Category,
                Description = Description,
                Coordinates = new InputCoordinates() { Coordinates = Coordinates.Coordinates.Transform(Epoch.J2000) }
            };
        }

        public virtual bool Validate() {
            var i = new List<string>();
            if (!telescopeMediator.GetInfo().Connected) {
                i.Add(Locale.Loc.Instance["LblTelescopeNotConnected"]);
            }
            Issues = i;
            return i.Count == 0;
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(Center)}, Coordinates {Coordinates?.Coordinates}";
        }
    }
}