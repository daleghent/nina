#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Model.MyTelescope;
using NINA.PlateSolving;
using NINA.Utility;

namespace NINA.ViewModel.Interfaces {

    public interface IPolarAlignmentVM : IDockableVM {
        double AltitudeDeclination { get; set; }
        double AltitudeMeridianOffset { get; set; }
        ApplicationStatus AltitudePolarErrorStatus { get; set; }
        double AzimuthDeclination { get; set; }
        double AzimuthMeridianOffset { get; set; }
        ApplicationStatus AzimuthPolarErrorStatus { get; set; }
        CameraInfo CameraInfo { get; }
        ICommand CancelDARVSlewCommand { get; }
        ICommand CancelMeasureAltitudeErrorCommand { get; }
        ICommand CancelMeasureAzimuthErrorCommand { get; }
        IAsyncCommand DARVSlewCommand { get; }
        double DARVSlewDuration { get; set; }
        double DARVSlewRate { get; set; }
        string DarvStatus { get; set; }
        string HourAngleTime { get; set; }
        IAsyncCommand MeasureAltitudeErrorCommand { get; }
        IAsyncCommand MeasureAzimuthErrorCommand { get; }
        PlateSolveResult PlateSolveResult { get; set; }
        double Rotation { get; set; }
        IAsyncCommand SlewToAltitudeMeridianOffsetCommand { get; }
        IAsyncCommand SlewToAzimuthMeridianOffsetCommand { get; }
        BinningMode SnapBin { get; set; }
        double SnapExposureDuration { get; set; }
        FilterInfo SnapFilter { get; set; }
        int SnapGain { get; set; }
        ApplicationStatus Status { get; set; }
        TelescopeInfo TelescopeInfo { get; }

        void Dispose();

        Task<bool> SlewToMeridianOffset(double meridianOffset, double declination, CancellationToken token);

        void UpdateDeviceInfo(CameraInfo cameraInfo);

        void UpdateDeviceInfo(TelescopeInfo telescopeInfo);
    }
}