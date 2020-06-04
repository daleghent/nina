using NINA.Profile;
using NINA.Utility;
using NINA.Utility.AtikSDK;
using NINA.Utility.Notification;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyFilterWheel {
    internal class AtikInternalFilterWheel : AtikFilterWheelBase {
        private readonly int filterWheelDeviceId;
        private AtikCameraDll.ArtemisPropertiesStruct info;
        private IntPtr cameraDevice = IntPtr.Zero;
        private bool connected = false;

        public AtikInternalFilterWheel(int deviceId, IProfileService profileService) : base(profileService) {
            filterWheelDeviceId = deviceId;
            info = AtikCameraDll.GetCameraProperties(filterWheelDeviceId);
        }

        public bool CameraHasInternalFilterWheel => (info.cameraflags & (1 << 11 - 1)) != 0;

        public override short Position {
            get {
                if (AtikCameraDll.GetInternalFilterWheelIsMoving(cameraDevice)) return -1;
                else return AtikCameraDll.GetInternalFilterWheelCurrentPosition(cameraDevice);
            }
            set => AtikCameraDll.SetInternalFilterWheelTargetPosition(cameraDevice, value);
        }

        public override string Id => CleanedUpString(info.Description) + " (internal)";

        public override string Name => CleanedUpString(info.Description) + " (internal)";

        public override bool Connected => connected;

        public override string Description => CleanedUpString(info.Description) + " internal filterwheel";

        public override async Task<bool> Connect(CancellationToken token) {
            return await Task.Run(() => {
                var success = false;
                try {
                    cameraDevice = AtikCameraDll.Connect(filterWheelDeviceId);
                    info = AtikCameraDll.GetCameraProperties(cameraDevice);

                    success = true;
                    connected = true;
                } catch (Exception e) {
                    connected = false;
                    Logger.Error(e);
                    Notification.ShowError(e.Message);
                }

                return success;
            });
        }

        public override void Disconnect() {
            // do nothing, if we disconnect the atik camera here it will disconnect in general
            connected = false;
        }

        private string CleanedUpString(char[] values) {
            return string.Join("", values.Take(Array.IndexOf(values, '\0')));
        }

        protected override int GetEfwPositions() => AtikCameraDll.GetInternalFilterWheelPositions(cameraDevice);
    }
}