#region "copyright"
/*
    Copyright © 2016 - 2023 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NINA.Core.API.SGP;
using NINA.Core.Interfaces.API.SGP;
using System;

namespace NINA.API.SGP {

    [ApiController]
    public class SGPServerEmulation : ControllerBase {
        private readonly ILogger<SGPServerEmulation> _logger;
        private readonly ISGPServiceBackend serviceBackend;

        public SGPServerEmulation(ILogger<SGPServerEmulation> logger, ISGPServiceBackend serviceBackend) {
            _logger = logger;
            this.serviceBackend = serviceBackend;
        }

        [HttpPost]
        [Route("abortimage")]
        public SgAbortImageResponse SgAbortImage_Post() {
            return serviceBackend.AbortImage();
        }

        [HttpPost]
        [Route("cameraprops")]
        public SgGetCameraPropsResponse SgGetCameraProps_Post() {
            return serviceBackend.GetCameraProps();
        }

        [HttpPost]
        [Route("cameratemp")]
        public SgGetCameraTempResponse SgGetCameraTemp_Post() {
            return serviceBackend.GetCameraTemp();
        }

        [HttpPost]
        [Route("image")]
        public SgCaptureImageResponse SgCaptureImage_PostWithBody(SgCaptureImage input) {
            return serviceBackend.CaptureImage(input);
        }

        [HttpPost]
        [Route("connectdevice")]
        public SgConnectDeviceResponse SgConnectDevice_PostWithBody(SgConnectDevice input) {
            return serviceBackend.ConnectDevice(input).Result;
        }

        [HttpPost]
        [Route("disconnectdevice")]
        public SgDisconnectDeviceResponse SgDisconnectDevice_PostWithBody(SgDisconnectDevice input) {
            return serviceBackend.DisconnectDevice(input).Result;
        }

        [HttpPost]
        [Route("enumdevices")]
        public SgEnumerateDevicesResponse SgEnumerateDevices_PostWithBody(SgEnumerateDevices input) {
            return serviceBackend.EnumerateDevices(input).Result;
        }

        [HttpPost]
        //[Consumes("application/json")]
        [Route("devicestatus")]
        public SgGetDeviceStatusResponse SgGetDeviceStatus_PostWithBody(SgGetDeviceStatus input) {
            return serviceBackend.GetDeviceStatus(input);
        }

        [HttpPost]
        [Route("imagepath")]
        public SgGetImagePathResponse SgGetImagePath_PostWithBody(SgGetImagePath input) {
            return serviceBackend.GetImagePath(input);
        }

        [HttpPost]
        [Route("setcameratemp")]
        public SgSetCameraTempResponse SgSetCameraTemp_PostWithBody(SgSetCameraTemp input) {
            return serviceBackend.SetCameraTemp(input);
        }

        [HttpPost]
        [Route("setcameracooler")]
        public SgSetCameraCoolerEnabledResponse SgSetCameraCoolerEnabled_PostWithBody(SgSetCameraCoolerEnabled input) {
            return serviceBackend.SetCameraCoolerEnabled(input);
        }

        [HttpPost]
        [Route("cameracooler")]
        public SgCameraCoolerResponse SgCameraCooler_Post() {
            return serviceBackend.GetCameraCooler();
        }

        #region Adapter Methods

        [HttpPost]
        [Route("connectdevice/{device}/{deviceName}")]
        public SgConnectDeviceResponse SgConnectDevice_Post(string device, string deviceName) {
            var input = new SgConnectDevice() {
                Device = (DeviceType)Enum.Parse(typeof(DeviceType), device),
                DeviceName = deviceName
            };
            return SgConnectDevice_PostWithBody(input);
        }

        [HttpPost]
        [Route("disconnectdevice/{device}")]
        public SgDisconnectDeviceResponse SgDisconnectDevice_Post(string device) {
            var input = new SgDisconnectDevice() {
                Device = (DeviceType)Enum.Parse(typeof(DeviceType), device)
            };
            return SgDisconnectDevice_PostWithBody(input);
        }

        [HttpPost]
        [Route("enumdevices/{device}")]
        public SgEnumerateDevicesResponse SgEnumerateDevices_Post(string device) {
            var input = new SgEnumerateDevices() {
                Device = (DeviceType)Enum.Parse(typeof(DeviceType), device)
            };
            return SgEnumerateDevices_PostWithBody(input);
        }

        [HttpPost]
        [Route("devicestatus/{device}")]
        public SgGetDeviceStatusResponse SgGetDeviceStatus_Post(string device) {
            var input = new SgGetDeviceStatus() {
                Device = (DeviceType)Enum.Parse(typeof(DeviceType), device)
            };
            return SgGetDeviceStatus_PostWithBody(input);
        }

        [HttpPost]
        [Route("imagepath/{receipt}")]
        public SgGetImagePathResponse SgGetImagePath_Post(string receipt) {
            var input = new SgGetImagePath() {
                Receipt = Guid.Parse(receipt)
            };
            return SgGetImagePath_PostWithBody(input);
        }

        [HttpPost]
        [Route("setcameratemp/{temperature}")]
        public SgSetCameraTempResponse SgSetCameraTemp_Post(string temperature) {
            var input = new SgSetCameraTemp() {
                Temperature = double.Parse(temperature)
            };
            return SgSetCameraTemp_PostWithBody(input);
        }

        [HttpPost]
        [Route("setcameracooler/{enabled}")]
        public SgSetCameraCoolerEnabledResponse SgSetCameraCoolerEnabled_Post(string enabled) {
            var input = new SgSetCameraCoolerEnabled() {
                Enabled = bool.Parse(enabled)
            };
            return SgSetCameraCoolerEnabled_PostWithBody(input);
        }

        [HttpGet]
        [Route("devicestatus/{device}")]
        public SgGetDeviceStatusResponse SgGetDeviceStatus_Get(string device) {
            return SgGetDeviceStatus_Post(device);
        }

        [HttpGet]
        [Route("abortimage")]
        public SgAbortImageResponse SgAbortImage_Get() {
            return SgAbortImage_Post();
        }

        [HttpGet]
        [Route("connectdevice/{device}/{deviceName}")]
        public SgConnectDeviceResponse SgConnectDevice_Get(string device, string deviceName) {
            return SgConnectDevice_Post(device, deviceName);
        }

        [HttpGet]
        [Route("disconnectdevice/{device}")]
        public SgDisconnectDeviceResponse SgDisconnectDevice_Get(string device) {
            return SgDisconnectDevice_Post(device);
        }

        [HttpGet]
        [Route("enumdevices/{device}")]
        public SgEnumerateDevicesResponse SgEnumerateDevices_Get(string device) {
            return SgEnumerateDevices_Post(device);
        }

        [HttpGet]
        [Route("cameraprops")]
        public SgGetCameraPropsResponse SgGetCameraProps_Get() {
            return SgGetCameraProps_Post();
        }

        [HttpGet]
        [Route("cameratemp")]
        public SgGetCameraTempResponse SgGetCameraTemp_Get() {
            return SgGetCameraTemp_Post();
        }

        [HttpGet]
        [Route("imagepath/{receipt}")]
        public SgGetImagePathResponse SgGetImagePath_Get(string receipt) {
            return SgGetImagePath_Post(receipt);
        }

        [HttpGet]
        [Route("setcameratemp/{temperature}")]
        public SgSetCameraTempResponse SgSetCameraTemp_Get(string temperature) {
            return SgSetCameraTemp_Post(temperature);
        }

        [HttpGet]
        [Route("setcameracooler/{enabled}")]
        public SgSetCameraCoolerEnabledResponse SgSetCameraCoolerEnabled_Get(string enabled) {
            return SgSetCameraCoolerEnabled_Post(enabled);
        }

        [HttpGet]
        [Route("cameracooler")]
        public SgCameraCoolerResponse SgCameraCooler_Get() {
            return SgCameraCooler_Post();
        }

        #endregion Adapter Methods
    }
}