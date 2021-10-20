#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Profile.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces;
using NINA.WPF.Base.ViewModel.AutoFocus;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.Image.ImageAnalysis;

namespace NINA.WPF.Base.ViewModel {

    public class AutoFocusVMFactory : IAutoFocusVMFactory {
        private readonly IProfileService profileService;
        private readonly ICameraMediator cameraMediator;
        private readonly IFilterWheelMediator filterWheelMediator;
        private readonly IFocuserMediator focuserMediator;
        private readonly IGuiderMediator guiderMediator;
        private readonly IImagingMediator imagingMediator;
        private readonly IStarDetection starDetection;
        private readonly IStarAnnotator starAnnotator;

        public AutoFocusVMFactory(
                IProfileService profileService,
                ICameraMediator cameraMediator,
                IFilterWheelMediator filterWheelMediator,
                IFocuserMediator focuserMediator,
                IGuiderMediator guiderMediator,
                IImagingMediator imagingMediator,
                IStarDetection starDetection,
                IStarAnnotator starAnnotator) {
            this.profileService = profileService;
            this.cameraMediator = cameraMediator;
            this.filterWheelMediator = filterWheelMediator;
            this.focuserMediator = focuserMediator;
            this.guiderMediator = guiderMediator;
            this.imagingMediator = imagingMediator;
            this.starDetection = starDetection;
            this.starAnnotator = starAnnotator;
        }

        public IAutoFocusVM Create() {
            return new AutoFocusVM(profileService, cameraMediator, filterWheelMediator, focuserMediator, guiderMediator, imagingMediator, starDetection, starAnnotator);
        }
    }
}