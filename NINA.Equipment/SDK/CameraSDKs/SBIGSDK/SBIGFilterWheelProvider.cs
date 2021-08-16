using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Equipment.SDK.CameraSDKs.SBIGSDK {
    public class SBIGFilterWheelProvider : IEquipmentProvider<IFilterWheel> {
        private readonly ISbigSdk sbigSdk;
        private readonly IProfileService profileService;

        public SBIGFilterWheelProvider(ISbigSdk sbigSdk, IProfileService profileService) {
            this.sbigSdk = sbigSdk;
            this.profileService = profileService;
        }

        public IList<IFilterWheel> GetEquipment() {
            Logger.Debug("Getting SBIG Filter Wheels");

            var devices = new List<IFilterWheel>();
            foreach (var instance in sbigSdk.QueryUsbDevices()) {
                if (instance.FilterWheelInfo.HasValue && instance.FilterWheelInfo.Value.Model != SDK.CameraSDKs.SBIGSDK.SbigSharp.SBIG.CfwModelSelect.CFWSEL_UNKNOWN) {
                    var fw = new SBIGFilterWheel(sbigSdk, instance, profileService);
                    devices.Add(fw);
                }
            }
            return devices;
        }
    }
}
