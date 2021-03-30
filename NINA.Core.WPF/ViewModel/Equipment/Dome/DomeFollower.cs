#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Model;
using NINA.Model.MyDome;
using NINA.Model.MyTelescope;
using NINA.Profile;
using NINA.Utility;
using NINA.Astrometry;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.ViewModel.Equipment.Dome {

    public class DomeFollower : BaseINPC, IDomeFollower, ITelescopeConsumer, IDomeConsumer {
        private static readonly double RA_DEC_WARN_THRESHOLD = 2.0;
        private readonly IProfileService profileService;
        private readonly ITelescopeMediator telescopeMediator;
        private readonly IDomeMediator domeMediator;
        private readonly IDomeSynchronization domeSynchronization;
        private Task domeFollowerTask;
        private CancellationTokenSource domeFollowerTaskCTS;
        private TelescopeInfo telescopeInfo = DeviceInfo.CreateDefaultInstance<TelescopeInfo>();
        private DomeInfo domeInfo = DeviceInfo.CreateDefaultInstance<DomeInfo>();
        private Task<bool> domeRotationTask;
        private CancellationTokenSource domeRotationCTS;

        public DomeFollower(
            IProfileService profileService,
            ITelescopeMediator telescopeMediator,
            IDomeMediator domeMediator,
            IDomeSynchronization domeSynchronization) {
            this.profileService = profileService;
            this.telescopeMediator = telescopeMediator;
            this.telescopeMediator.RegisterConsumer(this);
            this.domeMediator = domeMediator;
            this.domeMediator.RegisterConsumer(this);
            this.domeSynchronization = domeSynchronization;
        }

        public void Dispose() {
            this.telescopeMediator?.RemoveConsumer(this);
            this.domeMediator?.RemoveConsumer(this);
        }

        public async Task Start() {
            if (domeFollowerTask != null) {
                throw new InvalidOperationException("Dome follower is running already");
            }

            StartChecks();

            IsFollowing = true;
            domeFollowerTask = Task.Run(async () => {
                domeFollowerTaskCTS?.Dispose();
                domeFollowerTaskCTS = new CancellationTokenSource();
                try {
                    do {
                        if (!this.IsSynchronized) {
                            this.TriggerTelescopeSync();
                        }

                        await Task.Delay(TimeSpan.FromSeconds(2), domeFollowerTaskCTS.Token);
                    } while (true);
                } catch (OperationCanceledException) {
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(Locale.Loc.Instance["LblDomeFollowError"]);
                } finally {
                    IsFollowing = false;
                }
            });
            await domeFollowerTask;
            domeFollowerTask = null;
        }

        public async Task Stop() {
            domeFollowerTaskCTS?.Cancel();
            domeRotationCTS?.Cancel();
            while (domeFollowerTask?.IsCompleted == false) {
                await Task.Delay(TimeSpan.FromMilliseconds(500));
            }
            domeFollowerTask = null;
            domeRotationCTS = null;
        }

        private void StartChecks() {
            if (!telescopeInfo.Connected) {
                return;
            }

            if (Double.IsNaN(telescopeInfo.Altitude) || Double.IsNaN(telescopeInfo.Azimuth) ||
                Double.IsNaN(telescopeInfo.RightAscension) || Double.IsNaN(telescopeInfo.Declination)) {
                Logger.Warning("Scope does not report altitude, azimuth, RA, and Dec so we cannot validate the epoch");
                return;
            }

            var topocentricCoordinates = new TopocentricCoordinates(
                azimuth: Angle.ByDegree(telescopeInfo.Azimuth),
                altitude: Angle.ByDegree(telescopeInfo.Altitude),
                latitude: Angle.ByDegree(telescopeInfo.SiteLatitude),
                longitude: Angle.ByDegree(telescopeInfo.SiteLongitude));
            var eqCoordinates = topocentricCoordinates.Transform(telescopeInfo.EquatorialSystem);

            var error = Math.Sqrt(
                Math.Pow(Angle.ByHours(telescopeInfo.RightAscension).Degree - eqCoordinates.RADegrees, 2.0) +
                Math.Pow(telescopeInfo.Declination - eqCoordinates.Dec, 2.0));
            if (error > RA_DEC_WARN_THRESHOLD) {
                Logger.Warning($"Mount reported RA ({telescopeInfo.RightAscensionString}) and Dec ({telescopeInfo.DeclinationString}) differs substantially from the calculated RA ({eqCoordinates.RAString}) " +
                    $"and Dec ({eqCoordinates.DecString}). Confirm your mount epoch is configured properly and do a plate solve sync.");
                Notification.ShowWarning(Locale.Loc.Instance["LblDomeFollowPointingError"]);
            }
        }

        public async Task WaitForDomeSynchronization(CancellationToken cancellationToken) {
            var timeoutCTS = new CancellationTokenSource(TimeSpan.FromSeconds(profileService.ActiveProfile.DomeSettings.DomeSyncTimeoutSeconds));
            var timeoutOrClientCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(timeoutCTS.Token, cancellationToken).Token;
            while (IsFollowing && !IsSynchronized) {
                if (timeoutOrClientCancellationToken.IsCancellationRequested) {
                    Notification.ShowWarning(Locale.Loc.Instance["LblDomeSyncError_SyncTimeout"]);
                    Logger.Warning("Waiting for Dome synchronization cancelled or timed out");
                    return;
                }
                Logger.Trace("Dome not synchronized. Waiting...");
                await Task.Delay(TimeSpan.FromSeconds(1), timeoutOrClientCancellationToken);
            }
        }

        public void UpdateDeviceInfo(TelescopeInfo deviceInfo) {
            this.telescopeInfo = deviceInfo;
            if (!IsFollowing) {
                return;
            }

            if (!this.telescopeInfo.Connected) {
                IsFollowing = false;
                return;
            }

            try {
                var calculatedTargetAzimuth = GetSynchronizedPosition(this.telescopeInfo);
                var currentAzimuth = Angle.ByDegree(this.domeInfo.Azimuth);
                var tolerance = Angle.ByDegree(profileService.ActiveProfile.DomeSettings.AzimuthTolerance_degrees);
                this.IsSynchronized = calculatedTargetAzimuth.Equals(currentAzimuth, tolerance);
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(Locale.Loc.Instance["LblDomeFollowError"]);
                IsFollowing = false;
            }
        }

        public void UpdateDeviceInfo(DomeInfo deviceInfo) {
            this.domeInfo = deviceInfo;
            if (!IsFollowing) {
                return;
            }

            if (this.domeInfo.DriverCanFollow && this.domeInfo.DriverFollowing) {
                Notification.ShowError(Locale.Loc.Instance["LblDomeFollowError_DriverFollowing"]);
                IsFollowing = false;
                return;
            }

            if (!this.domeInfo.Connected) {
                IsFollowing = false;
                return;
            }
        }

        public void TriggerTelescopeSync() {
            if (!domeInfo.Connected || !telescopeInfo.Connected) {
                return;
            }

            var isLastSyncStillRotating = domeRotationTask?.IsCompleted == false;
            if (isLastSyncStillRotating) {
                return;
            }

            if (domeInfo.Slewing) {
                Logger.Trace("Cannot synchronize with telescope while dome is slewing");
                return;
            }

            // If TargetCoordinates is not null then NINA initiated the slew and we know the target coordinates
            if (telescopeInfo.Slewing && telescopeInfo.TargetCoordinates == null && !profileService.ActiveProfile.DomeSettings.SynchronizeDuringMountSlew) {
                Logger.Info("Will not synchronize telescope while it is slewing since SynchronizeDuringMountSlew is disabled");
                return;
            }

            var calculatedTargetAzimuth = GetSynchronizedPosition(telescopeInfo);
            var currentAzimuth = Angle.ByDegree(domeInfo.Azimuth);
            var tolerance = Angle.ByDegree(this.profileService.ActiveProfile.DomeSettings.AzimuthTolerance_degrees);
            if (!calculatedTargetAzimuth.Equals(currentAzimuth, tolerance)) {
                Logger.Trace($"Dome direct telescope follow slew. Current azimuth={currentAzimuth}, Target azimuth={calculatedTargetAzimuth}, Tolerance={tolerance}");
                domeRotationCTS = new CancellationTokenSource();
                domeRotationTask = this.domeMediator.SlewToAzimuth(calculatedTargetAzimuth.Degree, domeRotationCTS.Token);
            }
        }

        public Angle GetSynchronizedPosition(TelescopeInfo telescopeInfo) {
            var targetCoordinates = telescopeInfo.TargetCoordinates ?? telescopeInfo.Coordinates;
            PierSide targetSideOfPier;
            if (this.profileService.ActiveProfile.MeridianFlipSettings.UseSideOfPier) {
                targetSideOfPier = telescopeInfo.TargetSideOfPier ?? telescopeInfo.SideOfPier;
            } else {
                targetSideOfPier = MeridianFlip.ExpectedPierSide(targetCoordinates, Angle.ByHours(telescopeInfo.SiderealTime));
            }
            return domeSynchronization.TargetDomeAzimuth(
                scopeCoordinates: targetCoordinates,
                localSiderealTime: telescopeInfo.SiderealTime,
                siteLatitude: Angle.ByDegree(telescopeInfo.SiteLatitude),
                siteLongitude: Angle.ByDegree(telescopeInfo.SiteLongitude),
                sideOfPier: targetSideOfPier);
        }

        private bool isSynchronized = false;

        public bool IsSynchronized {
            get {
                return this.isSynchronized;
            }
            private set {
                if (this.isSynchronized != value) {
                    this.isSynchronized = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool following = false;

        public bool IsFollowing {
            get {
                return this.following;
            }
            private set {
                if (this.following != value) {
                    this.following = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}