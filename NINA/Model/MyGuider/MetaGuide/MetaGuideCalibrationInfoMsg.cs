using NINA.Utility;
using System;

namespace NINA.Model.MyGuider.MetaGuide {
    public class MetaGuideCalibrationInfoMsg {
        private MetaGuideCalibrationInfoMsg() { }

        public static MetaGuideCalibrationInfoMsg Create(string[] args) {
            if (args.Length < 11) {
                return null;
            }
            try {
                return new MetaGuideCalibrationInfoMsg() {
                    WestAngle = double.Parse(args[5]),
                    Parity = int.Parse(args[6]),
                    WestX = double.Parse(args[7]),
                    WestY = double.Parse(args[8]),
                    NorthX = double.Parse(args[9]),
                    NorthY = double.Parse(args[10])
                };
            } catch (Exception ex) {
                Logger.Error(ex);
                return null;
            }
        }

        public double WestAngle { get; private set; }
        public int Parity { get; private set; }
        public double WestX { get; private set; }
        public double WestY { get; private set; }
        public double NorthX { get; private set; }
        public double NorthY { get; private set; }
    }
}
