using NINA.Utility;
using System;

namespace NINA.Model.MyGuider.MetaGuide {
    public class MetaGuideGuideMsg : MetaGuideBaseMsg {
        private MetaGuideGuideMsg() { }

        public static MetaGuideGuideMsg Create(string[] args) {
            if (args.Length < 10) {
                return null;
            }
            try {
                return new MetaGuideGuideMsg() {
                    SystemTimeInSeconds = double.Parse(args[5]),
                    SecondsSinceStart = double.Parse(args[6]),
                    WestPulse = int.Parse(args[7]),
                    NorthPulse = int.Parse(args[8]),
                    CalibrationState = (CalibrationState)int.Parse(args[9])
                };
            } catch (Exception ex) {
                Logger.Error(ex);
                return null;
            }
        }
        public double SystemTimeInSeconds { get; private set; }
        public double SecondsSinceStart { get; private set; }
        public int WestPulse { get; private set; }
        public int NorthPulse { get; private set; }
        public CalibrationState CalibrationState { get; private set; }
    }
}
