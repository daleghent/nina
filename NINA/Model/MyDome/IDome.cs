using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyDome {
    public enum ShutterState {
        ShutterNone = -1,
        ShutterOpen = 0,
        ShutterClosed = 1,
        ShutterOpening = 2,
        ShutterClosing = 3,
        ShutterError = 4
    }

    internal interface IDome : IDevice {
        ShutterState ShutterStatus { get; }
        bool DriverCanSlave { get; }
        bool CanSetShutter { get; }
        bool CanSetPark { get; }
        bool CanSetAzimuth { get; }
        bool CanSyncAzimuth { get; }
        bool CanPark { get; }
        bool CanFindHome { get; }
        double Azimuth { get; }
        bool AtPark { get; }
        bool AtHome { get; }
        bool DriverSlaved { get; }
        bool Slewing { get; }

        Task SlewToAzimuth(double azimuth, CancellationToken ct);
        Task StartRotateCW(CancellationToken ct);
        Task StartRotateCCW(CancellationToken ct);
        void StopSlewing();
        void StopShutter();
        Task OpenShutter(CancellationToken ct);
        Task CloseShutter(CancellationToken ct);
        Task FindHome(CancellationToken ct);
        Task Park(CancellationToken ct);
        void SetPark();
        void SyncToAzimuth(double azimuth);
    }
}
