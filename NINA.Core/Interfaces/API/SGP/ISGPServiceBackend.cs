#region "copyright"

/*
    Copyright © 2016 - 2022 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.API.SGP;
using System.Threading.Tasks;

namespace NINA.Core.Interfaces.API.SGP {

    public interface ISGPServiceBackend {

        SgEnumerateDevicesResponse EnumerateDevices(SgEnumerateDevices input);

        SgGetDeviceStatusResponse GetDeviceStatus(SgGetDeviceStatus input);

        Task<SgConnectDeviceResponse> ConnectDevice(SgConnectDevice input);

        Task<SgDisconnectDeviceResponse> DisconnectDevice(SgDisconnectDevice input);

        SgAbortImageResponse AbortImage();

        SgCaptureImageResponse CaptureImage(SgCaptureImage input);

        SgGetImagePathResponse GetImagePath(SgGetImagePath input);

        SgGetCameraPropsResponse GetCameraProps();

        SgSetCameraCoolerEnabledResponse SetCameraCoolerEnabled(SgSetCameraCoolerEnabled input);

        SgCameraCoolerResponse GetCameraCooler();

        SgGetCameraTempResponse GetCameraTemp();

        SgSetCameraTempResponse SetCameraTemp(SgSetCameraTemp input);
    }
}