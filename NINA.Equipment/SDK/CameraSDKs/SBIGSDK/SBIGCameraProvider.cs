using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Equipment.SDK.CameraSDKs.SBIGSDK.SbigSharp;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Equipment.SDK.CameraSDKs.SBIGSDK {

    public class SBIGCameraProvider : IEquipmentProvider<ICamera> {
        private readonly ISbigSdk sbigSdk;
        private readonly IProfileService profileService;
        private readonly IExposureDataFactory exposureDataFactory;

        public SBIGCameraProvider(ISbigSdk sbigSdk, IProfileService profileService, IExposureDataFactory exposureDataFactory) {
            this.sbigSdk = sbigSdk;
            this.profileService = profileService;
            this.exposureDataFactory = exposureDataFactory;
        }

        public IList<ICamera> GetEquipment() {
            Logger.Debug("Getting SBIG Cameras");

            var devices = new List<ICamera>();
            foreach (var instance in sbigSdk.QueryUsbDevices()) {
                if (instance.CameraType != SbigSharp.SBIG.CameraType.NoCamera) {
                    var cam = new SBIGCamera(sbigSdk, SBIG.CCD.Imaging, instance, profileService, exposureDataFactory);
                    devices.Add(cam);
                }
            }
            return devices;
        }
    }
}