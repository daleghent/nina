using NINA.Core.API.SGP;
using System;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace NINA.Core.Interfaces.API.SGP {

    [ServiceContract]
    public interface ISGPService {

        [OperationContract]
        [WebInvoke(
            Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            UriTemplate = "image")]
        SgCaptureImageResponse SgCaptureImage_PostWithBody(SgCaptureImage input);

        [OperationContract]
        [WebInvoke(
            Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            UriTemplate = "devicestatus/{device}")]
        SgGetDeviceStatusResponse SgGetDeviceStatus_Get(String device);

        [OperationContract]
        [WebInvoke(
            Method = "POST",
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            UriTemplate = "devicestatus/{device}")]
        SgGetDeviceStatusResponse SgGetDeviceStatus_Post(String device);

        [OperationContract]
        [WebInvoke(
            Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            UriTemplate = "devicestatus")]
        SgGetDeviceStatusResponse SgGetDeviceStatus_PostWithBody(SgGetDeviceStatus input);

        [OperationContract]
        [WebInvoke(
            Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            UriTemplate = "abortimage")]
        SgAbortImageResponse SgAbortImage_Get();

        [OperationContract]
        [WebInvoke(
            Method = "POST",
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            UriTemplate = "abortimage")]
        SgAbortImageResponse SgAbortImage_Post();

        [OperationContract]
        [WebInvoke(
            Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            UriTemplate = "connectdevice/{device}/{deviceName}")]
        SgConnectDeviceResponse SgConnectDevice_Get(string device, string deviceName);

        [OperationContract]
        [WebInvoke(
            Method = "POST",
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            UriTemplate = "connectdevice/{device}/{deviceName}")]
        SgConnectDeviceResponse SgConnectDevice_Post(string device, string deviceName);

        [OperationContract]
        [WebInvoke(
            Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            UriTemplate = "connectdevice")]
        SgConnectDeviceResponse SgConnectDevice_PostWithBody(SgConnectDevice input);

        [OperationContract]
        [WebInvoke(
            Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            UriTemplate = "disconnectdevice/{device}")]
        SgDisconnectDeviceResponse SgDisconnectDevice_Get(string device);

        [OperationContract]
        [WebInvoke(
            Method = "POST",
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            UriTemplate = "disconnectdevice/{device}")]
        SgDisconnectDeviceResponse SgDisconnectDevice_Post(string device);

        [OperationContract]
        [WebInvoke(
            Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            UriTemplate = "disconnectdevice")]
        SgDisconnectDeviceResponse SgDisconnectDevice_PostWithBody(SgDisconnectDevice input);

        [OperationContract]
        [WebInvoke(
            Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            UriTemplate = "enumdevices/{device}")]
        SgEnumerateDevicesResponse SgEnumerateDevices_Get(string device);

        [OperationContract]
        [WebInvoke(
            Method = "POST",
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            UriTemplate = "enumdevices/{device}")]
        SgEnumerateDevicesResponse SgEnumerateDevices_Post(string device);

        [OperationContract]
        [WebInvoke(
            Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            UriTemplate = "enumdevices")]
        SgEnumerateDevicesResponse SgEnumerateDevices_PostWithBody(SgEnumerateDevices input);

        [OperationContract]
        [WebInvoke(
            Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            UriTemplate = "cameraprops")]
        SgGetCameraPropsResponse SgGetCameraProps_Get();

        [OperationContract]
        [WebInvoke(
            Method = "POST",
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            UriTemplate = "cameraprops")]
        SgGetCameraPropsResponse SgGetCameraProps_Post();

        [OperationContract]
        [WebInvoke(
            Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            UriTemplate = "cameratemp")]
        SgGetCameraTempResponse SgGetCameraTemp_Get();

        [OperationContract]
        [WebInvoke(
            Method = "POST",
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            UriTemplate = "cameratemp")]
        SgGetCameraTempResponse SgGetCameraTemp_Post();

        [OperationContract]
        [WebInvoke(
            Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            UriTemplate = "imagepath/{receipt}")]
        SgGetImagePathResponse SgGetImagePath_Get(string receipt);

        [OperationContract]
        [WebInvoke(
            Method = "POST",
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            UriTemplate = "imagepath/{receipt}")]
        SgGetImagePathResponse SgGetImagePath_Post(string receipt);

        [OperationContract]
        [WebInvoke(
            Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            UriTemplate = "imagepath")]
        SgGetImagePathResponse SgGetImagePath_PostWithBody(SgGetImagePath input);

        [OperationContract]
        [WebInvoke(
            Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            UriTemplate = "setcameratemp/{temperature}")]
        SgSetCameraTempResponse SgSetCameraTemp_Get(string temperature);

        [OperationContract]
        [WebInvoke(
            Method = "POST",
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            UriTemplate = "setcameratemp/{temperature}")]
        SgSetCameraTempResponse SgSetCameraTemp_Post(string temperature);

        [OperationContract]
        [WebInvoke(
            Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            UriTemplate = "setcameratemp")]
        SgSetCameraTempResponse SgSetCameraTemp_PostWithBody(SgSetCameraTemp input);

        [OperationContract]
        [WebInvoke(
            Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            UriTemplate = "setcameracooler/{enabled}")]
        SgSetCameraCoolerEnabledResponse SgSetCameraCoolerEnabled_Get(string enabled);

        [OperationContract]
        [WebInvoke(
            Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            UriTemplate = "setcameracooler/{enabled}")]
        SgSetCameraCoolerEnabledResponse SgSetCameraCoolerEnabled_Post(string enabled);

        [OperationContract]
        [WebInvoke(
            Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            UriTemplate = "setcameracooler")]
        SgSetCameraCoolerEnabledResponse SgSetCameraCoolerEnabled_PostWithBody(SgSetCameraCoolerEnabled input);

        [OperationContract]
        [WebInvoke(
            Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            UriTemplate = "cameracooler/")]
        SgCameraCoolerResponse SgCameraCooler_Get();

        [OperationContract]
        [WebInvoke(
            Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            UriTemplate = "cameracooler/")]
        SgCameraCoolerResponse SgCameraCooler_Post();
    }
}