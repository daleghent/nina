using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Equipment.SDK.CameraSDKs.SBIGSDK {
    public class SBIGCameraProvider : IEquipmentProvider<ICamera> {
        private IProfileService profileService;
        private ISbigSdk sbigSdk;

        public SBIGCameraProvider(IProfileService profileService, ISbigSdk sbigSdk) {
            this.profileService = profileService;
            this.sbigSdk = sbigSdk;
        }

        public IList<ICamera> GetEquipment() {
            Logger.Debug("Getting SBIG Cameras");

            try {
                var devices = new List<ICamera>();
                sbigSdk.InitSdk();
                foreach (var instance in sbigSdk.QueryUsbCameras()) {
                    var cam = new SBIGCamera(sbigSdk, instance);
                    devices.Add(cam);
                }
                return devices;
            } finally {
                sbigSdk.ReleaseSdk();
            }
        }
    }
}
