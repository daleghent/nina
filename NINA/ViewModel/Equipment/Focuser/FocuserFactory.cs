using System;
using NINA.Model;
using NINA.Model.MyFocuser;
using NINA.Profile;
using NINA.Utility;
using System.Collections.Generic;

namespace NINA.ViewModel.Equipment.Focuser {

    internal class FocuserFactory : IDeviceFactory {
        private readonly IProfileService profileService;

        public FocuserFactory(IProfileService profileService) {
            this.profileService = profileService;
        }

        public IList<IDevice> GetDevices() {
            var result = new List<IDevice> {
                new DummyDevice(Locale.Loc.Instance["LblNoFocuser"]),
                new UltimatePowerboxV2(profileService)
            };
            try {
                result.AddRange(ASCOMInteraction.GetFocusers(profileService));
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            return result;
        }
    }
}