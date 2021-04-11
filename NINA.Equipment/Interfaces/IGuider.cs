#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Interfaces {

    public interface IGuider : IDevice {
        double PixelScale { get; set; }
        string State { get; }
        bool CanClearCalibration { get; }

        event EventHandler<IGuideStep> GuideEvent;

        Task<bool> AutoSelectGuideStar();

        //Task<bool> Pause(bool pause, CancellationToken ct);

        Task<bool> StartGuiding(bool forceCalibration, CancellationToken ct);

        Task<bool> StopGuiding(CancellationToken ct);

        Task<bool> Dither(CancellationToken ct);

        Task<bool> ClearCalibration(CancellationToken ct);
    }

    public interface IGuiderAppState {
        string State { get; }
    }
}