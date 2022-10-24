using NINA.Core.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Interfaces {
    public interface IGenericCameraSDK {
        bool Connected { get; }

        void Connect();

        void Disconnect();

        int[] GetBinningInfo();

        (int, int) GetDimensions();

        int GetGain();

        int GetMaxGain();

        int GetMaxOffset();

        int GetMaxUSBLimit();

        int GetMinGain();

        int GetMinOffset();

        int GetMinUSBLimit();

        int GetOffset();

        double GetPixelSize();

        int GetUSBLimit();

        SensorType GetSensorInfo();

        bool SetGain(int value);

        bool SetOffset(int value);

        bool SetUSBLimit(int value);

        double GetMaxExposureTime();

        double GetMinExposureTime();

        void StartExposure(double exposureTime, int width, int height);
        void StopExposure();

        bool SetROI(int startX, int startY, int width, int height, int binning);

        int GetBitDepth();

        (int, int, int, int, int) GetROI();

        bool HasTemperatureControl();

        bool SetCooler(bool onOff);

        bool GetCoolerOnOff();

        bool SetTargetTemperature(double temperature);

        double GetTargetTemperature();

        double GetTemperature();

        double GetCoolerPower();
        [HandleProcessCorruptedStateExceptions]
        Task<ushort[]> GetExposure(double exposureTime, int width, int height, CancellationToken ct);
        bool IsExposureReady();

        bool HasDewHeater();
        bool SetDewHeater(bool onoff);
        bool IsDewHeaterOn();

        void StartVideoCapture(double exposureTime, int width, int height);
        void StopVideoCapture();
        [HandleProcessCorruptedStateExceptions]
        Task<ushort[]> GetVideoCapture(double exposureTime, int width, int height, CancellationToken ct);
        List<string> GetReadoutModes();
        int GetReadoutMode();
        void SetReadoutMode(int modeIndex);
    }
}
