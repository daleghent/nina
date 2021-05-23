#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using ASCOM;
using ASCOM.DeviceInterface;
using ASCOM.DriverAccess;
using NINA.Core.Utility;
using NINA.Astrometry;
using NINA.Core.Utility.Notification;
using NINA.Profile.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Immutable;
using PierSide = NINA.Core.Enum.PierSide;
using TelescopeAxes = NINA.Core.Enum.TelescopeAxes;
using GuideDirections = NINA.Core.Enum.GuideDirections;
using NINA.Core.Locale;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Utility;

namespace NINA.Equipment.Equipment.MyTelescope {

    internal class AscomTelescope : AscomDevice<Telescope>, ITelescope, IDisposable {
        private static readonly TimeSpan MERIDIAN_FLIP_SLEW_RETRY_WAIT = TimeSpan.FromMinutes(1);
        private static readonly int MERIDIAN_FLIP_SLEW_RETRY_ATTEMPTS = 20;
        private static double TRACKING_RATE_EPSILON = 0.000001;

        public AscomTelescope(string telescopeId, string name, IProfileService profileService) : base(telescopeId, name) {
            this.profileService = profileService;
        }

        private IProfileService profileService;

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

        private bool _canGetAlignmentMode;

        public AlignmentModes AlignmentMode {
            get {
                AlignmentModes val = AlignmentModes.algGermanPolar;
                try {
                    if (Connected && _canGetAlignmentMode) {
                        val = device.AlignmentMode;
                    }
                } catch (PropertyNotImplementedException) {
                    Logger.Debug("AlignmentMode GET is not implemented in this driver");
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
                        _altitude = device.Altitude;
                    }
                } catch (PropertyNotImplementedException) {
                    Logger.Debug("Altitude GET is not implemented in this driver");
                    _canGetAltitude = false;
                }
                return _altitude;
            }
        }

        public string AltitudeString => double.IsNaN(Altitude) ? string.Empty : AstroUtil.DegreesToDMS(Altitude);

        private bool _canGetAzimuth;

        private double _azimuth = double.NaN;

        public double Azimuth {
            get {
                try {
                    if (Connected && _canGetAzimuth) {
                        _azimuth = device.Azimuth;
                    }
                } catch (PropertyNotImplementedException) {
                    Logger.Debug("Azimuth GET is not implemented in this driver");
                    _canGetAzimuth = false;
                }
                return _azimuth;
            }
        }

        public string AzimuthString => double.IsNaN(Azimuth) ? string.Empty : AstroUtil.DegreesToDMS(Azimuth);

        private bool _canGetApertureArea;

        public double ApertureArea {
            get {
                double val = -1;
                try {
                    if (Connected && _canGetApertureArea) {
                        val = device.Altitude;
                    }
                } catch (PropertyNotImplementedException) {
                    Logger.Debug("ApertureArea GET is not implemented in this driver");
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
                        val = device.ApertureDiameter;
                    }
                } catch (PropertyNotImplementedException) {
                    Logger.Debug("ApertureDiameter GET is not implemented in this driver");
                    _canGetApertureDiameter = false;
                }
                return val;
            }
        }

        public bool AtHome {
            get {
                if (Connected) {
                    return device.AtHome;
                } else {
                    return false;
                }
            }
        }

        public bool AtPark {
            get {
                if (Connected) {
                    return device.AtPark;
                } else {
                    return false;
                }
            }
        }

        public bool CanFindHome {
            get {
                if (Connected) {
                    return device.CanFindHome;
                } else {
                    return false;
                }
            }
        }

        public bool CanPark {
            get {
                if (Connected) {
                    return device.CanPark;
                } else {
                    return false;
                }
            }
        }

        public bool CanPulseGuide {
            get {
                if (Connected) {
                    return device.CanPulseGuide;
                } else {
                    return false;
                }
            }
        }

        public bool CanSetDeclinationRate {
            get {
                if (Connected) {
                    return device.CanSetDeclinationRate;
                } else {
                    return false;
                }
            }
        }

        public bool CanSetGuideRates {
            get {
                if (Connected) {
                    return device.CanSetGuideRates;
                } else {
                    return false;
                }
            }
        }

        public bool CanSetPark {
            get {
                if (Connected) {
                    return device.CanSetPark;
                } else {
                    return false;
                }
            }
        }

        public bool CanSetPierSide {
            get {
                if (Connected) {
                    return device.CanSetPierSide;
                } else {
                    return false;
                }
            }
        }

        public bool CanSetRightAscensionRate {
            get {
                if (Connected) {
                    return device.CanSetRightAscensionRate;
                } else {
                    return false;
                }
            }
        }

        public bool CanSetTrackingRate {
            get {
                if (Connected) {
                    return device.CanSetTracking;
                } else {
                    return false;
                }
            }
        }

        public bool CanSlew {
            get {
                if (Connected) {
                    return device.CanSlew;
                } else {
                    return false;
                }
            }
        }

        public bool CanSlewAltAz {
            get {
                if (Connected) {
                    return device.CanSlewAltAz;
                } else {
                    return false;
                }
            }
        }

        public bool CanSlewAltAzAsync {
            get {
                if (Connected) {
                    return device.CanSlewAltAzAsync;
                } else {
                    return false;
                }
            }
        }

        public bool CanSlewAsync {
            get {
                if (Connected) {
                    try {
                        return device.CanSlewAsync;
                    } catch (Exception ex) {
                        Logger.Error(ex);
                        ASCOMInteraction.LogComplianceIssue();
                    }
                }
                return false;
            }
        }

        public bool CanSync {
            get {
                if (Connected) {
                    return device.CanSync;
                } else {
                    return false;
                }
            }
        }

        public bool CanSyncAltAz {
            get {
                if (Connected) {
                    return device.CanSyncAltAz;
                } else {
                    return false;
                }
            }
        }

        public bool CanUnpark {
            get {
                if (Connected) {
                    return device.CanUnpark;
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
                    return device.Declination;
                } else {
                    return -1;
                }
            }
        }

        public string DeclinationString {
            get {
                return AstroUtil.DegreesToDMS(Declination);
            }
        }

        public double DeclinationRate {
            get {
                if (Connected) {
                    return device.DeclinationRate;
                } else {
                    return -1;
                }
            }
            set {
                try {
                    if (CanSetDeclinationRate) {
                        device.DeclinationRate = value;
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
                    return device.RightAscensionRate;
                } else {
                    return -1;
                }
            }
            set {
                try {
                    if (CanSetRightAscensionRate) {
                        device.RightAscensionRate = value;
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
                        val = (PierSide)device.SideOfPier;
                    }
                } catch (PropertyNotImplementedException) {
                    Logger.Debug("SideOfPier GET is not implemented in this driver");
                    _canGetSideOfPier = false;
                }
                return val;
            }
            set {
                try {
                    if (CanSetPierSide) {
                        device.SideOfPier = (ASCOM.DeviceInterface.PierSide)value;
                        RaisePropertyChanged();
                    }
                } catch (PropertyNotImplementedException ex) {
                    Logger.Warning(ex.Message);
                } catch (InvalidValueException ex) {
                    Logger.Warning(ex.Message);
                }
            }
        }

        private bool _canDoRefraction;

        public bool DoesRefraction {
            get {
                bool val = false;
                try {
                    if (Connected && _canDoRefraction) {
                        val = device.DoesRefraction;
                    }
                } catch (PropertyNotImplementedException) {
                    Logger.Debug("DoesRefraction GET is not implemented in this driver");
                    _canDoRefraction = false;
                }
                return val;
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
                        val = device.FocalLength;
                    }
                } catch (PropertyNotImplementedException) {
                    Logger.Debug("FocalLength GET is not implemented in this driver");
                    _canGetFocalLength = false;
                }
                return val;
            }
        }

        public short InterfaceVersion {
            get {
                if (Connected) {
                    return device.InterfaceVersion;
                } else {
                    return -1;
                }
            }
        }

        public double RightAscension {
            get {
                if (Connected) {
                    return device.RightAscension;
                } else {
                    return -1;
                }
            }
        }

        public string RightAscensionString {
            get {
                return AstroUtil.HoursToHMS(RightAscension);
            }
        }

        public double SiderealTime {
            get {
                if (Connected) {
                    return device.SiderealTime;
                } else {
                    return -1;
                }
            }
        }

        public string SiderealTimeString {
            get {
                return AstroUtil.HoursToHMS(SiderealTime);
            }
        }

        private bool _canGetSlewing;

        public bool Slewing {
            get {
                bool val = false;
                try {
                    if (Connected && _canGetSlewing) {
                        val = device.Slewing;
                    }
                } catch (PropertyNotImplementedException) {
                    _canGetSlewing = false;
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(Loc.Instance["LblTelescope"] + Environment.NewLine + ex.Message);
                }
                return val;
            }
        }

        public ArrayList SupportedActions {
            get {
                if (Connected) {
                    return device.SupportedActions;
                } else {
                    return new ArrayList();
                }
            }
        }

        public bool IsPulseGuiding {
            get {
                if (CanPulseGuide) {
                    return device.IsPulseGuiding;
                } else {
                    return false;
                }
            }
        }

        public double GuideRateDeclinationArcsecPerSec {
            get {
                if (CanSetGuideRates) {
                    try {
                        return device.GuideRateDeclination * 3600.0;
                    } catch (PropertyNotImplementedException) {
                        Logger.Warning("GuideRateDeclination GET is not implemented in this driver, although CanSetGuideRates is returning true");
                    }
                }
                return double.NaN;
            }
            set {
                try {
                    if (CanSetGuideRates) {
                        device.GuideRateDeclination = value;
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
                if (CanSetGuideRates) {
                    try {
                        return device.GuideRateRightAscension * 3600.0;
                    } catch (PropertyNotImplementedException) {
                        Logger.Warning("GuideRateRightAscension GET is not implemented in this driver, although CanSetGuideRates is returning true");
                    }
                }
                return double.NaN;
            }
            set {
                try {
                    if (CanSetGuideRates) {
                        device.GuideRateRightAscension = value;
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
                    return device.UTCDate;
                } else {
                    return DateTime.MinValue;
                }
            }
            set {
                try {
                    if (Connected && _canSetUTCDate) {
                        device.UTCDate = value;
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
                        val = device.SiteElevation;
                    } catch (PropertyNotImplementedException) {
                        Logger.Debug("SiteElevation GET is not implemented in this driver");
                        _canGetSiteElevation = false;
                    }
                }
                return val;
            }
            set {
                try {
                    if (Connected && _canSetSiteElevation) {
                        device.SiteElevation = value;
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
                        val = device.SiteLatitude;
                    } catch (PropertyNotImplementedException) {
                        Logger.Debug("SiteLatitude GET is not implemented in this driver");
                        _canGetSiteLatLong = false;
                    }
                }
                return val;
            }
            set {
                try {
                    if (Connected && _canSetSiteLatLong) {
                        device.SiteLatitude = value;
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
                        val = device.SiteLongitude;
                    } catch (PropertyNotImplementedException) {
                        Logger.Debug("SiteLongitude GET is not implemented in this driver");
                        _canGetSiteLatLong = false;
                    }
                }
                return val;
            }
            set {
                try {
                    if (Connected && _canSetSiteLatLong) {
                        device.SiteLongitude = value;
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
                        val = device.SlewSettleTime;
                    } catch (PropertyNotImplementedException) {
                        Logger.Debug("SlewSettleTime GET is not implemented in this driver");
                        _canSetSlewSettleTime = false;
                    }
                }
                return val;
            }
            set {
                try {
                    if (Connected && _canSetSlewSettleTime) {
                        device.SlewSettleTime = value;
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
                        val = device.TargetDeclination;
                    } catch (PropertyNotImplementedException) {
                        Logger.Debug("TargetDeclination GET is not implemented in this driver");
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
                        device.TargetDeclination = value;
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
                        val = device.TargetRightAscension;
                    } catch (PropertyNotImplementedException) {
                        Logger.Debug("TargetRightAscension GET is not implemented in this driver");
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
                        device.TargetRightAscension = value;
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
                await CoreUtil.Wait(TimeSpan.FromSeconds(2));
                while (Slewing) {
                    await CoreUtil.Wait(TimeSpan.FromSeconds(profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval));
                }
                return true;
            } catch (Exception ex) {
                Logger.Error("Failed to flip side of pier", ex);
                return false;
            }
        }

        public async Task<bool> MeridianFlip(Coordinates targetCoordinates, CancellationToken token) {
            var success = false;
            try {
                if (!TrackingEnabled) {
                    TrackingEnabled = true;
                }

                var targetSideOfPier = NINA.Astrometry.MeridianFlip.ExpectedPierSide(
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
                    // Keep attempting slews as well, in case that's what it takes to flip to the other side of pier
                    slewSuccess = await SlewToCoordinates(targetCoordinates, token);
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
                    Notification.ShowWarning(String.Format(Loc.Instance["LblMeridianFlipWaitLonger"], retries));
                }
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(Loc.Instance["LblMeridianFlipFailed"]);
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
                            device.MoveAxis(translatedAxis, rate);
                        } catch (Exception e) {
                            Logger.Error(e);
                            Notification.ShowError(e.Message);
                        }
                    } else {
                        Notification.ShowWarning(Loc.Instance["LblTelescopeParkedWarn"]);
                    }
                } else {
                    Notification.ShowWarning(Loc.Instance["LblTelescopeCannotSlew"]);
                }
            } else {
                Notification.ShowWarning(Loc.Instance["LblTelescopeNotConnected"]);
            }
        }

        public void PulseGuide(GuideDirections direction, int duration) {
            if (Connected) {
                if (CanPulseGuide) {
                    if (!AtPark) {
                        try {
                            device.PulseGuide((ASCOM.DeviceInterface.GuideDirections)direction, duration);
                        } catch (Exception e) {
                            Logger.Error(e);
                            Notification.ShowError(e.Message);
                        }
                    } else {
                        Notification.ShowWarning(Loc.Instance["LblTelescopeParkedWarn"]);
                    }
                } else {
                    Notification.ShowWarning(Loc.Instance["LblTelescopeCannotPulseGuide"]);
                }
            } else {
                Notification.ShowWarning(Loc.Instance["LblTelescopeNotConnected"]);
            }
        }

        public void Park() {
            if (CanPark) {
                device.Park();
            }
        }

        public void Setpark() {
            if (CanSetPark) {
                try {
                    device.SetPark();
                } catch (Exception e) {
                    Logger.Error(e);
                    Notification.ShowError(e.Message);
                }
            }
        }

        public bool CanSetTrackingEnabled {
            get {
                return Connected && device.CanSetTracking;
            }
        }

        public bool TrackingEnabled {
            get {
                return Connected && device.Tracking;
            }
            set {
                if (CanSetTrackingEnabled) {
                    if (device.Tracking != value) {
                        device.Tracking = value;
                        RaisePropertyChanged();
                        RaisePropertyChanged(nameof(TrackingMode));
                        RaisePropertyChanged(nameof(TrackingRate));
                    }
                }
            }
        }

        public async Task<bool> SlewToCoordinates(Coordinates coordinates, CancellationToken token) {
            if (Connected && !AtPark) {
                try {
                    TrackingEnabled = true;
                    TargetCoordinates = coordinates.Transform(EquatorialSystem);
                    if (CanSlewAsync) {
                        device.SlewToCoordinatesAsync(TargetCoordinates.RA, TargetCoordinates.Dec);
                    } else {
                        device.SlewToCoordinates(TargetCoordinates.RA, TargetCoordinates.Dec);
                    }

                    while (Slewing) {
                        await CoreUtil.Wait(TimeSpan.FromSeconds(profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval), token);
                    }

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
            if (CanSlew) {
                device.AbortSlew();
            }
        }

        public bool Sync(Coordinates coordinates) {
            bool success = false;
            if (CanSync) {
                if (TrackingEnabled) {
                    try {
                        coordinates = coordinates.Transform(EquatorialSystem);
                        device.SyncToCoordinates(coordinates.RA, coordinates.Dec);
                        success = true;
                    } catch (Exception ex) {
                        Logger.Error(ex);
                        Notification.ShowError(ex.Message);
                    }
                } else {
                    Logger.Error("Telescope is not tracking to be able to sync");
                    Notification.ShowError(Loc.Instance["LblTelescopeNotTrackingForSync"]);
                }
            }
            return success;
        }

        public void FindHome() {
            if (CanFindHome) {
                try {
                    device.FindHome();
                } catch (Exception e) {
                    Logger.Error(e);
                    Notification.ShowError(e.Message);
                }
            }
        }

        public void Unpark() {
            if (CanUnpark) {
                try {
                    device.Unpark();
                } catch (Exception e) {
                    Logger.Error(e);
                    Notification.ShowError(e.Message);
                }
            }
        }

        public double HoursToMeridian {
            get {
                if (TrackingEnabled) {
                    return NINA.Astrometry.MeridianFlip.TimeToMeridian(
                    coordinates: Coordinates,
                    localSiderealTime: Angle.ByHours(SiderealTime)).TotalHours;
                }
                return 24;
            }
        }

        public string HoursToMeridianString => AstroUtil.HoursToHMS(HoursToMeridian);

        public double TimeToMeridianFlip {
            get {
                try {
                    if (TrackingEnabled) {
                        return NINA.Astrometry.MeridianFlip.TimeToMeridianFlip(
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
                return AstroUtil.HoursToHMS(TimeToMeridianFlip);
            }
        }

        private double _movingRate = double.NaN;

        public double MovingRate {
            get {
                if (double.IsNaN(_movingRate)) {
                    MovingRate = _movingRate;
                }

                return _movingRate;
            }
            set {
                if (Connected) {
                    double result = value;
                    if (result < 0) result = 0;
                    bool incr = result > _movingRate;

                    double max = double.MinValue;
                    double min = double.MaxValue;
                    IAxisRates r = device.AxisRates(ASCOM.DeviceInterface.TelescopeAxes.axisSecondary);
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
                    if (result > max || double.IsNaN(_movingRate)) result = max;
                    if (result < min) result = min;

                    _movingRate = result;
                    RaisePropertyChanged();
                }
            }
        }

        public void SendCommandString(string command) {
            if (Connected) {
                device.CommandString(command, true);
            } else {
                Notification.ShowError(Loc.Instance["LblTelescopeNotConnectedForCommand"] + ": " + command);
            }
        }

        private Epoch DetermineEquatorialSystem() {
            Epoch epoch = Epoch.JNOW;

            if (device.InterfaceVersion > 1) {
                EquatorialCoordinateType mountEqSystem = device.EquatorialSystem;

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
            var trackingRateEnum = device.TrackingRates.GetEnumerator();
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

            if (device.CanSetRightAscensionRate && device.CanSetDeclinationRate) {
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
                if (!Connected || !device.Tracking) {
                    return new TrackingRate() { TrackingMode = TrackingMode.Stopped };
                } else if (!device.CanSetTracking) {
                    return new TrackingRate() { TrackingMode = TrackingMode.Sidereal };
                }

                var ascomTrackingRate = device.TrackingRate;
                if (ascomTrackingRate == DriveRates.driveSidereal && (
                    Math.Abs(device.DeclinationRate) >= TRACKING_RATE_EPSILON) || (Math.Abs(device.RightAscensionRate) >= TRACKING_RATE_EPSILON)) {
                    return new TrackingRate() {
                        TrackingMode = TrackingMode.Custom,
                        CustomRightAscensionRate = device.RightAscensionRate,
                        CustomDeclinationRate = device.DeclinationRate
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

                if (Connected && device.CanSetTracking) {
                    // Set the mode regardless of whether it is the same as what is currently set
                    // Some ASCOM drivers incorrectly report custom rates as Sidereal, and this can help force set the tracking mode to the desired value
                    var currentTrackingMode = TrackingRate.TrackingMode;
                    switch (value) {
                        case TrackingMode.Sidereal:
                            device.TrackingRate = DriveRates.driveSidereal;
                            break;

                        case TrackingMode.Lunar:
                            device.TrackingRate = DriveRates.driveLunar;
                            break;

                        case TrackingMode.Solar:
                            device.TrackingRate = DriveRates.driveSolar;
                            break;

                        case TrackingMode.King:
                            device.TrackingRate = DriveRates.driveKing;
                            break;
                    }
                    device.Tracking = (value != TrackingMode.Stopped);
                    if (currentTrackingMode != value) {
                        RaisePropertyChanged();
                        RaisePropertyChanged(nameof(TrackingRate));
                        RaisePropertyChanged(nameof(TrackingEnabled));
                    }
                }
            }
        }

        protected override string ConnectionLostMessage => Loc.Instance["LblTelescopeConnectionLost"];

        public void SetCustomTrackingRate(double rightAscensionRate, double declinationRate) {
            if (!this.TrackingModes.Contains(TrackingMode.Custom) || !this.CanSetTrackingRate) {
                throw new NotSupportedException("Custom tracking rate not supported");
            }

            this.device.TrackingRate = DriveRates.driveSidereal;
            this.device.Tracking = true;
            this.device.RightAscensionRate = rightAscensionRate;
            this.device.DeclinationRate = declinationRate;
            RaisePropertyChanged(nameof(TrackingMode));
            RaisePropertyChanged(nameof(TrackingRate));
            RaisePropertyChanged(nameof(TrackingEnabled));
        }

        protected override Task PostConnect() {
            Initialize();
            EquatorialSystem = DetermineEquatorialSystem();
            SiteLongitude = SiteLongitude;
            SiteLatitude = SiteLatitude;
            trackingModes = GetTrackingModes();
            return Task.CompletedTask;
        }

        protected override Telescope GetInstance(string id) {
            return new Telescope(id);
        }
    }
}