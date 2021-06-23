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
            _hasUnknownEpoch = false;
        }

        public AlignmentModes AlignmentMode {
            get {
                return GetProperty(nameof(Telescope.AlignmentMode), AlignmentModes.algGermanPolar);
            }
        }

        public bool CanSetSiteLatLong {
            get {
                if (propertySETMemory.TryGetValue(nameof(Telescope.SiteLatitude), out var memory)) {
                    return memory.IsImplemented;
                }
                return false;
            }
        }

        public double Altitude {
            get {
                return GetProperty(nameof(Telescope.Altitude), double.NaN);
            }
        }

        public string AltitudeString => double.IsNaN(Altitude) ? string.Empty : AstroUtil.DegreesToDMS(Altitude);

        public double Azimuth {
            get {
                return GetProperty(nameof(Telescope.Azimuth), double.NaN);
            }
        }

        public string AzimuthString => double.IsNaN(Azimuth) ? string.Empty : AstroUtil.DegreesToDMS(Azimuth);

        public double ApertureArea {
            get {
                return GetProperty(nameof(Telescope.ApertureArea), -1d);
            }
        }

        public double ApertureDiameter {
            get {
                return GetProperty(nameof(Telescope.ApertureDiameter), -1d);
            }
        }

        public bool AtHome {
            get {
                return GetProperty(nameof(Telescope.AtHome), false);
            }
        }

        public bool AtPark {
            get {
                return GetProperty(nameof(Telescope.AtPark), false);
            }
        }

        public bool CanFindHome {
            get {
                return GetProperty(nameof(Telescope.CanFindHome), false);
            }
        }

        public bool CanPark {
            get {
                return GetProperty(nameof(Telescope.CanPark), false);
            }
        }

        public bool CanPulseGuide {
            get {
                return GetProperty(nameof(Telescope.CanPulseGuide), false);
            }
        }

        public bool CanSetDeclinationRate {
            get {
                return GetProperty(nameof(Telescope.CanSetDeclinationRate), false);
            }
        }

        public bool CanSetGuideRates {
            get {
                return GetProperty(nameof(Telescope.CanSetGuideRates), false);
            }
        }

        public bool CanSetPark {
            get {
                return GetProperty(nameof(Telescope.CanSetPark), false);
            }
        }

        public bool CanSetPierSide {
            get {
                return GetProperty(nameof(Telescope.CanSetPierSide), false);
            }
        }

        public bool CanSetRightAscensionRate {
            get {
                return GetProperty(nameof(Telescope.CanSetRightAscensionRate), false);
            }
        }

        public bool CanSetTrackingRate {
            get {
                return GetProperty(nameof(Telescope.CanSetTracking), false);
            }
        }

        public bool CanSlew {
            get {
                return GetProperty(nameof(Telescope.CanSlew), false);
            }
        }

        public bool CanSlewAltAz {
            get {
                return GetProperty(nameof(Telescope.CanSlewAltAz), false);
            }
        }

        public bool CanSlewAltAzAsync {
            get {
                return GetProperty(nameof(Telescope.CanSlewAltAzAsync), false);
            }
        }

        public bool CanSlewAsync {
            get {
                return GetProperty(nameof(Telescope.CanSlewAsync), false);
            }
        }

        public bool CanSync {
            get {
                return GetProperty(nameof(Telescope.CanSync), false);
            }
        }

        public bool CanSyncAltAz {
            get {
                return GetProperty(nameof(Telescope.CanSyncAltAz), false);
            }
        }

        public bool CanUnpark {
            get {
                return GetProperty(nameof(Telescope.CanUnpark), false);
            }
        }

        public Coordinates Coordinates {
            get {
                return new Coordinates(RightAscension, Declination, EquatorialSystem, Coordinates.RAType.Hours);
            }
        }

        public double Declination {
            get {
                return GetProperty(nameof(Telescope.Declination), -1d);
            }
        }

        public string DeclinationString {
            get {
                return AstroUtil.DegreesToDMS(Declination);
            }
        }

        public double DeclinationRate {
            get {
                return GetProperty(nameof(Telescope.DeclinationRate), -1d);
            }
            set {
                if (CanSetDeclinationRate) {
                    SetProperty(nameof(Telescope.DeclinationRate), value);
                }
            }
        }

        public double RightAscensionRate {
            get {
                return GetProperty(nameof(Telescope.RightAscensionRate), -1d);
            }
            set {
                if (CanSetRightAscensionRate) {
                    SetProperty(nameof(Telescope.RightAscensionRate), value);
                }
            }
        }

        public PierSide SideOfPier {
            get {
                var pierside = GetProperty(nameof(Telescope.SideOfPier), ASCOM.DeviceInterface.PierSide.pierUnknown);
                return (PierSide)pierside;
            }
            set {
                if (CanSetPierSide) {
                    SetProperty(nameof(Telescope.SideOfPier), (ASCOM.DeviceInterface.PierSide)value);
                }
            }
        }

        public bool DoesRefraction {
            get {
                return GetProperty(nameof(Telescope.DoesRefraction), false);
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

        public double FocalLength {
            get {
                return GetProperty(nameof(Telescope.FocalLength), -1d);
            }
        }

        public short InterfaceVersion {
            get {
                return GetProperty<short>(nameof(Telescope.InterfaceVersion), -1);
            }
        }

        public double RightAscension {
            get {
                return GetProperty(nameof(Telescope.RightAscension), -1d);
            }
        }

        public string RightAscensionString {
            get {
                return AstroUtil.HoursToHMS(RightAscension);
            }
        }

        public double SiderealTime {
            get {
                return GetProperty(nameof(Telescope.SiderealTime), -1d);
            }
        }

        public string SiderealTimeString {
            get {
                return AstroUtil.HoursToHMS(SiderealTime);
            }
        }

        public bool Slewing {
            get {
                return GetProperty(nameof(Telescope.Slewing), false);
            }
        }

        public ArrayList SupportedActions {
            get {
                return GetProperty(nameof(Telescope.SupportedActions), new ArrayList());
            }
        }

        public bool IsPulseGuiding {
            get {
                if (CanPulseGuide) {
                    return GetProperty(nameof(Telescope.IsPulseGuiding), false);
                } else {
                    return false;
                }
            }
        }

        public double GuideRateDeclinationArcsecPerSec {
            get {
                if (CanSetGuideRates) {
                    var rate = GetProperty(nameof(Telescope.GuideRateDeclination), double.NaN);
                    if (!double.IsNaN(rate)) {
                        return rate * 3600.0;
                    }
                }
                return double.NaN;
            }
            set {
                if (CanSetGuideRates) {
                    SetProperty(nameof(Telescope.GuideRateDeclination), value);
                }
            }
        }

        public double GuideRateRightAscensionArcsecPerSec {
            get {
                if (CanSetGuideRates) {
                    var rate = GetProperty(nameof(Telescope.GuideRateRightAscension), double.NaN);
                    if (!double.IsNaN(rate)) {
                        return rate * 3600.0;
                    }
                }
                return double.NaN;
            }
            set {
                if (CanSetGuideRates) {
                    SetProperty(nameof(Telescope.GuideRateRightAscension), value);
                }
            }
        }

        public DateTime UTCDate {
            get {
                return GetProperty(nameof(Telescope.UTCDate), DateTime.MinValue);
            }
            set {
                SetProperty(nameof(Telescope.UTCDate), value);
            }
        }

        public double SiteElevation {
            get {
                return GetProperty(nameof(Telescope.SiteElevation), -1d);
            }
            set {
                SetProperty(nameof(Telescope.SiteElevation), value);
            }
        }

        public double SiteLatitude {
            get {
                return GetProperty(nameof(Telescope.SiteLatitude), -1d);
            }
            set {
                SetProperty(nameof(Telescope.SiteLatitude), value);
            }
        }

        public double SiteLongitude {
            get {
                return GetProperty(nameof(Telescope.SiteLongitude), -1d);
            }
            set {
                SetProperty(nameof(Telescope.SiteLongitude), value);
            }
        }

        public short SlewSettleTime {
            get {
                return GetProperty<short>(nameof(Telescope.SlewSettleTime), -1);
            }
            set {
                SetProperty(nameof(Telescope.SlewSettleTime), value);
            }
        }

        public double TargetDeclination {
            get {
                double val = double.NaN;
                if (!Slewing) {
                    val = GetProperty(nameof(Telescope.TargetDeclination), double.NaN);
                }
                return val;
            }
            set {
                SetProperty(nameof(Telescope.TargetDeclination), value);
            }
        }

        public double TargetRightAscension {
            get {
                double val = double.NaN;
                if (!Slewing) {
                    val = GetProperty(nameof(Telescope.TargetRightAscension), double.NaN);
                }
                return val;
            }
            set {
                SetProperty(nameof(Telescope.TargetRightAscension), value);
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

        private ASCOM.DeviceInterface.TelescopeAxes TransformAxis(TelescopeAxes axis) {
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
            return translatedAxis;
        }

        /// <summary>
        /// Retrieves axis rates for a given axis
        /// </summary>
        /// <param name="axis"></param>
        /// <returns>A collection of touples of (minimum, maximum) Rates</returns>
        public IList<(double, double)> GetAxisRates(TelescopeAxes axis) {
            var translatedAxis = TransformAxis(axis);
            List<(double, double)> axisRates = new List<(double, double)>();
            try {
                var rates = device.AxisRates(translatedAxis);
                foreach (IRate item in rates) {
                    axisRates.Add((item.Minimum, item.Maximum));
                }
            } catch (Exception) {
            }
            return axisRates;
        }

        public void MoveAxis(TelescopeAxes axis, double rate) {
            if (Connected) {
                if (CanSlew) {
                    if (!AtPark) {
                        try {
                            var translatedAxis = TransformAxis(axis);

                            if (axis == TelescopeAxes.Primary && !CanMovePrimaryAxis) {
                                Logger.Warning("Telescope cannot move primary axis");
                                Notification.ShowWarning(Loc.Instance["LblTelescopeCannotMovePrimaryAxis"]);
                            } else if (axis == TelescopeAxes.Secondary && !CanMoveSecondaryAxis) {
                                Logger.Warning("Telescope cannot move secondary axis");
                                Notification.ShowWarning(Loc.Instance["LblTelescopeCannotMoveSecondaryAxis"]);
                            } else {
                                var actualRate = rate;
                                if (actualRate != 0) {
                                    //Check that the given rate falls into the values of acceptable rates and adjust to the nearest rate if outside
                                    var sign = 1;
                                    if (actualRate < 0) { sign = -1; }
                                    actualRate = GetAdjustedMovingRate(Math.Abs(rate), Math.Abs(rate), translatedAxis) * sign;
                                }

                                device.MoveAxis(translatedAxis, actualRate);
                            }
                        } catch (Exception e) {
                            Logger.Error(e);
                            Notification.ShowError(e.Message);
                        }
                    } else {
                        Logger.Warning("Telescope parked");
                        Notification.ShowWarning(Loc.Instance["LblTelescopeParkedWarn"]);
                    }
                } else {
                    Logger.Warning("Telescope cannot slew");
                    Notification.ShowWarning(Loc.Instance["LblTelescopeCannotSlew"]);
                }
            } else {
                Logger.Warning("Telescope not connected");
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
                return GetProperty(nameof(Telescope.Tracking), false);
            }
            set {
                if (CanSetTrackingEnabled) {
                    if (SetProperty(nameof(Telescope.Tracking), value)) {
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
                } catch (OperationCanceledException e) {
                    throw e;
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

        public bool CanMovePrimaryAxis {
            get {
                if (Connected) {
                    return device.CanMoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary);
                }
                return false;
            }
        }

        public bool CanMoveSecondaryAxis {
            get {
                if (Connected) {
                    return device.CanMoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisSecondary);
                }
                return false;
            }
        }

        private double primaryMovingRate = double.NaN;

        public double PrimaryMovingRate {
            get {
                if (double.IsNaN(primaryMovingRate)) {
                    PrimaryMovingRate = primaryMovingRate;
                }

                return primaryMovingRate;
            }
            set {
                if (Connected) {
                    primaryMovingRate = GetAdjustedMovingRate(value, primaryMovingRate, ASCOM.DeviceInterface.TelescopeAxes.axisPrimary);
                    RaisePropertyChanged();
                }
            }
        }

        private double secondaryMovingRate = double.NaN;

        public double SecondaryMovingRate {
            get {
                if (double.IsNaN(secondaryMovingRate)) {
                    SecondaryMovingRate = secondaryMovingRate;
                }

                return secondaryMovingRate;
            }
            set {
                if (Connected) {
                    secondaryMovingRate = GetAdjustedMovingRate(value, secondaryMovingRate, ASCOM.DeviceInterface.TelescopeAxes.axisSecondary);
                    RaisePropertyChanged();
                }
            }
        }

        private double GetAdjustedMovingRate(double value, double oldValue, ASCOM.DeviceInterface.TelescopeAxes axis) {
            double result = value;
            if (result < 0) result = 0;
            bool incr = result > oldValue;

            double max = double.MinValue;
            double min = double.MaxValue;
            IAxisRates r = device.AxisRates(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary);
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
            if (result > max || double.IsNaN(oldValue)) result = max;
            if (result < min) result = min;

            return result;
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