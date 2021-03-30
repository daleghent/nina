#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model.MyCamera;
using NINA.Model.MyDome;
using NINA.Model.MyFilterWheel;
using NINA.Model.MyFocuser;
using NINA.Model.MyGuider;
using NINA.Model.MyRotator;
using NINA.Model.MySwitch;
using NINA.Model.MyTelescope;
using NINA.Utility.Mediator.Interfaces;

namespace NINA.Utility {

    public interface IAllDeviceConsumer : ICameraConsumer, IFocuserConsumer, IRotatorConsumer, ITelescopeConsumer, IDomeConsumer, IFilterWheelConsumer, IGuiderConsumer, ISwitchConsumer {
        CameraInfo CameraInfo { get; }
        DomeInfo DomeInfo { get; }
        FilterWheelInfo FilterWheelInfo { get; }
        FocuserInfo FocuserInfo { get; }
        GuiderInfo GuiderInfo { get; }
        RotatorInfo RotatorInfo { get; }
        SwitchInfo SwitchInfo { get; }
        TelescopeInfo TelescopeInfo { get; }
    }
}