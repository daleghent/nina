using Newtonsoft.Json;
using NINA.Astrometry;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Model;
using NINA.PlateSolving;
using NINA.PlateSolving.Interfaces;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Validations;
using NINA.WPF.Base.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.SequenceItem.Platesolving {

    [ExportMetadata("Name", "Lbl_SequenceItem_Platesolving_SolveAndRotate_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Platesolving_SolveAndRotate_Description")]
    [ExportMetadata("Icon", "PlatesolveAndRotateSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Rotator")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SolveAndRotate : SequenceItem, IValidatable {
        protected IProfileService profileService;
        protected ITelescopeMediator telescopeMediator;
        protected IImagingMediator imagingMediator;
        protected IFilterWheelMediator filterWheelMediator;
        protected IGuiderMediator guiderMediator;
        protected IPlateSolverFactory plateSolverFactory;
        protected IWindowServiceFactory windowServiceFactory;
        private IRotatorMediator rotatorMediator;
        public PlateSolvingStatusVM PlateSolveStatusVM { get; } = new PlateSolvingStatusVM();

        [ImportingConstructor]
        public SolveAndRotate(IProfileService profileService,
                               ITelescopeMediator telescopeMediator,
                               IImagingMediator imagingMediator,
                               IRotatorMediator rotatorMediator,
                               IFilterWheelMediator filterWheelMediator,
                               IGuiderMediator guiderMediator,
                               IPlateSolverFactory plateSolverFactory,
                               IWindowServiceFactory windowServiceFactory) {
            this.profileService = profileService;
            this.telescopeMediator = telescopeMediator;
            this.imagingMediator = imagingMediator;
            this.filterWheelMediator = filterWheelMediator;
            this.guiderMediator = guiderMediator;
            this.plateSolverFactory = plateSolverFactory;
            this.windowServiceFactory = windowServiceFactory;
            this.rotatorMediator = rotatorMediator;
        }

        private SolveAndRotate(SolveAndRotate cloneMe) : this(cloneMe.profileService,
                                                                cloneMe.telescopeMediator,
                                                                cloneMe.imagingMediator,
                                                                cloneMe.rotatorMediator,
                                                                cloneMe.filterWheelMediator,
                                                                cloneMe.guiderMediator,
                                                                cloneMe.plateSolverFactory,
                                                                cloneMe.windowServiceFactory) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new SolveAndRotate(this) {
                Rotation = Rotation
            };
        }

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
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
            var service = windowServiceFactory.Create();
            service.Show(PlateSolveStatusVM, PlateSolveStatusVM.Title, System.Windows.ResizeMode.CanResize, System.Windows.WindowStyle.ToolWindow);
            try {
                var orientation = 0.0f;
                float rotationDistance = float.MaxValue;

                var stoppedGuiding = await guiderMediator.StopGuiding(token);

                var targetRotation = (float)Rotation;

                /* Loop until the rotation is within tolerances*/
                while (Math.Abs(rotationDistance) > profileService.ActiveProfile.PlateSolveSettings.RotationTolerance) {
                    var solveResult = await Solve(progress, token);
                    if (!solveResult.Success) {
                        throw new SequenceEntityFailedException(Loc.Instance["LblPlatesolveFailed"]);
                    }

                    orientation = (float)solveResult.Orientation;
                    rotatorMediator.Sync(orientation);

                    var prevTargetRotation = targetRotation;
                    targetRotation = rotatorMediator.GetTargetPosition(prevTargetRotation);
                    if (Math.Abs(targetRotation - prevTargetRotation) > 0.1) {
                        Logger.Info($"Rotator target position {Rotation} adjusted to {targetRotation} to be within the allowed mechanical range");
                        Notification.ShowInformation(string.Format(Loc.Instance["LblRotatorRangeAdjusted"], targetRotation));
                    }

                    rotationDistance = targetRotation - orientation;
                    if (profileService.ActiveProfile.RotatorSettings.RangeType == Core.Enum.RotatorRangeTypeEnum.FULL) {
                        // If the full rotation range is allowed, then consider the 180-degree rotated orientation as well in case it is closer
                        var movement = AstroUtil.EuclidianModulus(rotationDistance, 180);
                        var movement2 = movement - 180;

                        if (movement < Math.Abs(movement2)) {
                            rotationDistance = movement;
                        } else {
                            targetRotation = AstroUtil.EuclidianModulus(targetRotation + 180, 360);
                            Logger.Info($"Changing rotation target to {targetRotation} instead since it is closer to the current position");
                            rotationDistance = movement2;
                        }
                    }

                    if (Math.Abs(rotationDistance) > profileService.ActiveProfile.PlateSolveSettings.RotationTolerance) {
                        Logger.Info($"Rotator not inside tolerance {profileService.ActiveProfile.PlateSolveSettings.RotationTolerance} - Current {orientation}° / Target: {Rotation}° - Moving rotator relatively by {rotationDistance}°");
                        await rotatorMediator.MoveRelative(rotationDistance);
                    }
                };

                if (stoppedGuiding) {
                    await guiderMediator.StartGuiding(false, progress, token);
                }
            } finally {
                service.DelayedClose(TimeSpan.FromSeconds(10));
            }
        }

        private async Task<PlateSolveResult> Solve(IProgress<ApplicationStatus> progress, CancellationToken token) {
            var plateSolver = plateSolverFactory.GetPlateSolver(profileService.ActiveProfile.PlateSolveSettings);
            var blindSolver = plateSolverFactory.GetBlindSolver(profileService.ActiveProfile.PlateSolveSettings);

            var solver = plateSolverFactory.GetCaptureSolver(plateSolver, blindSolver, imagingMediator, filterWheelMediator);
            var parameter = new CaptureSolverParameter() {
                Attempts = profileService.ActiveProfile.PlateSolveSettings.NumberOfAttempts,
                Binning = profileService.ActiveProfile.PlateSolveSettings.Binning,
                Coordinates = telescopeMediator.GetCurrentPosition(),
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
                new BinningMode(profileService.ActiveProfile.PlateSolveSettings.Binning, profileService.ActiveProfile.PlateSolveSettings.Binning),
                1
            );
            return await solver.Solve(seq, parameter, PlateSolveStatusVM.Progress, progress, token);
        }

        public bool Validate() {
            var i = new List<string>();

            if (!rotatorMediator.GetInfo().Connected) {
                i.Add(Loc.Instance["LblRotatorNotConnected"]);
            }

            Issues = i;
            return Issues.Count == 0;
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(SolveAndRotate)}, Rotation: {Rotation}°";
        }
    }
}