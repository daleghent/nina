using NINA.Astrometry;

namespace NINA.Sequencer.Container {
    public class ContextCoordinates {
        public ContextCoordinates(Coordinates coordinates, double rotation, SiderealShiftTrackingRate shiftTrackingRate) {
            this.Coordinates = coordinates;
            this.Rotation = rotation;
            this.ShiftTrackingRate = shiftTrackingRate;
        }

        public Coordinates Coordinates { get; private set; }
        public double Rotation { get; private set; }
        public SiderealShiftTrackingRate ShiftTrackingRate { get; private set; }
    }
}
