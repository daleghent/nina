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
using NINA.PlateSolving;
using NINA.Profile;
using NINA.Sequencer.Container;
using NINA.Sequencer.Utility;
using NINA.Sequencer.Validations;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Mediator;
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

    [ExportMetadata("Name", "Lbl_SequenceItem_Platesolving_CenterAndRotate_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Platesolving_CenterAndRotate_Description")]
    [ExportMetadata("Icon", "PlatesolveAndRotateSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Telescope")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class CenterAndRotate : Center {
        private IRotatorMediator rotatorMediator;

        [ImportingConstructor]
        public CenterAndRotate(IProfileService profileService, ITelescopeMediator telescopeMediator, IImagingMediator imagingMediator, IRotatorMediator rotatorMediator, IFilterWheelMediator filterWheelMediator) : base(profileService, telescopeMediator, imagingMediator, filterWheelMediator) {
            this.rotatorMediator = rotatorMediator;
        }

        private double rotation = 0;

        [JsonProperty]
        public double Rotation {
            get => rotation;
            set {
                rotation = value;
                RaisePropertyChanged();
            }
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            var service = WindowServiceFactory.Create();
            service.Show(plateSolveStatusVM, plateSolveStatusVM.Title, System.Windows.ResizeMode.CanResize, System.Windows.WindowStyle.ToolWindow);
            try {
                var orientation = 0.0f;
                float rotationDistance = float.MaxValue;

                await telescopeMediator.SlewToCoordinatesAsync(Coordinates.Coordinates, token);

                /* Loop until the rotation is within tolerances*/
                while (Math.Abs(rotationDistance) > profileService.ActiveProfile.PlateSolveSettings.RotationTolerance) {
                    var solveResult = await Solve(progress, token);

                    orientation = (float)solveResult.Orientation;

                    rotationDistance = (float)((float)Rotation - orientation);

                    var movement = Astrometry.EuclidianModulus(rotationDistance, 180);
                    var movement2 = movement - 180;

                    if (movement < Math.Abs(movement2)) {
                        rotationDistance = movement;
                    } else {
                        rotationDistance = movement2;
                    }

                    rotatorMediator.Sync(orientation);

                    if (Math.Abs(rotationDistance) > profileService.ActiveProfile.PlateSolveSettings.RotationTolerance) {
                        Logger.Info($"Rotator not inside tolerance {profileService.ActiveProfile.PlateSolveSettings.RotationTolerance} - Current {orientation}° / Target: {Rotation}° - Moving rotator relatively by {rotationDistance}°");
                        await rotatorMediator.MoveRelative(rotationDistance);
                    }
                };

                /* Once everything is in place do a centering of the object */
                await base.DoCenter(progress, token);
            } finally {
                service.DelayedClose(TimeSpan.FromSeconds(10));
            }
        }

        private async Task<PlateSolveResult> Solve(IProgress<ApplicationStatus> progress, CancellationToken token) {
            var plateSolver = PlateSolverFactory.GetPlateSolver(profileService.ActiveProfile.PlateSolveSettings);
            var blindSolver = PlateSolverFactory.GetBlindSolver(profileService.ActiveProfile.PlateSolveSettings);

            var solver = new CaptureSolver(plateSolver, blindSolver, imagingMediator, filterWheelMediator);
            var parameter = new CaptureSolverParameter() {
                Attempts = profileService.ActiveProfile.PlateSolveSettings.NumberOfAttempts,
                Binning = profileService.ActiveProfile.PlateSolveSettings.Binning,
                Coordinates = Coordinates?.Coordinates ?? telescopeMediator.GetCurrentPosition(),
                DownSampleFactor = profileService.ActiveProfile.PlateSolveSettings.DownSampleFactor,
                FocalLength = profileService.ActiveProfile.TelescopeSettings.FocalLength,
                MaxObjects = profileService.ActiveProfile.PlateSolveSettings.MaxObjects,
                PixelSize = profileService.ActiveProfile.CameraSettings.PixelSize,
                ReattemptDelay = TimeSpan.FromMinutes(profileService.ActiveProfile.PlateSolveSettings.ReattemptDelay),
                Regions = profileService.ActiveProfile.PlateSolveSettings.Regions,
                SearchRadius = profileService.ActiveProfile.PlateSolveSettings.SearchRadius
            };

            var seq = new CaptureSequence(
                profileService.ActiveProfile.PlateSolveSettings.ExposureTime,
                CaptureSequence.ImageTypes.SNAPSHOT,
                profileService.ActiveProfile.PlateSolveSettings.Filter,
                new Model.MyCamera.BinningMode(profileService.ActiveProfile.PlateSolveSettings.Binning, profileService.ActiveProfile.PlateSolveSettings.Binning),
                1
            );
            return await solver.Solve(seq, parameter, plateSolveStatusVM.Progress, progress, token);
        }

        public override void AfterParentChanged() {
            var tuple = ItemUtility.RetrieveContextCoordinates(this.Parent);
            if (tuple.Item1 != null) {
                Coordinates.Coordinates = tuple.Item1;
                Rotation = tuple.Item2;
                Inherited = true;
            } else {
                Inherited = false;
            }
            Validate();
        }

        public override object Clone() {
            return new CenterAndRotate(profileService, telescopeMediator, imagingMediator, rotatorMediator, filterWheelMediator) {
                Icon = Icon,
                Name = Name,
                Category = Category,
                Description = Description,
                Coordinates = new InputCoordinates() { Coordinates = Coordinates.Coordinates.Transform(Epoch.J2000) },
                Rotation = Rotation
            };
        }

        public override bool Validate() {
            var i = new List<string>();
            if (!telescopeMediator.GetInfo().Connected) {
                i.Add(Locale.Loc.Instance["LblTelescopeNotConnected"]);
            }
            if (!rotatorMediator.GetInfo().Connected) {
                i.Add(Locale.Loc.Instance["LblRotatorNotConnected"]);
            }
            Issues = i;
            return Issues.Count == 0;
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(CenterAndRotate)}, Coordinates {Coordinates?.Coordinates}, Rotation: {Rotation}";
        }
    }
}