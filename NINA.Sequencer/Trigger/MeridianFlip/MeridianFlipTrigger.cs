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
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Utility;
using NINA.Sequencer.Validations;
using NINA.Astrometry;
using NINA.Equipment.Interfaces.Mediator;
using NINA.ViewModel;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Enum;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Core.Locale;
using NINA.WPF.Base.ViewModel;
using NINA.Sequencer.Interfaces;

namespace NINA.Sequencer.Trigger.MeridianFlip {

    [ExportMetadata("Name", "Lbl_SequenceTrigger_MeridianFlipTrigger_Name")]
    [ExportMetadata("Description", "Lbl_SequenceTrigger_MeridianFlipTrigger_Description")]
    [ExportMetadata("Icon", "MeridianFlipSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Telescope")]
    [Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]
    public class MeridianFlipTrigger : SequenceTrigger, IMeridianFlipTrigger, IValidatable {
        protected IProfileService profileService;
        protected ITelescopeMediator telescopeMediator;
        protected IGuiderMediator guiderMediator;
        protected IImagingMediator imagingMediator;
        protected IFilterWheelMediator filterWheelMediator;
        protected IApplicationStatusMediator applicationStatusMediator;
        protected ICameraMediator cameraMediator;
        protected IFocuserMediator focuserMediator;
        protected IImageHistoryVM history;

        protected DateTime lastFlipTime = DateTime.MinValue;
        protected Coordinates lastFlipCoordiantes;

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

        protected MeridianFlipTrigger(MeridianFlipTrigger cloneMe) : this(cloneMe.profileService, cloneMe.cameraMediator, cloneMe.telescopeMediator, cloneMe.guiderMediator, cloneMe.focuserMediator, cloneMe.imagingMediator, cloneMe.applicationStatusMediator, cloneMe.filterWheelMediator, cloneMe.history) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new MeridianFlipTrigger(this);
        }

        protected IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = ImmutableList.CreateRange(value);
                RaisePropertyChanged();
            }
        }

        protected DateTime latestFlipTime;
        protected DateTime earliestFlipTime;

        public virtual double MinutesAfterMeridian {
            get {
                return profileService.ActiveProfile.MeridianFlipSettings.MinutesAfterMeridian;
            }
            set { }
        }

        public virtual double PauseTimeBeforeMeridian {
            get {
                return profileService.ActiveProfile.MeridianFlipSettings.PauseTimeBeforeMeridian;
            }
            set { }
        }

        public virtual double MaxMinutesAfterMeridian {
            get {
                return profileService.ActiveProfile.MeridianFlipSettings.MaxMinutesAfterMeridian;
            }
            set { }
        }

        public virtual bool UseSideOfPier {
            get {
                return profileService.ActiveProfile.MeridianFlipSettings.UseSideOfPier;
            }
            set { }
        }

        public virtual DateTime LatestFlipTime {
            get => latestFlipTime;
            protected set {
                latestFlipTime = value;
                RaisePropertyChanged();
            }
        }

        public virtual DateTime EarliestFlipTime {
            get => earliestFlipTime;
            protected set {
                earliestFlipTime = value;
                RaisePropertyChanged();
            }
        }

        public virtual double TimeToMeridianFlip {
            get {
                return telescopeMediator.GetInfo().TimeToMeridianFlip;
            }
            set { }
        }

        public override Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            var target = ItemUtility.RetrieveContextCoordinates(context).Item1;
            if (target == null) {
                target = telescopeMediator.GetCurrentPosition();
                Logger.Warning("No target information available for flip. Taking current telescope coordinates instead");
            }

            var timeToFlip = CalculateMinimumTimeRemaining();
            if (timeToFlip > TimeSpan.FromHours(2)) {
                //Assume a delayed flip when the time is more than two hours and flip immediately
                timeToFlip = TimeSpan.Zero;
            }

            lastFlipTime = DateTime.Now;
            lastFlipCoordiantes = target;
            return new MeridianFlipVM(profileService, cameraMediator, telescopeMediator, guiderMediator, focuserMediator, imagingMediator, applicationStatusMediator, filterWheelMediator, history)
                .MeridianFlip(target, timeToFlip);
        }

        public override void AfterParentChanged() {
            lastFlipTime = DateTime.MinValue;
            lastFlipCoordiantes = null;
        }

        protected virtual TimeSpan CalculateMinimumTimeRemaining() {
            //Substract delta from maximum to get minimum time
            var delta = MaxMinutesAfterMeridian - MinutesAfterMeridian;
            var time = CalculateMaximumTimeRemainaing() - TimeSpan.FromMinutes(delta);
            if (time < TimeSpan.Zero) {
                time = TimeSpan.Zero;
            }
            return time;
        }

        protected virtual TimeSpan CalculateMaximumTimeRemainaing() {
            return TimeSpan.FromHours(TimeToMeridianFlip);
        }

        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
            var telescopeInfo = telescopeMediator.GetInfo();
            //var settings = profileService.ActiveProfile.MeridianFlipSettings;

            if (!telescopeInfo.Connected || double.IsNaN(telescopeInfo.TimeToMeridianFlip)) {
                EarliestFlipTime = DateTime.MinValue;
                LatestFlipTime = DateTime.MinValue;
                Logger.Error("Meridian Flip - Telescope is not connected to evaluate if a flip should happen!");
                return false;
            }

            if (!telescopeInfo.TrackingEnabled) {
                EarliestFlipTime = DateTime.MinValue;
                LatestFlipTime = DateTime.MinValue;
                Logger.Info("Meridian Flip - Telescope is not tracking. Skip flip evaluation");
                return false;
            }

            // When side of pier is disabled - check if the last flip time was less than 11 hours ago and further check if the current position is similar to the last flip position. If all are true, no flip is required.
            if (UseSideOfPier == false && (DateTime.Now - lastFlipTime) < TimeSpan.FromHours(11) && lastFlipCoordiantes != null && (lastFlipCoordiantes - telescopeInfo.Coordinates).Distance.ArcMinutes < 20) {
                //A flip for the same target is only expected every 12 hours on planet earth and
                Logger.Info($"Meridian Flip - Flip for the current target already happened at {lastFlipTime}. Skip flip evaluation");
                return false;
            }

            var nextInstructionTime = nextItem?.GetEstimatedDuration().TotalSeconds ?? 0;

            //The time to meridian flip reported by the telescope is the latest time for a flip to happen
            var minimumTimeRemaining = CalculateMinimumTimeRemaining();
            var maximumTimeRemaining = CalculateMaximumTimeRemainaing();
            var originalMaximumTimeRemaining = maximumTimeRemaining;
            if (PauseTimeBeforeMeridian != 0) {
                //A pause prior to a meridian flip is a hard limit due to equipment obstruction. There is no possibility for a timerange as we have to pause early and wait for meridian to pass
                minimumTimeRemaining = minimumTimeRemaining - TimeSpan.FromMinutes(MinutesAfterMeridian) - TimeSpan.FromMinutes(PauseTimeBeforeMeridian);
                maximumTimeRemaining = minimumTimeRemaining;
            }

            UpdateMeridianFlipTimeTriggerValues(minimumTimeRemaining, originalMaximumTimeRemaining, TimeSpan.FromMinutes(PauseTimeBeforeMeridian), TimeSpan.FromMinutes(MaxMinutesAfterMeridian));

            if (minimumTimeRemaining <= TimeSpan.Zero && maximumTimeRemaining > TimeSpan.Zero) {
                Logger.Info($"Meridian Flip - Remaining Time is between minimum and maximum flip time. Minimum time remaining {minimumTimeRemaining}, maximum time remaining {maximumTimeRemaining}. Flip should happen now");
                return true;
            } else {
                if (UseSideOfPier && telescopeInfo.SideOfPier == PierSide.pierUnknown) {
                    Logger.Error("Side of Pier usage is enabled, however the side of pier reported by the driver is unknown. Ignoring side of pier to calculate the flip time");
                }

                if (UseSideOfPier && telescopeInfo.SideOfPier != PierSide.pierUnknown) {
                    //The minimum time to flip has not been reached yet. Check if a flip is required based on the estimation of the next instruction
                    var noRemainingTime = maximumTimeRemaining <= TimeSpan.FromSeconds(nextInstructionTime);

                    if (noRemainingTime) {
                        // There is no more time remaining. Project the side of pier to that at the time after the flip and check if this flip is required
                        var projectedSiderealTime = Angle.ByHours(AstroUtil.EuclidianModulus(telescopeInfo.SiderealTime + originalMaximumTimeRemaining.TotalHours, 24));
                        var targetSideOfPier = NINA.Astrometry.MeridianFlip.ExpectedPierSide(
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
                        var targetSideOfPier = NINA.Astrometry.MeridianFlip.ExpectedPierSide(
                            coordinates: telescopeInfo.Coordinates,
                            localSiderealTime: Angle.ByHours(telescopeInfo.SiderealTime));
                        if (telescopeInfo.SideOfPier == targetSideOfPier) {
                            Logger.Info($"Meridian Flip - There is still time remainging, but the telescope already reports {telescopeInfo.SideOfPier}. Automated Flip will not be performed.");
                            return false;
                        } else {
                            // When pier side doesn't match the target, but remaining time indicating that a flip happened, the flip seems to have not happened yet and must be done immediately
                            // Only allow delayed flip behavior for the first hour after a flip should've happened
                            var delayedFlip = maximumTimeRemaining
                                >= (TimeSpan.FromHours(11)
                                    - TimeSpan.FromMinutes(MaxMinutesAfterMeridian)
                                    - TimeSpan.FromMinutes(PauseTimeBeforeMeridian)
                                  );

                            if (delayedFlip) {
                                Logger.Info($"Meridian Flip - Flip seems to not happened in time as Side Of Pier is {telescopeInfo.SideOfPier} but expected to be {targetSideOfPier}. Flip should happen now");
                            }
                            return delayedFlip;
                        }
                    }
                } else {
                    //The minimum time to flip has not been reached yet. Check if a flip is required based on the estimation of the next instruction plus a 2 minute window due to not having side of pier access for dalyed flip evaluation
                    var noRemainingTime = maximumTimeRemaining <= (TimeSpan.FromSeconds(nextInstructionTime) + TimeSpan.FromMinutes(2));

                    if (noRemainingTime) {
                        Logger.Info($"Meridian Flip - No more remaining time available before flip. Max remaining time {maximumTimeRemaining}, next instruction time {nextInstructionTime}. Flip should happen now");
                        return true;
                    } else {
                        Logger.Info($"Meridian Flip - There is still time remainging. Max remaining time {maximumTimeRemaining}, next instruction time {nextInstructionTime}.");
                        return false;
                    }
                }
            }
        }

        protected virtual void UpdateMeridianFlipTimeTriggerValues(TimeSpan minimumTimeRemaining, TimeSpan maximumTimeRemaining, TimeSpan pauseBeforeMeridian, TimeSpan maximumTimeAfterMeridian) {
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

        public virtual bool Validate() {
            var i = new List<string>();
            var meridianFlipSettings = profileService.ActiveProfile.MeridianFlipSettings;
            var telescopeInfo = telescopeMediator.GetInfo();

            if (meridianFlipSettings.Recenter && !cameraMediator.GetInfo().Connected) {
                i.Add(Loc.Instance["LblCameraNotConnected"]);
            }

            if (!telescopeInfo.Connected) {
                i.Add(Loc.Instance["LblTelescopeNotConnected"]);
            }

            if (meridianFlipSettings.AutoFocusAfterFlip) {
                if (!focuserMediator.GetInfo().Connected) {
                    i.Add(Loc.Instance["LblFocuserNotConnected"]);
                }
            }

            Issues = i;
            return i.Count == 0;
        }
    }
}