#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Model;
using NINA.Utility;
using NINA.Profile;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Utility;
using NINA.Sequencer.Validations;
using NINA.Utility.Astrometry;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel;
using NINA.ViewModel.ImageHistory;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.Trigger.MeridianFlip {

    [ExportMetadata("Name", "Lbl_SequenceTrigger_MeridianFlipTrigger_Name")]
    [ExportMetadata("Description", "Lbl_SequenceTrigger_MeridianFlipTrigger_Description")]
    [ExportMetadata("Icon", "MeridianFlipSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Telescope")]
    [Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]
    public class MeridianFlipTrigger : SequenceTrigger, IValidatable {
        private IProfileService profileService;
        private ITelescopeMediator telescopeMediator;
        private IGuiderMediator guiderMediator;
        private IImagingMediator imagingMediator;
        private IFilterWheelMediator filterWheelMediator;
        private IApplicationStatusMediator applicationStatusMediator;
        private ICameraMediator cameraMediator;
        private DateTime lastFlipTime = DateTime.MinValue;

        [ImportingConstructor]
        public MeridianFlipTrigger(IProfileService profileService, ICameraMediator cameraMediator, ITelescopeMediator telescopeMediator, IGuiderMediator guiderMediator, IFocuserMediator focuserMediator, IImagingMediator imagingMediator, IApplicationStatusMediator applicationStatusMediator, IFilterWheelMediator filterWheelMediator, IImageHistoryVM historyVM) : base() {
            this.profileService = profileService;
            this.telescopeMediator = telescopeMediator;
            this.guiderMediator = guiderMediator;
            this.imagingMediator = imagingMediator;
            this.filterWheelMediator = filterWheelMediator;
            this.applicationStatusMediator = applicationStatusMediator;
            this.cameraMediator = cameraMediator;
            this.focuserMediator = focuserMediator;
            this.history = historyVM;
        }

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = ImmutableList.CreateRange(value);
                RaisePropertyChanged();
            }
        }

        public override object Clone() {
            return new MeridianFlipTrigger(profileService, cameraMediator, telescopeMediator, guiderMediator, focuserMediator, imagingMediator, applicationStatusMediator, filterWheelMediator, history) {
                Icon = Icon,
                Name = Name,
                Category = Category,
                Description = Description,
            };
        }

        private DateTime latestFlipTime;
        private DateTime earliestFlipTime;
        private IFocuserMediator focuserMediator;
        private IImageHistoryVM history;

        public DateTime LatestFlipTime {
            get => latestFlipTime;
            private set {
                latestFlipTime = value;
                RaisePropertyChanged();
            }
        }

        public DateTime EarliestFlipTime {
            get => earliestFlipTime;
            private set {
                earliestFlipTime = value;
                RaisePropertyChanged();
            }
        }

        public override Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            var target = ItemUtility.RetrieveContextCoordinates(context).Item1 ?? telescopeMediator.GetCurrentPosition();

            var timeToFlip = CalculateMinimumTimeRemaining();
            if (timeToFlip > TimeSpan.FromHours(2)) {
                //Assume a delayed flip when the time is more than two hours and flip immediately
                timeToFlip = TimeSpan.Zero;
            }

            lastFlipTime = DateTime.Now;
            return new MeridianFlipVM(profileService, cameraMediator, telescopeMediator, guiderMediator, focuserMediator, imagingMediator, applicationStatusMediator, filterWheelMediator, history)
                .MeridianFlip(target, timeToFlip);
        }

        public override void AfterParentChanged() {
            lastFlipTime = DateTime.MinValue;
        }

        public override void Initialize() {
        }

        private TimeSpan CalculateMinimumTimeRemaining() {
            var settings = profileService.ActiveProfile.MeridianFlipSettings;
            //Substract delta from maximum to get minimum time
            var delta = settings.MaxMinutesAfterMeridian - settings.MinutesAfterMeridian;
            var time = CalculateMaximumTimeRemainaing() - TimeSpan.FromMinutes(delta);
            if (time < TimeSpan.Zero) {
                time = TimeSpan.Zero;
            }
            return time;
        }

        private TimeSpan CalculateMaximumTimeRemainaing() {
            var telescopeInfo = telescopeMediator.GetInfo();

            return TimeSpan.FromHours(telescopeInfo.TimeToMeridianFlip);
        }

        public override bool ShouldTrigger(ISequenceItem nextItem) {
            var telescopeInfo = telescopeMediator.GetInfo();
            var settings = profileService.ActiveProfile.MeridianFlipSettings;

            if (!telescopeInfo.Connected || double.IsNaN(telescopeInfo.TimeToMeridianFlip)) {
                Logger.Error("Meridian Flip - Telescope is not connected to evaluate if a flip should happen!");
                return false;
            }

            if ((DateTime.Now - lastFlipTime) < TimeSpan.FromHours(11)) {
                //A flip for the same target is only expected every 12 hours on planet earth
                Logger.Debug($"Meridian Flip - Flip for the current target already happened at {lastFlipTime}. Flip will be skipped");
                return false;
            }

            var nextInstructionTime = nextItem.GetEstimatedDuration().TotalSeconds;

            //The time to meridian flip reported by the telescope is the latest time for a flip to happen
            var minimumTimeRemaining = CalculateMinimumTimeRemaining();
            var maximumTimeRemaining = CalculateMaximumTimeRemainaing();
            var originalMaximumTimeRemaining = maximumTimeRemaining;
            if (settings.PauseTimeBeforeMeridian != 0) {
                //A pause prior to a meridian flip is a hard limit due to equipment obstruction. There is no possibility for a timerange as we have to pause early and wait for meridian to pass
                minimumTimeRemaining = minimumTimeRemaining - TimeSpan.FromMinutes(profileService.ActiveProfile.MeridianFlipSettings.MinutesAfterMeridian) - TimeSpan.FromMinutes(profileService.ActiveProfile.MeridianFlipSettings.PauseTimeBeforeMeridian);
                maximumTimeRemaining = minimumTimeRemaining;
            }

            UpdateMeridianFlipTimeTriggerValues(minimumTimeRemaining, originalMaximumTimeRemaining, TimeSpan.FromMinutes(profileService.ActiveProfile.MeridianFlipSettings.PauseTimeBeforeMeridian), TimeSpan.FromMinutes(profileService.ActiveProfile.MeridianFlipSettings.MaxMinutesAfterMeridian));

            if (minimumTimeRemaining <= TimeSpan.Zero && maximumTimeRemaining > TimeSpan.Zero) {
                Logger.Info($"Meridian Flip - Remaining Time is between minimum and maximum flip time. Minimum time remaining {minimumTimeRemaining}, maximum time remaining {maximumTimeRemaining}. Flip should happen now");
                return true;
            } else {
                //The minimum time to flip has not been reached yet. Check if a flip is required based on the estimation of the next instruction
                var noRemainingTime = maximumTimeRemaining <= TimeSpan.FromSeconds(nextInstructionTime);

                if (settings.UseSideOfPier && telescopeInfo.SideOfPier == Model.MyTelescope.PierSide.pierUnknown) {
                    Logger.Error("Side of Pier usage is enabled, however the side of pier reported by the driver is unknown. Ignoring side of pier to calculate the flip time");
                }

                if (settings.UseSideOfPier && telescopeInfo.SideOfPier != Model.MyTelescope.PierSide.pierUnknown) {
                    if (noRemainingTime) {
                        // There is no more time remaining. Project the side of pier to that at the time after the flip and check if this flip is required
                        var projectedSiderealTime = Angle.ByHours(Astrometry.EuclidianModulus(telescopeInfo.SiderealTime + originalMaximumTimeRemaining.TotalHours, 24));
                        var targetSideOfPier = NINA.Utility.MeridianFlip.ExpectedPierSide(
                            coordinates: telescopeInfo.Coordinates,
                            localSiderealTime: projectedSiderealTime);
                        if (telescopeInfo.SideOfPier == targetSideOfPier) {
                            Logger.Info($"Meridian Flip - Telescope already reports {telescopeInfo.SideOfPier}. Automated Flip will not be performed.");
                            return false;
                        } else {
                            Logger.Info("Meridian Flip - No more remaining time available before flip. Flip should happen now");
                            return true;
                        }
                    } else {
                        // There is still time remaining. A flip is likely not required. Double check by checking the current expected side of pier with the actual side of pier
                        var targetSideOfPier = NINA.Utility.MeridianFlip.ExpectedPierSide(
                            coordinates: telescopeInfo.Coordinates,
                            localSiderealTime: Angle.ByHours(telescopeInfo.SiderealTime));
                        if (telescopeInfo.SideOfPier == targetSideOfPier) {
                            Logger.Info($"Meridian Flip - Telescope already reports {telescopeInfo.SideOfPier}. Automated Flip will not be performed.");
                            return false;
                        } else {
                            // When pier side doesn't match the target, but remaining time indicating that a flip happened, the flip seems to have not happened yet and must be done immediately
                            // Only allow delayed flip behavior for the first hour after a flip should've happened
                            var delayedFlip = maximumTimeRemaining
                                >= (TimeSpan.FromHours(11)
                                    - TimeSpan.FromMinutes(settings.MaxMinutesAfterMeridian)
                                    - TimeSpan.FromMinutes(settings.PauseTimeBeforeMeridian)
                                  );

                            if (delayedFlip) {
                                Logger.Info($"Meridian Flip - Flip seems to not happened in time as Side Of Pier is {telescopeInfo.SideOfPier} but expected to be {targetSideOfPier}. Flip should happen now");
                            }
                            return delayedFlip;
                        }
                    }
                } else {
                    if (noRemainingTime) {
                        Logger.Info($"Meridian Flip - No more remaining time available before flip. Max remaining time {maximumTimeRemaining}, next instruction time {nextInstructionTime}. Flip should happen now");
                        return true;
                    }
                }

                return false;
            }
        }

        private void UpdateMeridianFlipTimeTriggerValues(TimeSpan minimumTimeRemaining, TimeSpan maximumTimeRemaining, TimeSpan pauseBeforeMeridian, TimeSpan maximumTimeAfterMeridian) {
            //Update the FlipTimes
            if (pauseBeforeMeridian == TimeSpan.Zero) {
                EarliestFlipTime = DateTime.Now + minimumTimeRemaining;
                LatestFlipTime = DateTime.Now + maximumTimeRemaining;
            } else {
                EarliestFlipTime = DateTime.Now + maximumTimeRemaining - maximumTimeAfterMeridian - pauseBeforeMeridian;
                LatestFlipTime = DateTime.Now + maximumTimeRemaining - maximumTimeAfterMeridian - pauseBeforeMeridian;
            }
        }

        public override string ToString() {
            return $"Trigger: {nameof(MeridianFlipTrigger)}";
        }

        public bool Validate() {
            var i = new List<string>();
            var cameraInfo = telescopeMediator.GetInfo();

            if (!cameraMediator.GetInfo().Connected) {
                i.Add(Locale.Loc.Instance["LblCameraNotConnected"]);
            }
            if (!cameraInfo.Connected) {
                i.Add(Locale.Loc.Instance["LblTelescopeNotConnected"]);
            }

            if (profileService.ActiveProfile.MeridianFlipSettings.AutoFocusAfterFlip) {
                if (!focuserMediator.GetInfo().Connected) {
                    i.Add(Locale.Loc.Instance["LblFocuserNotConnected"]);
                }
            }

            Issues = i;
            return i.Count == 0;
        }
    }
}