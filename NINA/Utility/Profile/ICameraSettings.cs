﻿using NINA.Utility.Enum;

namespace NINA.Utility.Profile {
    public interface ICameraSettings {
        double BitDepth { get; set; }
        CameraBulbModeEnum BulbMode { get; set; }
        double DownloadToDataRatio { get; set; }
        double FullWellCapacity { get; set; }
        string Id { get; set; }
        double Offset { get; set; }
        double PixelSize { get; set; }
        RawConverterEnum RawConverter { get; set; }
        double ReadNoise { get; set; }
        string SerialPort { get; set; }
    }
}