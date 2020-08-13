using System;

namespace NINA.Model.MyGuider.MetaGuide {
    [Flags]
    public enum CalibrationState {
        Unknown = 0,
        WestPierSide = 1,
        Rotate180OnFlip = 2,
        NorthSouthReverseOnFlip = 4,
        EastWestReverseOnFlip = 8,
        NorthSouthReversed = 16,
        EastWestReversed = 32,
        CalibratedFresh = 64,
        Calibrated = 128,
        Calibrating = 256
    }
}
