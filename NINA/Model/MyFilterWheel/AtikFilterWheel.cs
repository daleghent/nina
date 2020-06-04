using NINA.Profile;
using NINA.Utility.AtikSDK;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyFilterWheel {
    internal class AtikFilterWheel : AtikFilterWheelBase {
        private readonly int filterWheelDeviceId;
        private IntPtr filterWheelDevice;

        public AtikFilterWheel(int deviceId, IProfileService profileService) : base(profileService) {
            filterWheelDeviceId = deviceId;
        }

        public override short Position {
            get {
                if (AtikCameraDll.GetCurrentEfwMoving(filterWheelDevice)) return -1;
                else return AtikCameraDll.GetCurrentEfwPosition(filterWheelDevice);
            }
            set => AtikCameraDll.SetCurrentEfwPosition(filterWheelDevice, value);
        }

        public override string Id => AtikCameraDll.GetArtemisEfwType(filterWheelDeviceId).ToString() + " (" + AtikCameraDll.GetArtemisEfwSerial(filterWheelDeviceId) + ")";

        public override string Name => AtikCameraDll.GetArtemisEfwType(filterWheelDeviceId).ToString() + " (" + AtikCameraDll.GetArtemisEfwSerial(filterWheelDeviceId) + ")";

        public override bool Connected => AtikCameraDll.IsConnectedEfw(filterWheelDevice);

        public override string Description => "Native Atik " + AtikCameraDll.GetConnectedArtemisEfwType(filterWheelDevice).ToString() + " (Serial Nr " + AtikCameraDll.GetArtemisEfwSerial(filterWheelDeviceId) + ")";

        public override Task<bool> Connect(CancellationToken token) {
            filterWheelDevice = AtikCameraDll.ConnectEfw(filterWheelDeviceId);
            return Task.FromResult(filterWheelDevice != IntPtr.Zero);
        }

        public override void Disconnect() {
            AtikCameraDll.DisconnectEfw(filterWheelDevice);
        }

        protected override int GetEfwPositions() => AtikCameraDll.GetEfwPositions(filterWheelDevice);
    }
}