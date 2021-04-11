using NINA.Core.API.SGP;
using NINA.Core.Interfaces.API.SGP;
using System;
using System.ServiceModel;

namespace NINA.API.SGP {

    [ServiceBehavior(
        ConcurrencyMode = ConcurrencyMode.Multiple,
        InstanceContextMode = InstanceContextMode.Single)]
    public class SGPServiceFrontend : ISGPService {
        private readonly ISGPServiceBackend serviceBackend;

        public SGPServiceFrontend(ISGPServiceBackend serviceBackend) {
            this.serviceBackend = serviceBackend;
        }

        public SgAbortImageResponse SgAbortImage_Post() {
            return serviceBackend.AbortImage();
        }

        public SgGetCameraPropsResponse SgGetCameraProps_Post() {
            return serviceBackend.GetCameraProps();
        }

        public SgGetCameraTempResponse SgGetCameraTemp_Post() {
            return serviceBackend.GetCameraTemp();
        }

        public SgCaptureImageResponse SgCaptureImage_PostWithBody(SgCaptureImage input) {
            return serviceBackend.CaptureImage(input);
        }

        public SgConnectDeviceResponse SgConnectDevice_PostWithBody(SgConnectDevice input) {
            return serviceBackend.ConnectDevice(input).Result;
        }

        public SgDisconnectDeviceResponse SgDisconnectDevice_PostWithBody(SgDisconnectDevice input) {
            return serviceBackend.DisconnectDevice(input).Result;
        }

        public SgEnumerateDevicesResponse SgEnumerateDevices_PostWithBody(SgEnumerateDevices input) {
            return serviceBackend.EnumerateDevices(input);
        }

        public SgGetDeviceStatusResponse SgGetDeviceStatus_PostWithBody(SgGetDeviceStatus input) {
            return serviceBackend.GetDeviceStatus(input);
        }

        public SgGetImagePathResponse SgGetImagePath_PostWithBody(SgGetImagePath input) {
            return serviceBackend.GetImagePath(input);
        }

        public SgSetCameraTempResponse SgSetCameraTemp_PostWithBody(SgSetCameraTemp input) {
            return serviceBackend.SetCameraTemp(input);
        }

        public SgSetCameraCoolerEnabledResponse SgSetCameraCoolerEnabled_PostWithBody(SgSetCameraCoolerEnabled input) {
            return serviceBackend.SetCameraCoolerEnabled(input);
        }

        public SgCameraCoolerResponse SgCameraCooler_Post() {
            return serviceBackend.GetCameraCooler();
        }

        #region Adapter Methods

        public SgConnectDeviceResponse SgConnectDevice_Post(string device, string deviceName) {
            var input = new SgConnectDevice() {
                Device = (DeviceType)Enum.Parse(typeof(DeviceType), device),
                DeviceName = deviceName
            };
            return SgConnectDevice_PostWithBody(input);
        }

        public SgDisconnectDeviceResponse SgDisconnectDevice_Post(string device) {
            var input = new SgDisconnectDevice() {
                Device = (DeviceType)Enum.Parse(typeof(DeviceType), device)
            };
            return SgDisconnectDevice_PostWithBody(input);
        }

        public SgEnumerateDevicesResponse SgEnumerateDevices_Post(string device) {
            var input = new SgEnumerateDevices() {
                Device = (DeviceType)Enum.Parse(typeof(DeviceType), device)
            };
            return SgEnumerateDevices_PostWithBody(input);
        }

        public SgGetDeviceStatusResponse SgGetDeviceStatus_Post(string device) {
            var input = new SgGetDeviceStatus() {
                Device = (DeviceType)Enum.Parse(typeof(DeviceType), device)
            };
            return SgGetDeviceStatus_PostWithBody(input);
        }

        public SgGetImagePathResponse SgGetImagePath_Post(string receipt) {
            var input = new SgGetImagePath() {
                Receipt = Guid.Parse(receipt)
            };
            return SgGetImagePath_PostWithBody(input);
        }

        public SgSetCameraTempResponse SgSetCameraTemp_Post(string temperature) {
            var input = new SgSetCameraTemp() {
                Temperature = double.Parse(temperature)
            };
            return SgSetCameraTemp_PostWithBody(input);
        }

        public SgSetCameraCoolerEnabledResponse SgSetCameraCoolerEnabled_Post(string enabled) {
            var input = new SgSetCameraCoolerEnabled() {
                Enabled = bool.Parse(enabled)
            };
            return SgSetCameraCoolerEnabled_PostWithBody(input);
        }

        public SgGetDeviceStatusResponse SgGetDeviceStatus_Get(string device) {
            return SgGetDeviceStatus_Post(device);
        }

        public SgAbortImageResponse SgAbortImage_Get() {
            return SgAbortImage_Post();
        }

        public SgConnectDeviceResponse SgConnectDevice_Get(string device, string deviceName) {
            return SgConnectDevice_Post(device, deviceName);
        }

        public SgDisconnectDeviceResponse SgDisconnectDevice_Get(string device) {
            return SgDisconnectDevice_Post(device);
        }

        public SgEnumerateDevicesResponse SgEnumerateDevices_Get(string device) {
            return SgEnumerateDevices_Post(device);
        }

        public SgGetCameraPropsResponse SgGetCameraProps_Get() {
            return SgGetCameraProps_Post();
        }

        public SgGetCameraTempResponse SgGetCameraTemp_Get() {
            return SgGetCameraTemp_Post();
        }

        public SgGetImagePathResponse SgGetImagePath_Get(string receipt) {
            return SgGetImagePath_Post(receipt);
        }

        public SgSetCameraTempResponse SgSetCameraTemp_Get(string temperature) {
            return SgSetCameraTemp_Post(temperature);
        }

        public SgSetCameraCoolerEnabledResponse SgSetCameraCoolerEnabled_Get(string enabled) {
            return SgSetCameraCoolerEnabled_Post(enabled);
        }

        public SgCameraCoolerResponse SgCameraCooler_Get() {
            return SgCameraCooler_Post();
        }

        #endregion Adapter Methods
    }
}