#region "copyright"

/*
    Copyright ? 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using ASCOM;
using ASCOM.DriverAccess;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MyWeatherData {

    internal class AscomObservingConditions : AscomDevice<ObservingConditions>, IWeatherData, IDisposable {

        public AscomObservingConditions(string weatherDataId, string weatherDataName) : base(weatherDataId, weatherDataName) {
        }

        private bool _hasAveragePeriod;

        public double AveragePeriod {
            get {
                double wxstat = double.NaN;
                try {
                    if (Connected && _hasAveragePeriod) {
                        wxstat = device.AveragePeriod;
                    }
                } catch (PropertyNotImplementedException) {
                    _hasAveragePeriod = false;
                }
                return wxstat;
            }
            set {
                try {
                    if (Connected && _hasAveragePeriod) {
                        device.AveragePeriod = value;
                    }
                } catch (InvalidValueException ex) {
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
                }
            }
        }

        private bool _hasCloudCover;

        public double CloudCover {
            get {
                double wxstat = double.NaN;
                try {
                    if (Connected && _hasCloudCover) {
                        wxstat = device.CloudCover;
                    }
                } catch (PropertyNotImplementedException) {
                    _hasCloudCover = false;
                }
                return wxstat;
            }
        }

        private bool _hasDewPoint;

        public double DewPoint {
            get {
                double wxstat = double.NaN;
                try {
                    if (Connected && _hasDewPoint) {
                        wxstat = device.DewPoint;
                    }
                } catch (PropertyNotImplementedException) {
                    _hasDewPoint = false;
                }
                return wxstat;
            }
        }

        private bool _hasHumidity;

        public double Humidity {
            get {
                double wxstat = double.NaN;
                try {
                    if (Connected && _hasHumidity) {
                        wxstat = device.Humidity;
                    }
                } catch (PropertyNotImplementedException) {
                    _hasHumidity = false;
                }
                return wxstat;
            }
        }

        private bool _hasPressure;

        public double Pressure {
            get {
                double wxstat = double.NaN;
                try {
                    if (Connected && _hasPressure) {
                        wxstat = device.Pressure;
                    }
                } catch (PropertyNotImplementedException) {
                    _hasPressure = false;
                }
                return wxstat;
            }
        }

        private bool _hasRainRate;

        public double RainRate {
            get {
                double wxstat = double.NaN;
                try {
                    if (Connected && _hasRainRate) {
                        wxstat = device.RainRate;
                    }
                } catch (PropertyNotImplementedException) {
                    _hasRainRate = false;
                }
                return wxstat;
            }
        }

        private bool _hasSkyBrightness;

        public double SkyBrightness {
            get {
                double wxstat = double.NaN;
                try {
                    if (Connected && _hasSkyBrightness) {
                        wxstat = device.SkyBrightness;
                    }
                } catch (PropertyNotImplementedException) {
                    _hasSkyBrightness = false;
                }
                return wxstat;
            }
        }

        private bool _hasSkyQuality;

        public double SkyQuality {
            get {
                double wxstat = double.NaN;
                try {
                    if (Connected && _hasSkyQuality) {
                        wxstat = device.SkyQuality;
                    }
                } catch (PropertyNotImplementedException) {
                    _hasSkyQuality = false;
                }
                return wxstat;
            }
        }

        private bool _hasSkyTemperature;

        public double SkyTemperature {
            get {
                double wxstat = double.NaN;
                try {
                    if (Connected && _hasSkyTemperature) {
                        wxstat = device.SkyTemperature;
                    }
                } catch (PropertyNotImplementedException) {
                    _hasSkyTemperature = false;
                }
                return wxstat;
            }
        }

        private bool _hasStarFWHM;

        public double StarFWHM {
            get {
                double wxstat = double.NaN;
                try {
                    if (Connected && _hasStarFWHM) {
                        wxstat = device.StarFWHM;
                    }
                } catch (PropertyNotImplementedException) {
                    _hasStarFWHM = false;
                }
                return wxstat;
            }
        }

        private bool _hasTemperature;

        public double Temperature {
            get {
                double wxstat = double.NaN;
                try {
                    if (Connected && _hasTemperature) {
                        wxstat = device.Temperature;
                    }
                } catch (PropertyNotImplementedException) {
                    _hasTemperature = false;
                }
                return wxstat;
            }
        }

        private bool _hasWindDirection;

        public double WindDirection {
            get {
                double wxstat = double.NaN;
                try {
                    if (Connected && _hasWindDirection) {
                        wxstat = device.WindDirection;
                    }
                } catch (PropertyNotImplementedException) {
                    _hasWindDirection = false;
                }
                return wxstat;
            }
        }

        private bool _hasWindGust;

        public double WindGust {
            get {
                double wxstat = double.NaN;
                try {
                    if (Connected && _hasWindGust) {
                        wxstat = device.WindGust;
                    }
                } catch (PropertyNotImplementedException) {
                    _hasWindGust = false;
                }
                return wxstat;
            }
        }

        private bool _hasWindSpeed;

        public double WindSpeed {
            get {
                double wxstat = double.NaN;
                try {
                    if (Connected && _hasWindSpeed) {
                        wxstat = device.WindSpeed;
                    }
                } catch (PropertyNotImplementedException) {
                    _hasWindSpeed = false;
                }
                return wxstat;
            }
        }

        protected override string ConnectionLostMessage => Loc.Instance["LblWeatherConnectionLost"];

        private void Init() {
            _hasAveragePeriod = true;
            _hasCloudCover = true;
            _hasDewPoint = true;
            _hasHumidity = true;
            _hasPressure = true;
            _hasRainRate = true;
            _hasSkyBrightness = true;
            _hasSkyQuality = true;
            _hasSkyTemperature = true;
            _hasStarFWHM = true;
            _hasTemperature = true;
            _hasWindDirection = true;
            _hasWindGust = true;
            _hasWindSpeed = true;
        }

        protected override Task PostConnect() {
            Init();
            return Task.CompletedTask;
        }

        protected override ObservingConditions GetInstance(string id) {
            return new ObservingConditions(id); ;
        }
    }
}