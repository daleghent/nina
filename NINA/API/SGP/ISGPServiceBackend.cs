using System.Threading.Tasks;

namespace NINA.API.SGP {
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
