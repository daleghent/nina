#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Astrometry;
using System;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Model;
using NINA.Equipment.Interfaces.ViewModel;

namespace NINA.Equipment.Interfaces.Mediator {

    public interface ITelescopeMediator : IDeviceMediator<ITelescopeVM, ITelescopeConsumer, TelescopeInfo> {

        void MoveAxis(TelescopeAxes axis, double rate);

        void PulseGuide(GuideDirections direction, int duration);

        Task<bool> Sync(Coordinates coordinates);

        Task<bool> SlewToCoordinatesAsync(Coordinates coords, CancellationToken token);

        Task<bool> SlewToCoordinatesAsync(TopocentricCoordinates coords, CancellationToken token);

        Task<bool> MeridianFlip(Coordinates targetCoordinates);

        bool SetTrackingEnabled(bool trackingEnabled);

        bool SetTrackingMode(TrackingMode trackingMode);

        bool SetCustomTrackingRate(double rightAscensionRate, double declinationRate);

        bool SendToSnapPort(bool start);

        Coordinates GetCurrentPosition();

        Task<bool> ParkTelescope(IProgress<ApplicationStatus> progress, CancellationToken token);

        void UnparkTelescope();

        Task WaitForSlew(CancellationToken cancellationToken);

        Task<bool> FindHome(IProgress<ApplicationStatus> progress, CancellationToken token);
    }
}