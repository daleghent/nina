using ASCOM.DeviceInterface;
using ASCOM.DriverAccess;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyFocuser {

    public interface IFocuserV3Ex : IFocuserV3 {

        Task MoveAsync(int position, CancellationToken ct, int waitInMs = 1000);

        IFocuserV3 GetASCOMInstance();
    }
}