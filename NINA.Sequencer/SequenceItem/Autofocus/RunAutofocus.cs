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
using NINA.Model.MyFilterWheel;
using NINA.Profile;
using NINA.Sequencer.Validations;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.WindowService;
using NINA.ViewModel;
using NINA.ViewModel.AutoFocus;
using NINA.ViewModel.ImageHistory;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.SequenceItem.Autofocus {

    [ExportMetadata("Name", "Lbl_SequenceItem_Autofocus_RunAutofocus_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Autofocus_RunAutofocus_Description")]
    [ExportMetadata("Icon", "AutoFocusSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Focuser")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class RunAutofocus : SequenceItem, IValidatable {
        private IProfileService profileService;
        private IImageHistoryVM history;
        private ICameraMediator cameraMediator;
        private IFilterWheelMediator filterWheelMediator;
        private IFocuserMediator focuserMediator;
        private IGuiderMediator guiderMediator;
        private IImagingMediator imagingMediator;
        private IApplicationStatusMediator applicationStatusMediator;

        [ImportingConstructor]
        public RunAutofocus(IProfileService profileService, IImageHistoryVM history, ICameraMediator cameraMediator, IFilterWheelMediator filterWheelMediator, IFocuserMediator focuserMediator, IGuiderMediator guiderMediator, IImagingMediator imagingMediator, IApplicationStatusMediator applicationStatusMediator) {
            this.profileService = profileService;
            this.history = history;
            this.cameraMediator = cameraMediator;
            this.filterWheelMediator = filterWheelMediator;
            this.focuserMediator = focuserMediator;
            this.guiderMediator = guiderMediator;
            this.imagingMediator = imagingMediator;
            this.applicationStatusMediator = applicationStatusMediator;
            AutoFocusVMFactory = new AutoFocusVMFactory(profileService, cameraMediator, filterWheelMediator, focuserMediator, guiderMediator, imagingMediator, applicationStatusMediator);
        }

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        public IWindowServiceFactory WindowServiceFactory { get; set; } = new WindowServiceFactory();
        public IAutoFocusVMFactory AutoFocusVMFactory { get; set; }

        public override object Clone() {
            return new RunAutofocus(profileService, history, cameraMediator, filterWheelMediator, focuserMediator, guiderMediator, imagingMediator, applicationStatusMediator) {
                Icon = Icon,
                Name = Name,
                Category = Category,
                Description = Description,
            };
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            using (var autoFocus = AutoFocusVMFactory.Create()) {
                var service = WindowServiceFactory.Create();
                service.Show(autoFocus, autoFocus.Title, System.Windows.ResizeMode.CanResize, System.Windows.WindowStyle.ToolWindow);
                try {
                    FilterInfo filter = null;
                    var selectedFilter = filterWheelMediator.GetInfo()?.SelectedFilter;
                    if (selectedFilter != null) {
                        filter = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters.Where(x => x.Position == selectedFilter.Position).FirstOrDefault();
                    }

                    var report = await autoFocus.StartAutoFocus(filter, token, progress);
                    history.AppendAutoFocusPoint(report);
                } finally {
                    service.DelayedClose(TimeSpan.FromSeconds(10));
                }
            }
        }

        public bool Validate() {
            var i = new List<string>();
            if (!cameraMediator.GetInfo().Connected) {
                i.Add(Locale.Loc.Instance["LblCameraNotConnected"]);
            }
            if (!focuserMediator.GetInfo().Connected) {
                i.Add(Locale.Loc.Instance["LblFocuserNotConnected"]);
            }

            Issues = i;
            return issues.Count == 0;
        }

        public override void AfterParentChanged() {
            Validate();
        }

        public override TimeSpan GetEstimatedDuration() {
            var filter = filterWheelMediator.GetInfo()?.SelectedFilter;

            var exposureTime = profileService.ActiveProfile.FocuserSettings.AutoFocusExposureTime;
            if (filter != null) {
                var filterTime = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters[filter.Position].AutoFocusExposureTime;
                exposureTime = filterTime > 0 ? filterTime : exposureTime;
            }

            // + 2 because the autofocus will take an initial exposure and a final exposure to evaluate the run
            var steps = profileService.ActiveProfile.FocuserSettings.AutoFocusInitialOffsetSteps * 2 * profileService.ActiveProfile.FocuserSettings.AutoFocusNumberOfFramesPerPoint + 2;
            var settleTime = profileService.ActiveProfile.FocuserSettings.FocuserSettleTime;

            var time = steps * (exposureTime + settleTime);

            return TimeSpan.FromSeconds(time);
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(RunAutofocus)}";
        }
    }
}