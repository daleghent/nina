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
using NINA.Model.MyTelescope;
using NINA.Profile;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Utility;
using NINA.Sequencer.Validations;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel;
using NINA.ViewModel.ImageHistory;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
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

        private DateTime flipTime;
        private IFocuserMediator focuserMediator;
        private IImageHistoryVM history;

        public DateTime FlipTime {
            get => flipTime;
            private set {
                flipTime = value;
                RaisePropertyChanged();
            }
        }

        public override Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            var target = ItemUtility.RetrieveContextCoordinates(context).Item1 ?? telescopeMediator.GetCurrentPosition();

            //Todo: The MeridianFlipVM could be completely replaced by sequential instructions and dedicated ui template
            var info = telescopeMediator.GetInfo();

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

            //Update the FlipTime
            FlipTime = DateTime.Now + TimeSpan.FromHours(telescopeInfo.TimeToMeridianFlip);

            if ((DateTime.Now - lastFlipTime) < TimeSpan.FromHours(11)) {
                //A flip for the same target is only expected every 12 hours on planet earth
                Logger.Debug($"Meridian Flip - Flip for the current target already happened at {lastFlipTime }. Flip will be skipped");
                return false;
            }

            var exposureTime = nextItem.GetEstimatedDuration().TotalSeconds;

            //The time to meridian flip reported by the telescop is the latest time for a flip to happen
            var minimumTimeRemaining = CalculateMinimumTimeRemaining();
            var maximumTimeRemaining = CalculateMaximumTimeRemainaing();
            if (settings.PauseTimeBeforeMeridian != 0) {
                //A pause prior to a meridian flip is a hard limit due to equipment obstruction. There is no possibility for a timerange as we have to pause early and wait for meridian to pass
                minimumTimeRemaining = minimumTimeRemaining - TimeSpan.FromMinutes(profileService.ActiveProfile.MeridianFlipSettings.MinutesAfterMeridian) - TimeSpan.FromMinutes(profileService.ActiveProfile.MeridianFlipSettings.PauseTimeBeforeMeridian);
                maximumTimeRemaining = minimumTimeRemaining;
            }

            if (minimumTimeRemaining <= TimeSpan.Zero && maximumTimeRemaining > TimeSpan.Zero) {
                // We are in the zone between the minimum time and the maximum time. Flip is possible now.
                if (settings.UseSideOfPier) {
                    //Flip when the telescope is on the west side
                    return telescopeInfo.SideOfPier == PierSide.pierWest;
                } else {
                    //No pier info is available. Flip now.
                    Logger.Debug("Meridian Flip - Remaining Time is between minimum and maximum flip time. Flip should happen now");
                    return true;
                }
            } else {
                //The minimum time to flip has not been reached yet. Check if a flip is required based on the estimation of the next instruction
                var noRemainingTime = maximumTimeRemaining <= TimeSpan.FromSeconds(exposureTime);

                if (settings.UseSideOfPier) {
                    if (telescopeInfo.SideOfPier == PierSide.pierEast) {
                        Logger.Debug("Meridian Flip - Telescope reports East Side of Pier, Automated Flip will not be performed.");
                        return false;
                    } else {
                        if (noRemainingTime) {
                            Logger.Info("Meridian Flip - No more remaining time available before flip. Flip should happen now");
                            return true;
                        }

                        //When pier side is still West, but remaining time indicating that a flip happend, the flip seems to have not happened yet and must be done immediately
                        var delayedFlip = maximumTimeRemaining
                            >= (TimeSpan.FromSeconds(12 * 60 * 60)
                                - TimeSpan.FromMinutes(settings.MaxMinutesAfterMeridian)
                                - TimeSpan.FromMinutes(settings.PauseTimeBeforeMeridian)
                              );

                        if (delayedFlip) {
                            Logger.Info("Meridian Flip - Flip seems to not happened in time as Side Of Pier is West but expected to be East. Flip should happen now");
                        }
                        return delayedFlip;
                    }
                } else {
                    if (noRemainingTime) {
                        Logger.Info("Meridian Flip - No more remaining time available before flip. Flip should happen now");
                        return true;
                    }
                }

                return false;
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