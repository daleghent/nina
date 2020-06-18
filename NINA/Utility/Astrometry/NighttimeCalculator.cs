using NINA.Profile;
using OxyPlot;
using OxyPlot.Axes;
using System;

namespace NINA.Utility.Astrometry {

    internal class NighttimeCalculator : BaseINPC, INighttimeCalculator {
        private IProfileService profileService;

        public NighttimeCalculator(IProfileService profile) {
            profileService = profile;
            SelectedDate = DateTime.Now;
            Ticker = new Ticker(TimeSpan.FromSeconds(30));

            profileService.LocationChanged += (sender, e) => {
                _moonPhase = Astrometry.MoonPhase.Unknown;
                _nightDuration = null; //Clear cache
                Illumination = null;
                MoonRiseAndSet = null;
                SunRiseAndSet = null;
                TwilightRiseAndSet = null;
                _nightDuration = null;
                _twilightDuration = null;
                SelectedDate = DateTime.Now;
            };
        }

        public Ticker Ticker { get; }

        private Astrometry.MoonPhase _moonPhase;

        public Astrometry.MoonPhase MoonPhase {
            get {
                if (_moonPhase == Astrometry.MoonPhase.Unknown) {
                    var d = GetReferenceDate(SelectedDate);
                    _moonPhase = Astrometry.GetMoonPhase(d);
                }
                return _moonPhase;
            }
            private set {
                _moonPhase = value;
                RaisePropertyChanged();
            }
        }

        private AsyncObservableCollection<DataPoint> _nightDuration;

        public AsyncObservableCollection<DataPoint> NightDuration {
            get {
                if (_nightDuration == null) {
                    var twilight = TwilightRiseAndSet;
                    if (twilight != null && twilight.Rise.HasValue && twilight.Set.HasValue) {
                        var rise = twilight.Rise;
                        var set = twilight.Set;

                        _nightDuration = new AsyncObservableCollection<DataPoint>() {
                        new DataPoint(Axis.ToDouble(rise), 90),
                        new DataPoint(Axis.ToDouble(set), 90) };
                    } else {
                        _nightDuration = new AsyncObservableCollection<DataPoint>();
                    }
                }
                return _nightDuration;
            }
        }

        private AsyncObservableCollection<DataPoint> _twilightDuration;

        public AsyncObservableCollection<DataPoint> TwilightDuration {
            get {
                if (_twilightDuration == null) {
                    var twilight = SunRiseAndSet;
                    var night = TwilightRiseAndSet;
                    if (twilight != null && twilight.Rise.HasValue && twilight.Set.HasValue) {
                        var twilightRise = twilight.Rise;
                        var twilightSet = twilight.Set;
                        var rise = night.Rise;
                        var set = night.Set;

                        _twilightDuration = new AsyncObservableCollection<DataPoint>();
                        _twilightDuration.Add(new DataPoint(Axis.ToDouble(twilightSet), 90));

                        if (night != null && night.Rise.HasValue && night.Set.HasValue) {
                            _twilightDuration.Add(new DataPoint(Axis.ToDouble(set), 90));
                            _twilightDuration.Add(new DataPoint(Axis.ToDouble(set), 0));
                            _twilightDuration.Add(new DataPoint(Axis.ToDouble(rise), 0));
                            _twilightDuration.Add(new DataPoint(Axis.ToDouble(rise), 90));
                        }

                        _twilightDuration.Add(new DataPoint(Axis.ToDouble(twilightRise), 90));
                    } else {
                        _twilightDuration = new AsyncObservableCollection<DataPoint>();
                    }
                }
                return _twilightDuration;
            }
        }

        private RiseAndSetEvent _twilightRiseAndSet;

        public RiseAndSetEvent TwilightRiseAndSet {
            get {
                if (_twilightRiseAndSet == null) {
                    var d = GetReferenceDate(SelectedDate);
                    _twilightRiseAndSet = Astrometry.GetNightTimes(d, profileService.ActiveProfile.AstrometrySettings.Latitude, profileService.ActiveProfile.AstrometrySettings.Longitude);
                }
                return _twilightRiseAndSet;
            }
            private set {
                _twilightRiseAndSet = value;
                RaisePropertyChanged();
            }
        }

        private RiseAndSetEvent _moonRiseAndSet;

        public RiseAndSetEvent MoonRiseAndSet {
            get {
                if (_moonRiseAndSet == null) {
                    var d = GetReferenceDate(SelectedDate);
                    _moonRiseAndSet = Astrometry.GetMoonRiseAndSet(d, profileService.ActiveProfile.AstrometrySettings.Latitude, profileService.ActiveProfile.AstrometrySettings.Longitude);
                }
                return _moonRiseAndSet;
            }
            private set {
                _moonRiseAndSet = value;
                RaisePropertyChanged();
            }
        }

        private RiseAndSetEvent _sunRiseAndSet;

        public RiseAndSetEvent SunRiseAndSet {
            get {
                if (_sunRiseAndSet == null) {
                    var d = GetReferenceDate(SelectedDate);
                    _sunRiseAndSet = Astrometry.GetSunRiseAndSet(d, profileService.ActiveProfile.AstrometrySettings.Latitude, profileService.ActiveProfile.AstrometrySettings.Longitude);
                }
                return _sunRiseAndSet;
            }
            private set {
                _sunRiseAndSet = value;
                RaisePropertyChanged();
            }
        }

        private DateTime _selectedDate;

        public DateTime SelectedDate {
            get => _selectedDate; set {
                _selectedDate = value;
                RaiseAllPropertiesChanged();
            }
        }

        private double? _illumination;

        public double? Illumination {
            get {
                if (_illumination == null) {
                    var d = GetReferenceDate(SelectedDate);
                    _illumination = Astrometry.GetMoonIllumination(d);
                }
                return _illumination.Value;
            }
            private set {
                _illumination = value;
                RaisePropertyChanged();
            }
        }

        public static DateTime GetReferenceDate(DateTime reference) {
            DateTime d = reference;
            if (d.Hour > 12) {
                d = new DateTime(d.Year, d.Month, d.Day, 12, 0, 0);
            } else {
                var tmp = d.AddDays(-1);
                d = new DateTime(tmp.Year, tmp.Month, tmp.Day, 12, 0, 0);
            }
            return d;
        }
    }
}