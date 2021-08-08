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
using NINA.Equipment.SDK.CameraSDKs.SVBonySDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Interfaces {

    public interface ISVBonySDK {
        bool Connected { get; }

        void Connect();

        void Disconnect();

        int[] GetBinningInfo();

        (int, int) GetDimensions();

        int GetGain();

        int GetMaxGain();

        int GetMaxOffset();

        int GetMaxUSBLimit();

        int GetMinGain();

        int GetMinOffset();

        int GetMinUSBLimit();

        int GetOffset();

        double GetPixelSize();

        int GetUSBLimit();

        SensorType GetSensorInfo();

        bool SetGain(int value);

        bool SetOffset(int value);

        bool SetUSBLimit(int value);

        double GetMaxExposureTime();

        double GetMinExposureTime();

        Task<ushort[]> StartExposure(double exposureTime, int width, int height, CancellationToken ct);

        bool SetROI(int startX, int startY, int width, int height, int binning);

        int GetBitDepth();

        (int, int, int, int, int) GetROI();
    }
}