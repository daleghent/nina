using NINA.Core.Utility;
using OxyPlot;
using OxyPlot.Axes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace NINA.Astrometry {
    public class MoonInfo : BaseINPC {

        public MoonInfo(Coordinates coordinates) {
            _coordinates = coordinates;
        }

        private Coordinates _coordinates;
        private ObserverInfo _observer;

        private DataPoint _maxMoonAltitude;

        public Coordinates Coordinates {
            get { return _coordinates; }
            set {
                _coordinates = value;
            }
        }

        public DataPoint MaxAltitude {
            get {
                return _maxMoonAltitude;
            }
            set {
                _maxMoonAltitude = value;
                RaisePropertyChanged();
            }
        }

        public List<DataPoint> DataPoints {
            get { return Points; }
        }

        private static List<DataPoint> Points = new List<DataPoint>();

        private static DateTime _referenceDate = DateTime.MinValue;

        public void SetReferenceDateAndObserver(DateTime date, ObserverInfo observer) {
            _observer = observer;

            var calculate = DisplayMoon && date != _referenceDate;
            _referenceDate = date;
            if (calculate) {
                CalculateMoonData();
            }
            CalculateSeparation(date);
        }

        private double _separation;

        public double Separation {
            get {
                return _separation;
            }
            set {
                _separation = value;
                SeparationText = Math.Round(Separation, 0).ToString().PadLeft(3, '0') + "°";
                RaisePropertyChanged();
            }
        }

        private string _separationText;

        public string SeparationText {
            get { return _separationText; }
            set {
                _separationText = value;
                RaisePropertyChanged();
            }
        }

        private bool _displayMoon;
        public bool DisplayMoon {
            get { return _displayMoon; }
            set {
                _displayMoon = value;
                if(value) {
                    if (Points.Count == 0) {
                        CalculateMoonData();
                    }
                }
                RaisePropertyChanged();
            }
        }

        public AstroUtil.MoonPhase Phase {
            get { return AstroUtil.GetMoonPhase(DateTime.Now); }
            set { }
        }

        public Color Color {
            get {
                double angle = Math.Abs(AstroUtil.GetMoonPositionAngle(DateTime.Now));
                byte gray = (byte)(angle * 255 / 180);
                byte alpha = (byte)(255 - gray);
                return Color.FromArgb(alpha, gray, gray, gray);
            }
            set { }
        }

        private void CalculateMoonData() {
            Points.Clear();
            DateTime start = _referenceDate;
            for (int i = 0; i < 24 * 10 /*24 hours x 10/hr*/; i++) {
                Points.Add(new DataPoint(DateTimeAxis.ToDouble(start), AstroUtil.GetMoonAltitude(start, _observer)));
                start = start.AddHours(0.1);
            }

            MaxAltitude = Points.OrderByDescending((x) => x.Y).FirstOrDefault();
            RaisePropertyChanged("DataPoints");
        }

        private void CalculateSeparation(DateTime time) {
            NOVAS.SkyPosition pos = AstroUtil.GetMoonPosition(time, AstroUtil.GetJulianDate(time), _observer);
            var moonRaRadians = AstroUtil.ToRadians(AstroUtil.HoursToDegrees(pos.RA));
            var moonDecRadians = AstroUtil.ToRadians(pos.Dec);

            Coordinates target = _coordinates.Transform(Epoch.JNOW);
            var targetRaRadians = AstroUtil.ToRadians(target.RADegrees);
            var targetDecRadians = AstroUtil.ToRadians(target.Dec);

            var theta = SOFA.Seps(moonRaRadians, moonDecRadians, targetRaRadians, targetDecRadians);
            Separation = AstroUtil.ToDegree(theta);
        }
    }
}
