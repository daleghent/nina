#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.ViewModel.Equipment.Camera;
using NINA.ViewModel.Equipment.Dome;
using NINA.ViewModel.Equipment.FilterWheel;
using NINA.ViewModel.Equipment.FlatDevice;
using NINA.ViewModel.Equipment.Focuser;
using NINA.ViewModel.Equipment.Guider;
using NINA.ViewModel.Equipment.Rotator;
using NINA.ViewModel.Equipment.Switch;
using NINA.ViewModel.Equipment.Telescope;
using NINA.ViewModel.Equipment.WeatherData;

namespace NINA.ViewModel.Interfaces {

    public interface IEquipmentVM {
        ICameraVM CameraVM { get; }
        IDomeVM DomeVM { get; }
        IFilterWheelVM FilterWheelVM { get; }
        IFlatDeviceVM FlatDeviceVM { get; }
        IFocuserVM FocuserVM { get; }
        IGuiderVM GuiderVM { get; }
        IRotatorVM RotatorVM { get; }
        ISwitchVM SwitchVM { get; }
        ITelescopeVM TelescopeVM { get; }
        IWeatherDataVM WeatherDataVM { get; }
    }
}