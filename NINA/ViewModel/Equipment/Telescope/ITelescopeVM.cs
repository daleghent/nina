#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model.MyTelescope;
using NINA.Utility.Astrometry;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.ViewModel.Equipment.Telescope {

    public interface ITelescopeVM : IDeviceVM<TelescopeInfo>, IDockableVM {

        Task<bool> SlewToCoordinatesAsync(Coordinates coords, CancellationToken token);

        Task<bool> SlewToCoordinatesAsync(TopocentricCoordinates coordinates, CancellationToken token);

        void MoveAxis(TelescopeAxes axis, double rate);

        void PulseGuide(GuideDirections direction, int duration);

        Task<bool> Sync(Coordinates coordinates);

        Task<bool> MeridianFlip(Coordinates targetCoordinates);

        bool SendToSnapPort(bool start);

        Coordinates GetCurrentPosition();

        Task<bool> ParkTelescope(CancellationToken token);

        void UnparkTelescope();

        Task WaitForSlew(CancellationToken cancellationToken);

        bool SetTrackingEnabled(bool tracking);

        bool SetTrackingMode(TrackingMode trackingMode);

        bool SetCustomTrackingRate(double rightAscensionRate, double declinationRate);
    }
}