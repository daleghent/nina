#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using ASCOM;
using ASCOM.DeviceInterface;
using ASCOM.DriverAccess;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Notification;
using NINA.Profile;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Immutable;

namespace NINA.Model.MyTelescope {

    internal class AscomTelescope : BaseINPC, ITelescope, IDisposable {
        private static readonly TimeSpan MERIDIAN_FLIP_SLEW_RETRY_WAIT = TimeSpan.FromMinutes(1);
        private static readonly int MERIDIAN_FLIP_SLEW_RETRY_ATTEMPTS = 20;
        private static double TRACKING_RATE_EPSILON = 0.000001;

        public AscomTelescope(string telescopeId, string name, IProfileService profileService) {
            this.profileService = profileService;
            Id = telescopeId;
            Name = name;
        }

        private IProfileService profileService;

        public string Category { get; } = "ASCOM";

        private void Initialize() {
            _canGetAlignmentMode = true;
            _canGetAltitude = true;
            _canGetApertureArea = true;
            _canGetApertureDiameter = true;
            _canGetAzimuth = true;
            _canDoRefraction = true;
            _canGetFocalLength = true;
            _canSetUTCDate = true;
            _canGetSlewing = true;
            _canGetSideOfPier = true;
            _canGetSiteElevation = true;
            _canSetSiteElevation = true;
            _canGetSiteLatLong = true;
            _canSetSiteLatLong = true;
            _canSetSlewSettleTime = true;
            _canGetTargetRaDec = true;
            _canSetTargetRaDec = true;
            _hasUnknownEpoch = false;
        }

        private string _id;

        public string Id {
            get {
                return _id;
            }
            set {
                _id = value;
                RaisePropertyChanged();
            }
        }

        private Telescope _telescope;

        private bool _canGetAlignmentMode;

        public AlignmentModes AlignmentMode {
            get {
                AlignmentModes val = AlignmentModes.algGermanPolar;
                try {
                    if (Connected && _canGetAlignmentMode) {
                        val = _telescope.AlignmentMode;
                    }
                } catch (PropertyNotImplementedException) {
                    _canGetAlignmentMode = false;
                }
                return val;
            }
        }

        public bool CanSetSiteLatLong {
            get {
                return _canSetSiteLatLong;
            }
        }

        private bool _canGetAltitude;

        private double _altitude = double.NaN;

        public double Altitude {
            get {
                try {
                    if (Connected && _canGetAltitude) {
                        _altitude = _telescope.Altitude;
                    }
                } catch (PropertyNotImplementedException) {
                    _canGetAltitude = false;
                }
                return _altitude;
            }
        }

        public string AltitudeString => double.IsNaN(Altitude) ? string.Empty : Astrometry.DegreesToDMS(Altitude);

        private bool _canGetAzimuth;

        private double _azimuth = double.NaN;

        public double Azimuth {
            get {
                try {
                    if (Connected && _canGetAzimuth) {
                        _azimuth = _telescope.Azimuth;
                    }
                } catch (PropertyNotImplementedException) {
                    _canGetAzimuth = false;
                }
                return _azimuth;
            }
        }

        public string AzimuthString => double.IsNaN(Azimuth) ? string.Empty : Astrometry.DegreesToDMS(Azimuth);

        private bool _canGetApertureArea;

        public double ApertureArea {
            get {
                double val = -1;
                try {
                    if (Connected && _canGetApertureArea) {
                        val = _telescope.Altitude;
                    }
                } catch (PropertyNotImplementedException) {
                    _canGetApertureArea = false;
                }
                return val;
            }
        }

        private bool _canGetApertureDiameter;

        public double ApertureDiameter {
            get {
                double val = -1;
                try {
                    if (Connected && _canGetApertureDiameter) {
                        val = _telescope.ApertureDiameter;
                    }
                } catch (PropertyNotImplementedException) {
                    _canGetApertureDiameter = false;
                }
                return val;
            }
        }

        public bool AtHome {
            get {
                if (Connected) {
                    return _telescope.AtHome;
                } else {
                    return false;
                }
            }
        }

        public bool AtPark {
            get {
                if (Connected) {
                    return _telescope.AtPark;
                } else {
                    return false;
                }
            }
        }

        public bool CanFindHome {
            get {
                if (Connected) {
                    return _telescope.CanFindHome;
                } else {
                    return false;
                }
            }
        }

        public bool CanPark {
            get {
                if (Connected) {
                    return _telescope.CanPark;
                } else {
                    return false;
                }
            }
        }

        public bool CanPulseGuide {
            get {
                if (Connected) {
                    return _telescope.CanPulseGuide;
                } else {
                    return false;
                }
            }
        }

        public bool CanSetDeclinationRate {
            get {
                if (Connected) {
                    return _telescope.CanSetDeclinationRate;
                } else {
                    return false;
                }
            }
        }

        public bool CanSetGuideRates {
            get {
                if (Connected) {
                    return _telescope.CanSetGuideRates;
                } else {
                    return false;
                }
            }
        }

        public bool CanSetPark {
            get {
                if (Connected) {
                    return _telescope.CanSetPark;
                } else {
                    return false;
                }
            }
        }

        public bool CanSetPierSide {
            get {
                if (Connected) {
                    return _telescope.CanSetPierSide;
                } else {
                    return false;
                }
            }
        }

        public bool CanSetRightAscensionRate {
            get {
                if (Connected) {
                    return _telescope.CanSetRightAscensionRate;
                } else {
                    return false;
                }
            }
        }

        public bool CanSetTrackingRate {
            get {
                if (Connected) {
                    return _telescope.CanSetTracking;
                } else {
                    return false;
                }
            }
        }

        public bool CanSlew {
            get {
                if (Connected) {
                    return _telescope.CanSlew;
                } else {
                    return false;
                }
            }
        }

        public bool CanSlewAltAz {
            get {
                if (Connected) {
                    return _telescope.CanSlewAltAz;
                } else {
                    return false;
                }
            }
        }

        public bool CanSlewAltAzAsync {
            get {
                if (Connected) {
                    return _telescope.CanSlewAltAzAsync;
                } else {
                    return false;
                }
            }
        }

        public bool CanSlewAsync {
            get {
                if (Connected) {
                    return _telescope.CanSlewAsync;
                } else {
                    return false;
                }
            }
        }

        public bool CanSync {
            get {
                if (Connected) {
                    return _telescope.CanSync;
                } else {
                    return false;
                }
            }
        }

        public bool CanSyncAltAz {
            get {
                if (Connected) {
                    return _telescope.CanSyncAltAz;
                } else {
                    return false;
                }
            }
        }

        public bool CanUnpark {
            get {
                if (Connected) {
                    return _telescope.CanUnpark;
                } else {
                    return false;
                }
            }
        }

        public Coordinates Coordinates {
            get {
                return new Coordinates(RightAscension, Declination, EquatorialSystem, Coordinates.RAType.Hours);
            }
        }

        public double Declination {
            get {
                if (Connected) {
                    return _telescope.Declination;
                } else {
                    return -1;
                }
            }
        }

        public string DeclinationString {
            get {
                return Astrometry.DegreesToDMS(Declination);
            }
        }

        public double DeclinationRate {
            get {
                if (Connected) {
                    return _telescope.DeclinationRate;
                } else {
                    return -1;
                }
            }
            set {
                try {
                    if (Connected && CanSetDeclinationRate) {
                        _telescope.DeclinationRate = value;
                        RaisePropertyChanged();
                    }
                } catch (PropertyNotImplementedException ex) {
                    Logger.Warning(ex.Message);
                } catch (InvalidValueException ex) {
                    Logger.Warning(ex.Message);
                }
            }
        }

        public double RightAscensionRate {
            get {
                if (Connected) {
                    return _telescope.RightAscensionRate;
                } else {
                    return -1;
                }
            }
            set {
                try {
                    if (Connected && CanSetRightAscensionRate) {
                        _telescope.RightAscensionRate = value;
                        RaisePropertyChanged();
                    }
                } catch (PropertyNotImplementedException ex) {
                    Logger.Warning(ex.Message);
                } catch (InvalidValueException ex) {
                    Logger.Warning(ex.Message);
                }
            }
        }

        private bool _canGetSideOfPier;

        public PierSide SideOfPier {
            get {
                PierSide val = PierSide.pierUnknown;
                try {
                    if (Connected && _canGetSideOfPier) {
                        val = (PierSide)_telescope.SideOfPier;
                    }
                } catch (PropertyNotImplementedException) {
                    _canGetSideOfPier = false;
                }
                return val;
            }
            set {
                try {
                    if (Connected && CanSetPierSide) {
                        _telescope.SideOfPier = (ASCOM.DeviceInterface.PierSide)value;
                        RaisePropertyChanged();
                    }
                } catch (PropertyNotImplementedException ex) {
                    Logger.Warning(ex.Message);
                } catch (InvalidValueException ex) {
                    Logger.Warning(ex.Message);
                }
            }
        }

        public string Description {
            get {
                if (Connected) {
                    return _telescope.Description;
                } else {
                    return string.Empty;
                }
            }
        }

        private bool _canDoRefraction;

        public bool DoesRefraction {
            get {
                bool val = false;
                try {
                    if (Connected && _canDoRefraction) {
                        val = _telescope.DoesRefraction;
                    }
                } catch (PropertyNotImplementedException) {
                    _canDoRefraction = false;
                }
                return val;
            }
        }

        public string DriverInfo {
            get {
                if (Connected) {
                    return _telescope.DriverInfo;
                } else {
                    return string.Empty;
                }
            }
        }

        public string DriverVersion {
            get {
                if (Connected) {
                    return _telescope.DriverVersion;
                } else {
                    return string.Empty;
                }
            }
        }

        private Epoch equatorialSystem = Epoch.J2000;

        public Epoch EquatorialSystem {
            get => equatorialSystem;
            private set {
                equatorialSystem = value;
                RaisePropertyChanged();
            }
        }

        private bool _canGetFocalLength;

        public double FocalLength {
            get {
                double val = -1;
                try {
                    if (Connected && _canGetFocalLength) {
                        val = _telescope.FocalLength;
                    }
                } catch (PropertyNotImplementedException) {
                    _canGetFocalLength = false;
                }
                return val;
            }
        }

        public short InterfaceVersion {
            get {
                if (Connected) {
                    return _telescope.InterfaceVersion;
                } else {
                    return -1;
                }
            }
        }

        private string _name;

        public string Name {
            get {
                return _name;
            }
            set {
                _name = value;
                RaisePropertyChanged();
            }
        }

        public double RightAscension {
            get {
                if (Connected) {
                    return _telescope.RightAscension;
                } else {
                    return -1;
                }
            }
        }

        public string RightAscensionString {
            get {
                return Astrometry.HoursToHMS(RightAscension);
            }
        }

        public double SiderealTime {
            get {
                if (Connected) {
                    return _telescope.SiderealTime;
                } else {
                    return -1;
                }
            }
        }

        public string SiderealTimeString {
            get {
                return Astrometry.HoursToHMS(SiderealTime);
            }
        }

        private bool _canGetSlewing;

        public bool Slewing {
            get {
                bool val = false;
                try {
                    if (Connected && _canGetSlewing) {
                        val = _telescope.Slewing;
                    }
                } catch (PropertyNotImplementedException) {
                    _canGetSlewing = false;
                }
                return val;
            }
        }

        public ArrayList SupportedActions {
            get {
                if (Connected) {
                    return _telescope.SupportedActions;
                } else {
                    return new ArrayList();
                }
            }
        }

        public bool IsPulseGuiding {
            get {
                if (Connected && CanPulseGuide) {
                    return _telescope.IsPulseGuiding;
                } else {
                    return false;
                }
            }
        }

        public double GuideRateDeclinationArcsecPerSec {
            get {
                if (Connected) {
                    return _telescope.GuideRateDeclination * 3600.0;
                } else {
                    return -1;
                }
            }
            set {
                try {
                    if (Connected && CanSetGuideRates) {
                        _telescope.GuideRateDeclination = value;
                        RaisePropertyChanged();
                    }
                } catch (PropertyNotImplementedException ex) {
                    Logger.Warning(ex.Message);
                } catch (InvalidValueException ex) {
                    Logger.Warning(ex.Message);
                }
            }
        }

        public double GuideRateRightAscensionArcsecPerSec {
            get {
                if (Connected) {
                    return _telescope.GuideRateRightAscension * 3600.0;
                } else {
                    return -1;
                }
            }
            set {
                try {
                    if (Connected && CanSetGuideRates) {
                        _telescope.GuideRateRightAscension = value;
                        RaisePropertyChanged();
                    }
                } catch (PropertyNotImplementedException ex) {
                    Logger.Warning(ex.Message);
                } catch (InvalidValueException ex) {
                    Logger.Warning(ex.Message);
                }
            }
        }

        private bool _canSetUTCDate;

        public DateTime UTCDate {
            get {
                if (Connected) {
                    return _telescope.UTCDate;
                } else {
                    return DateTime.MinValue;
                }
            }
            set {
                try {
                    if (Connected && _canSetUTCDate) {
                        _telescope.UTCDate = value;
                        RaisePropertyChanged();
                    }
                } catch (PropertyNotImplementedException ex) {
                    Logger.Warning(ex.Message);
                    _canSetUTCDate = false;
                }
            }
        }

        private bool _canGetSiteElevation;
        private bool _canSetSiteElevation;

        public double SiteElevation {
            get {
                double val = -1;
                if (Connected && _canGetSiteElevation) {
                    try {
                        val = _telescope.SiteElevation;
                    } catch (PropertyNotImplementedException) {
                        _canGetSiteElevation = false;
                    }
                }
                return val;
            }
            set {
                try {
                    if (Connected && _canSetSiteElevation) {
                        _telescope.SiteElevation = value;
                        RaisePropertyChanged();
                    }
                } catch (PropertyNotImplementedException ex) {
                    Logger.Warning(ex.Message);
                    _canSetSiteElevation = false;
                } catch (InvalidValueException ex) {
                    Logger.Warning(ex.Message);
                } catch (ASCOM.InvalidOperationException ex) {
                    Logger.Warning(ex.Message);
                }
            }
        }

        private bool _canGetSiteLatLong;
        private bool _canSetSiteLatLong;

        public double SiteLatitude {
            get {
                double val = -1;
                if (Connected && _canGetSiteLatLong) {
                    try {
                        val = _telescope.SiteLatitude;
                    } catch (PropertyNotImplementedException) {
                        _canGetSiteLatLong = false;
                    }
                }
                return val;
            }
            set {
                try {
                    if (Connected && _canSetSiteLatLong) {
                        _telescope.SiteLatitude = value;
                        RaisePropertyChanged();
                    }
                } catch (PropertyNotImplementedException ex) {
                    Logger.Warning(ex.Message);
                    _canSetSiteLatLong = false;
                } catch (InvalidValueException ex) {
                    Logger.Warning(ex.Message);
                } catch (ASCOM.InvalidOperationException ex) {
                    Logger.Warning(ex.Message);
                }
            }
        }

        public double SiteLongitude {
            get {
                double val = -1;
                if (Connected && _canGetSiteLatLong) {
                    try {
                        val = _telescope.SiteLongitude;
                    } catch (PropertyNotImplementedException) {
                        _canGetSiteLatLong = false;
                    }
                }
                return val;
            }
            set {
                try {
                    if (Connected && _canSetSiteLatLong) {
                        _telescope.SiteLongitude = value;
                        RaisePropertyChanged();
                    }
                } catch (PropertyNotImplementedException ex) {
                    Logger.Warning(ex.Message);
                    _canSetSiteLatLong = false;
                } catch (InvalidValueException ex) {
                    Logger.Warning(ex.Message);
                } catch (ASCOM.InvalidOperationException ex) {
                    Logger.Warning(ex.Message);
                }
            }
        }

        private bool _canSetSlewSettleTime;

        public short SlewSettleTime {
            get {
                short val = -1;
                if (Connected && _canSetSlewSettleTime) {
                    try {
                        val = _telescope.SlewSettleTime;
                    } catch (PropertyNotImplementedException) {
                        _canSetSlewSettleTime = false;
                    }
                }
                return val;
            }
            set {
                try {
                    if (Connected && _canSetSlewSettleTime) {
                        _telescope.SlewSettleTime = value;
                        RaisePropertyChanged();
                    }
                } catch (PropertyNotImplementedException ex) {
                    Logger.Warning(ex.Message);
                    _canSetSlewSettleTime = false;
                } catch (InvalidValueException ex) {
                    Logger.Warning(ex.Message);
                }
            }
        }

        private bool _canGetTargetRaDec;
        private bool _canSetTargetRaDec;

        public double TargetDeclination {
            get {
                double val = double.NaN;
                if (Connected && _canGetTargetRaDec && Slewing) {
                    try {
                        val = _telescope.TargetDeclination;
                    } catch (PropertyNotImplementedException) {
                        _canGetTargetRaDec = false;
                    } catch (DriverAccessCOMException ex) {
                        Logger.Warning(ex.Message);
                    } catch (ASCOM.InvalidOperationException ex) {
                        Logger.Warning(ex.Message);
                    }
                }
                return val;
            }
            set {
                try {
                    if (Connected && _canSetTargetRaDec) {
                        _telescope.TargetDeclination = value;
                        RaisePropertyChanged();
                    }
                } catch (PropertyNotImplementedException ex) {
                    Logger.Warning(ex.Message);
                    _canSetTargetRaDec = false;
                } catch (InvalidValueException ex) {
                    Logger.Warning(ex.Message);
                } catch (ASCOM.InvalidOperationException ex) {
                    Logger.Warning(ex.Message);
                }
            }
        }

        public double TargetRightAscension {
            get {
                double val = double.NaN;
                if (Connected && _canGetTargetRaDec && Slewing) {
                    try {
                        val = _telescope.TargetRightAscension;
                    } catch (PropertyNotImplementedException) {
                        _canGetTargetRaDec = false;
                    } catch (DriverAccessCOMException ex) {
                        Logger.Warning(ex.Message);
                    } catch (ASCOM.InvalidOperationException ex) {
                        Logger.Warning(ex.Message);
                    }
                }
                return val;
            }
            set {
                try {
                    if (Connected && _canSetTargetRaDec) {
                        _telescope.TargetRightAscension = value;
                        RaisePropertyChanged();
                    }
                } catch (PropertyNotImplementedException ex) {
                    Logger.Warning(ex.Message);
                    _canSetTargetRaDec = false;
                } catch (InvalidValueException ex) {
                    Logger.Warning(ex.Message);
                } catch (ASCOM.InvalidOperationException ex) {
                    Logger.Warning(ex.Message);
                }
            }
        }

        private Coordinates _targetCoordinates;

        public Coordinates TargetCoordinates {
            get {
                if (Connected) {
                    return _targetCoordinates;
                }
                return null;
            }
            private set {
                _targetCoordinates = value;
                RaisePropertyChanged();
            }
        }

        private PierSide? targetSideOfPier;

        public PierSide? TargetSideOfPier {
            get {
                if (Connected) {
                    return targetSideOfPier;
                } else {
                    return null;
                }
            }
            set {
                targetSideOfPier = value;
                RaisePropertyChanged();
            }
        }

        private bool _connected;

        public bool Connected {
            get {
                if (_connected) {
                    bool val = false;
                    try {
                        val = _telescope.Connected;
                        if (_connected != val) {
                            Notification.ShowWarning(Locale.Loc.Instance["LblTelescopeConnectionLost"]);
                            Disconnect();
                        }
                    } catch (Exception ex) {
                        Logger.Error(ex);
                        Notification.ShowWarning(Locale.Loc.Instance["LblTelescopeConnectionLost"]);
                        try {
                            Disconnect();
                        } catch (Exception disconnectEx) {
                            Logger.Error(disconnectEx);
                        }
                    }
                    return val;
                } else {
                    return false;
                }
            }
            private set {
                try {
                    _connected = value;
                    _telescope.Connected = value;
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(Locale.Loc.Instance["LblReconnectTelescope"] + Environment.NewLine + ex.Message);
                    _connected = false;
                }
                RaisePropertyChanged();
            }
        }

        public void Disconnect() {
            Connected = false;
            _telescope?.Dispose();
            _telescope = null;
        }

        private bool _hasUnknownEpoch;

        public bool HasUnknownEpoch {
            get => _hasUnknownEpoch;
            private set { _hasUnknownEpoch = value; RaisePropertyChanged(); }
        }

        private async Task<bool> SetPierSide(PierSide targetPierSide) {
            try {
                var pierside = SideOfPier;
                Logger.Debug($"Setting pier side from {pierside} to {targetPierSide}");

                TargetSideOfPier = targetPierSide;
                SideOfPier = targetPierSide;

                //Check if setting the pier side will result already in a flip
                await Utility.Utility.Wait(TimeSpan.FromSeconds(2));
                while (Slewing) {
                    await Utility.Utility.Wait(TimeSpan.FromSeconds(profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval));
                }
                return true;
            } catch (Exception ex) {
                Logger.Error("Failed to flip side of pier", ex);
                return false;
            }
        }

        public async Task<bool> MeridianFlip(Coordinates targetCoordinates) {
            var success = false;
            try {
                if (!TrackingEnabled) {
                    TrackingEnabled = true;
                }

                var targetSideOfPier = Utility.MeridianFlip.ExpectedPierSide(
                    coordinates: targetCoordinates,
                    localSiderealTime: Angle.ByHours(SiderealTime));
                if (profileService.ActiveProfile.MeridianFlipSettings.UseSideOfPier) {
                    Logger.Debug($"Mount side of pier is currently {SideOfPier}, and target is {targetSideOfPier}");
                    if (targetSideOfPier == SideOfPier) {
                        // No flip required
                        return true;
                    }
                }

                targetCoordinates = targetCoordinates.Transform(EquatorialSystem);
                TargetCoordinates = targetCoordinates;
                bool pierSideSuccess = !CanSetPierSide;  // If we can't set the side of pier, consider our work done up front already
                bool slewSuccess = false;
                int retries = 0;
                do {
                    if (!pierSideSuccess) {
                        pierSideSuccess = await SetPierSide(targetSideOfPier);
                    }
                    if (!slewSuccess) {
                        slewSuccess = SlewToCoordinates(targetCoordinates);
                    }
                    if (!pierSideSuccess) {
                        pierSideSuccess = SideOfPier == targetSideOfPier;
                    }
                    success = slewSuccess && pierSideSuccess;
                    if (!success) {
                        if (retries++ >= MERIDIAN_FLIP_SLEW_RETRY_ATTEMPTS) {
                            Logger.Error("Failed to slew for Meridian Flip, even after retrying");
                            break;
                        } else {
                            var jsnowCoordinates = targetCoordinates.Transform(Epoch.JNOW);
                            var topocentricCoordinates = jsnowCoordinates.Transform(latitude: Angle.ByDegree(SiteLatitude), longitude: Angle.ByDegree(SiteLongitude));
                            Logger.Error($"Failed to slew for Meridian Flip. Retry {retries} of {MERIDIAN_FLIP_SLEW_RETRY_ATTEMPTS} times with a {MERIDIAN_FLIP_SLEW_RETRY_WAIT} wait between each.  " +
                                $"SideOfPier: {SideOfPier}, RA: {jsnowCoordinates.RAString}, DEC: {jsnowCoordinates.DecString}, Azimuth: {topocentricCoordinates.Azimuth}");
                            await Task.Delay(MERIDIAN_FLIP_SLEW_RETRY_WAIT);
                        }
                    }
                } while (!success);

                if (success && retries > 0) {
                    Logger.Info("Successfully slewed for Meridian Flip after retrying");
                    Notification.ShowWarning(String.Format(Locale.Loc.Instance["LblMeridianFlipWaitLonger"], retries));
                }
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(Locale.Loc.Instance["LblMeridianFlipFailed"]);
            } finally {
                TargetCoordinates = null;
                TargetSideOfPier = null;
            }
            return success;
        }

        public void MoveAxis(TelescopeAxes axis, double rate) {
            if (Connected) {
                if (CanSlew) {
                    if (!AtPark) {
                        try {
                            ASCOM.DeviceInterface.TelescopeAxes translatedAxis;
                            switch (axis) {
                                case TelescopeAxes.Primary:
                                    translatedAxis = ASCOM.DeviceInterface.TelescopeAxes.axisPrimary;
                                    break;

                                case TelescopeAxes.Secondary:
                                    translatedAxis = ASCOM.DeviceInterface.TelescopeAxes.axisSecondary;
                                    break;

                                case TelescopeAxes.Tertiary:
                                    translatedAxis = ASCOM.DeviceInterface.TelescopeAxes.axisTertiary;
                                    break;

                                default:
                                    translatedAxis = ASCOM.DeviceInterface.TelescopeAxes.axisPrimary;
                                    break;
                            }
                            _telescope.MoveAxis(translatedAxis, rate);
                        } catch (Exception e) {
                            Logger.Error(e);
                            Notification.ShowError(e.Message);
                        }
                    } else {
                        Notification.ShowWarning(Locale.Loc.Instance["LblTelescopeParkedWarn"]);
                    }
                } else {
                    Notification.ShowWarning(Locale.Loc.Instance["LblTelescopeCannotSlew"]);
                }
            } else {
                Notification.ShowWarning(Locale.Loc.Instance["LblTelescopeNotConnected"]);
            }
        }

        public void PulseGuide(GuideDirections direction, int duration) {
            if (Connected) {
                if (CanPulseGuide) {
                    if (!AtPark) {
                        try {
                            _telescope.PulseGuide((ASCOM.DeviceInterface.GuideDirections)direction, duration);
                        } catch (Exception e) {
                            Logger.Error(e);
                            Notification.ShowError(e.Message);
                        }
                    } else {
                        Notification.ShowWarning(Locale.Loc.Instance["LblTelescopeParkedWarn"]);
                    }
                } else {
                    Notification.ShowWarning(Locale.Loc.Instance["LblTelescopeCannotPulseGuide"]);
                }
            } else {
                Notification.ShowWarning(Locale.Loc.Instance["LblTelescopeNotConnected"]);
            }
        }

        public void Park() {
            if (Connected && CanPark) {
                _telescope.Park();
            }
        }

        public void Setpark() {
            if (Connected && CanSetPark) {
                try {
                    _telescope.SetPark();
                } catch (Exception e) {
                    Logger.Error(e);
                    Notification.ShowError(e.Message);
                }
            }
        }

        public bool CanSetTrackingEnabled {
            get {
                return Connected && _telescope.CanSetTracking;
            }
        }

        public bool TrackingEnabled {
            get {
                return Connected && _telescope.Tracking;
            }
            set {
                if (Connected && CanSetTrackingEnabled) {
                    if (_telescope.Tracking != value) {
                        _telescope.Tracking = value;
                        RaisePropertyChanged();
                        RaisePropertyChanged(nameof(TrackingMode));
                        RaisePropertyChanged(nameof(TrackingRate));
                    }
                }
            }
        }

        public bool SlewToCoordinates(Coordinates coordinates) {
            if (Connected && CanSlew && !AtPark) {
                try {
                    TrackingEnabled = true;
                    TargetCoordinates = coordinates.Transform(EquatorialSystem);
                    _telescope.SlewToCoordinates(TargetCoordinates.RA, TargetCoordinates.Dec);
                    return true;
                } catch (Exception e) {
                    Logger.Error(e);
                    Notification.ShowError(e.Message);
                } finally {
                    TargetCoordinates = null;
                }
            }
            return false;
        }

        public void StopSlew() {
            if (Connected && CanSlew) {
                _telescope.AbortSlew();
            }
        }

        private static readonly Lazy<ASCOM.Utilities.Util> lazyAscomUtil =
            new Lazy<ASCOM.Utilities.Util>(() => new ASCOM.Utilities.Util());

        private static ASCOM.Utilities.Util AscomUtil { get { return lazyAscomUtil.Value; } }

        public bool Sync(Coordinates coordinates) {
            bool success = false;
            if (Connected && CanSync) {
                if (TrackingEnabled) {
                    try {
                        coordinates = coordinates.Transform(EquatorialSystem);
                        _telescope.SyncToCoordinates(coordinates.RA, coordinates.Dec);
                        success = true;
                    } catch (Exception ex) {
                        Logger.Error(ex);
                        Notification.ShowError(ex.Message);
                    }
                } else {
                    Notification.ShowError(Locale.Loc.Instance["LblTelescopeNotTrackingForSync"]);
                }
            }
            return success;
        }

        public void FindHome() {
            if (Connected && CanFindHome) {
                try {
                    _telescope.FindHome();
                } catch (Exception e) {
                    Logger.Error(e);
                    Notification.ShowError(e.Message);
                }
            }
        }

        public void Unpark() {
            if (Connected && CanUnpark) {
                try {
                    _telescope.Unpark();
                } catch (Exception e) {
                    Logger.Error(e);
                    Notification.ShowError(e.Message);
                }
            }
        }

        public void Dispose() {
            _telescope.Dispose();
        }

        public double HoursToMeridian {
            get {
                if (TrackingEnabled) {
                    return Utility.MeridianFlip.TimeToMeridian(
                    coordinates: Coordinates,
                    localSiderealTime: Angle.ByHours(SiderealTime)).TotalHours;
                }
                return 24;
            }
        }

        public string HoursToMeridianString => Astrometry.HoursToHMS(HoursToMeridian);

        public double TimeToMeridianFlip {
            get {
                try {
                    if (TrackingEnabled) {
                        return Utility.MeridianFlip.TimeToMeridianFlip(
                            settings: profileService.ActiveProfile.MeridianFlipSettings,
                            coordinates: Coordinates,
                            localSiderealTime: Angle.ByHours(SiderealTime),
                            currentSideOfPier: SideOfPier).TotalHours;
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
                }
                return 24;
            }
        }

        public string TimeToMeridianFlipString {
            get {
                return Astrometry.HoursToHMS(TimeToMeridianFlip);
            }
        }

        private double _movingRate;

        public double MovingRate {
            get {
                return _movingRate;
            }
            set {
                if (Connected) {
                    double result = value;
                    if (result < 0) result = 0;
                    bool incr = result > _movingRate;

                    double max = double.MinValue;
                    double min = double.MaxValue;
                    IAxisRates r = _telescope.AxisRates(ASCOM.DeviceInterface.TelescopeAxes.axisSecondary);
                    IEnumerator e = r.GetEnumerator();
                    foreach (IRate item in r) {
                        if (min > item.Minimum) {
                            min = item.Minimum;
                        }
                        if (max < item.Maximum) {
                            max = item.Maximum;
                        }

                        if (item.Minimum <= value && value <= item.Maximum) {
                            result = value;
                            break;
                        } else if (incr && value < item.Minimum) {
                            result = item.Minimum;
                        } else if (!incr && value > item.Maximum) {
                            result = item.Maximum;
                        }
                    }
                    if (result > max) result = max;
                    if (result < min) result = min;

                    _movingRate = result;
                    RaisePropertyChanged();
                }
            }
        }

        public bool HasSetupDialog {
            get {
                return true;
            }
        }

        public void SetupDialog() {
            if (HasSetupDialog) {
                try {
                    bool dispose = false;
                    if (_telescope == null) {
                        _telescope = new Telescope(Id);
                        dispose = true;
                    }
                    _telescope.SetupDialog();
                    if (dispose) {
                        _telescope.Dispose();
                        _telescope = null;
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
                }
            }
        }

        public void SendCommandString(string command) {
            if (Connected) {
                _telescope.CommandString(command, true);
            } else {
                Notification.ShowError(Locale.Loc.Instance["LblTelescopeNotConnectedForCommand"] + ": " + command);
            }
        }

        public async Task<bool> Connect(CancellationToken token) {
            return await Task<bool>.Run(() => {
                try {
                    _telescope = new Telescope(Id);
                    Connected = true;
                    if (Connected) {
                        Initialize();
                        EquatorialSystem = DetermineEquatorialSystem();
                        SiteLongitude = SiteLongitude;
                        SiteLatitude = SiteLatitude;
                        trackingModes = GetTrackingModes();
                        RaiseAllPropertiesChanged();
                    }
                } catch (ASCOM.DriverAccessCOMException ex) {
                    Utility.Utility.HandleAscomCOMException(ex);
                } catch (System.Runtime.InteropServices.COMException ex) {
                    Utility.Utility.HandleAscomCOMException(ex);
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError("Unable to connect to telescope " + ex.Message);
                }
                return Connected;
            });
        }

        private Epoch DetermineEquatorialSystem() {
            Epoch epoch = Epoch.JNOW;

            if (_telescope.InterfaceVersion > 1) {
                EquatorialCoordinateType mountEqSystem = _telescope.EquatorialSystem;

                switch (mountEqSystem) {
                    case EquatorialCoordinateType.equB1950:
                        epoch = Epoch.B1950;
                        break;

                    case EquatorialCoordinateType.equJ2000:
                        epoch = Epoch.J2000;
                        break;

                    case EquatorialCoordinateType.equJ2050:
                        epoch = Epoch.J2050;
                        break;

                    case EquatorialCoordinateType.equOther:
                        epoch = Epoch.J2000;
                        HasUnknownEpoch = true;
                        break;
                }
            }

            return epoch;
        }

        private ImmutableList<TrackingMode> GetTrackingModes() {
            var trackingRateEnum = _telescope.TrackingRates.GetEnumerator();
            var trackingModes = ImmutableList.CreateBuilder<TrackingMode>();
            trackingModes.Add(TrackingMode.Sidereal);
            while (!trackingRateEnum.MoveNext()) {
                var trackingRate = (DriveRates)trackingRateEnum.Current;
                switch (trackingRate) {
                    case DriveRates.driveKing:
                        trackingModes.Add(TrackingMode.King);
                        break;

                    case DriveRates.driveLunar:
                        trackingModes.Add(TrackingMode.Lunar);
                        break;

                    case DriveRates.driveSolar:
                        trackingModes.Add(TrackingMode.Solar);
                        break;
                }
            }

            if (_telescope.CanSetRightAscensionRate && _telescope.CanSetDeclinationRate) {
                trackingModes.Add(TrackingMode.Custom);
            }
            trackingModes.Add(TrackingMode.Stopped);
            return trackingModes.ToImmutable();
        }

        private ImmutableList<TrackingMode> trackingModes = ImmutableList.Create<TrackingMode>();

        public IList<TrackingMode> TrackingModes {
            get {
                return trackingModes;
            }
        }

        public TrackingRate TrackingRate {
            get {
                if (!Connected || !_telescope.Tracking) {
                    return new TrackingRate() { TrackingMode = TrackingMode.Stopped };
                } else if (!_telescope.CanSetTracking) {
                    return new TrackingRate() { TrackingMode = TrackingMode.Sidereal };
                }

                var ascomTrackingRate = _telescope.TrackingRate;
                if (ascomTrackingRate == DriveRates.driveSidereal && (
                    Math.Abs(_telescope.DeclinationRate) >= TRACKING_RATE_EPSILON) || (Math.Abs(_telescope.RightAscensionRate) >= TRACKING_RATE_EPSILON)) {
                    return new TrackingRate() {
                        TrackingMode = TrackingMode.Custom,
                        CustomRightAscensionRate = _telescope.RightAscensionRate,
                        CustomDeclinationRate = _telescope.DeclinationRate
                    };
                }
                var trackingMode = TrackingMode.Sidereal;
                switch (ascomTrackingRate) {
                    case DriveRates.driveKing:
                        trackingMode = TrackingMode.King;
                        break;

                    case DriveRates.driveLunar:
                        trackingMode = TrackingMode.Lunar;
                        break;

                    case DriveRates.driveSolar:
                        trackingMode = TrackingMode.Solar;
                        break;
                }
                return new TrackingRate() { TrackingMode = trackingMode };
            }
        }

        public TrackingMode TrackingMode {
            get {
                return TrackingRate.TrackingMode;
            }
            set {
                if (value == TrackingMode.Custom) {
                    throw new ArgumentException("TrackingMode cannot be set to Custom. Use SetCustomTrackingRate");
                }

                if (Connected && _telescope.CanSetTracking) {
                    // Set the mode regardless of whether it is the same as what is currently set
                    // Some ASCOM drivers incorrectly report custom rates as Sidereal, and this can help force set the tracking mode to the desired value
                    var currentTrackingMode = TrackingRate.TrackingMode;
                    switch (value) {
                        case TrackingMode.Sidereal:
                            _telescope.TrackingRate = DriveRates.driveSidereal;
                            break;

                        case TrackingMode.Lunar:
                            _telescope.TrackingRate = DriveRates.driveLunar;
                            break;

                        case TrackingMode.Solar:
                            _telescope.TrackingRate = DriveRates.driveSolar;
                            break;

                        case TrackingMode.King:
                            _telescope.TrackingRate = DriveRates.driveKing;
                            break;
                    }
                    _telescope.Tracking = (value != TrackingMode.Stopped);
                    if (currentTrackingMode != value) {
                        RaisePropertyChanged();
                        RaisePropertyChanged(nameof(TrackingRate));
                        RaisePropertyChanged(nameof(TrackingEnabled));
                    }
                }
            }
        }

        public void SetCustomTrackingRate(double rightAscensionRate, double declinationRate) {
            if (!this.TrackingModes.Contains(TrackingMode.Custom) || !this.CanSetTrackingRate) {
                throw new NotSupportedException("Custom tracking rate not supported");
            }

            this._telescope.TrackingRate = DriveRates.driveSidereal;
            this._telescope.Tracking = true;
            this._telescope.RightAscensionRate = rightAscensionRate;
            this._telescope.DeclinationRate = declinationRate;
            RaisePropertyChanged(nameof(TrackingMode));
            RaisePropertyChanged(nameof(TrackingRate));
            RaisePropertyChanged(nameof(TrackingEnabled));
        }
    }
}