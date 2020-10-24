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
using NINA.Profile;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Utility;
using NINA.Sequencer.Validations;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel;
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

        [ImportingConstructor]
        public MeridianFlipTrigger(IProfileService profileService, ICameraMediator cameraMediator, ITelescopeMediator telescopeMediator, IGuiderMediator guiderMediator, IFocuserMediator focuserMediator, IImagingMediator imagingMediator, IApplicationStatusMediator applicationStatusMediator, IFilterWheelMediator filterWheelMediator) : base() {
            this.profileService = profileService;
            this.telescopeMediator = telescopeMediator;
            this.guiderMediator = guiderMediator;
            this.imagingMediator = imagingMediator;
            this.filterWheelMediator = filterWheelMediator;
            this.applicationStatusMediator = applicationStatusMediator;
            this.cameraMediator = cameraMediator;
            this.focuserMediator = focuserMediator;
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
            return new MeridianFlipTrigger(profileService, cameraMediator, telescopeMediator, guiderMediator, focuserMediator, imagingMediator, applicationStatusMediator, filterWheelMediator) {
                Icon = Icon,
                Name = Name,
                Category = Category,
                Description = Description,
            };
        }

        private DateTime flipTime;
        private IFocuserMediator focuserMediator;

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
            return new MeridianFlipVM(profileService, cameraMediator, telescopeMediator, guiderMediator, focuserMediator, imagingMediator, applicationStatusMediator, filterWheelMediator)
                .MeridianFlip(target, TimeSpan.FromHours(info.TimeToMeridianFlip));
        }

        public override void Initialize() {
        }

        public override bool ShouldTrigger(ISequenceItem nextItem) {
            var info = telescopeMediator.GetInfo();
            FlipTime = DateTime.Now + TimeSpan.FromHours(info.TimeToMeridianFlip);
            return MeridianFlipVM.ShouldFlip(profileService, nextItem.GetEstimatedDuration().TotalSeconds, telescopeMediator.GetInfo());
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