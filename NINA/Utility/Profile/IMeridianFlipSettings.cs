﻿namespace NINA.Utility.Profile {
    public interface IMeridianFlipSettings {
        bool Enabled { get; set; }
        double MinutesAfterMeridian { get; set; }
        double PauseTimeBeforeMeridian { get; set; }
        bool Recenter { get; set; }
        int SettleTime { get; set; }
    }
}