namespace NINA.Astrometry {
    public class SiderealShiftTrackingRate {
        public static readonly SiderealShiftTrackingRate Disabled = CreateDisabled();

        private SiderealShiftTrackingRate(bool enabled, double raArcsecsPerHour, double decArcsecsPerHour) {
            this.Enabled = enabled;
            this.RAArcsecsPerHour = raArcsecsPerHour;
            this.DecArcsecsPerHour = decArcsecsPerHour;
        }

        public bool Enabled { get; private set; }
        public double RAArcsecsPerHour { get; private set; }
        public double DecArcsecsPerHour { get; private set; }

        public static SiderealShiftTrackingRate CreateDisabled() {
            return new SiderealShiftTrackingRate(false, 0.0d, 0.0d);
        }

        public static SiderealShiftTrackingRate Create(double raArcsecsPerHour, double decArcsecsPerHour) {
            return new SiderealShiftTrackingRate(true, raArcsecsPerHour, decArcsecsPerHour);
        }
    }
}
