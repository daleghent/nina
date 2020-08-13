using NINA.Utility;
using System;

namespace NINA.Model.MyGuider.MetaGuide {
    public class MetaGuideGuideParamsMsg : MetaGuideBaseMsg {
        private MetaGuideGuideParamsMsg() { }

        public static MetaGuideGuideParamsMsg Create(string[] args) {
            if (args.Length < 14) {
                return null;
            }
            try {
                return new MetaGuideGuideParamsMsg() {
                    RARate = double.Parse(args[5]),
                    DECRate = double.Parse(args[6]),
                    RAAggressiveness = double.Parse(args[7]),
                    DECAggressiveness = double.Parse(args[8]),
                    MinMove = double.Parse(args[9]),
                    MaxMove = double.Parse(args[10]),
                    DecRev = double.Parse(args[11]),
                    NorthSouthRev = int.Parse(args[12]),
                    EastWestRev = int.Parse(args[13])
                };
            } catch (Exception ex) {
                Logger.Error(ex);
                return null;
            }
        }
        public double RARate { get; private set; }
        public double DECRate { get; private set; }
        public double RAAggressiveness { get; private set; }
        public double DECAggressiveness { get; private set; }
        public double MinMove { get; private set; }
        public double MaxMove { get; private set; }
        public double DecRev { get; private set; }
        public int NorthSouthRev { get; private set; }
        public int EastWestRev { get; private set; }
    }
}
