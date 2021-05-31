#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Equipment.MyTelescope;
using NINA.Core.Utility;
using NINA.Astrometry;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Core.Utility.Notification;
using NINA.Profile.Interfaces;
using NINA.Core.Utility.WindowService;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using System.Collections.Immutable;
using NINA.Core.Enum;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Core.Model;
using NINA.Core.Locale;
using NINA.Core.MyMessageBox;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Equipment.Equipment;

namespace NINA.WPF.Base.ViewModel.Equipment.Telescope {

    public class TelescopeVM : DockableVM, ITelescopeVM {
        private static double LAT_LONG_TOLERANCE = 0.001;

        public TelescopeVM(
            IProfileService profileService,
            ITelescopeMediator telescopeMediator,
            IApplicationStatusMediator applicationStatusMediator,
            IDomeMediator domeMediator) : base(profileService) {
            this.profileService = profileService;
            this.telescopeMediator = telescopeMediator;
            this.telescopeMediator.RegisterHandler(this);
            this.applicationStatusMediator = applicationStatusMediator;
            this.domeMediator = domeMediator;
            Title = "LblTelescope";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["TelescopeSVG"];

            progress = new Progress<ApplicationStatus>(p => {
                p.Source = this.Title;
                this.applicationStatusMediator.StatusUpdate(p);
            });
            Task.Run(() => TelescopeChooserVM.GetEquipment());

            ChooseTelescopeCommand = new AsyncCommand<bool>(() => ChooseTelescope());
            CancelChooseTelescopeCommand = new RelayCommand(CancelChooseTelescope);
            DisconnectCommand = new AsyncCommand<bool>(() => DisconnectTelescope());
            ParkCommand = new AsyncCommand<bool>(() => {
                InitCancelSlewTelescope();
                return ParkTelescope(progress, _cancelSlewTelescopeSource.Token);
            });

            UnparkCommand = new RelayCommand(UnparkTelescope);
            SetParkPositionCommand = new AsyncCommand<bool>(SetParkPosition);
            SlewToCoordinatesCommand = new AsyncCommand<bool>(SlewToCoordinatesInternal);
            RefreshTelescopeListCommand = new RelayCommand(RefreshTelescopeList, o => !(Telescope?.Connected == true));
            FindHomeCommand = new AsyncCommand<bool>(() => {
                InitCancelSlewTelescope();
                return FindHome(progress, _cancelSlewTelescopeSource.Token);
            });

            MoveCommand = new RelayCommand(Move);
            StopMoveCommand = new RelayCommand(StopMove);
            StopSlewCommand = new RelayCommand(StopSlew);
            SetTrackingEnabledCommand = new RelayCommand(HandleSetTrackingEnabledCommand);
            SetTrackingModeCommand = new RelayCommand(HandleSetTrackingModeCommand);

            updateTimer = new DeviceUpdateTimer(
                GetTelescopeValues,
                UpdateTelescopeValues,
                profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval
            );

            profileService.ProfileChanged += (object sender, EventArgs e) => {
                RefreshTelescopeList(null);
            };
        }

        public bool SendToSnapPort(bool start) {
            if (TelescopeInfo.Connected) {
                string command = string.Empty;
                if (start) {
                    command = profileService.ActiveProfile.TelescopeSettings.SnapPortStart;
                } else {
                    command = profileService.ActiveProfile.TelescopeSettings.SnapPortStop;
                }
                _telescope?.SendCommandString(command);
                return true;
            } else {
                Notification.ShowError(Loc.Instance["LblTelescopeNotConnectedForCommand"]);
                return false;
            }
        }

        private void RefreshTelescopeList(object obj) {
            TelescopeChooserVM.GetEquipment();
        }

        public async Task<bool> ParkTelescope(IProgress<ApplicationStatus> progress, CancellationToken token) {
            bool result = true;

            await Task.Run(async () => {
                Logger.Info("Telescope has been commanded to park");
                IsParkingOrHoming = true;

                try {
                    if (Telescope.CanPark) {
                        if (!Telescope.AtPark) {
                            progress?.Report(new ApplicationStatus { Status = Loc.Instance["LblWaitingForTelescopeToPark"] });
                            Telescope.Park();

                            // Detect if the parking process was cancelled by an external force, such as the user hitting the stop button
                            // in another app or in the driver's own UI, external to NINA.
                            if (!Telescope.AtPark && !Telescope.Slewing) {
                                Logger.Warning("Park operation appears to have been cancelled by an external process");
                                throw new OperationCanceledException();
                            } else {
                                // Defend against drivers that might surprise us with a non-conformant async Park()
                                // Also catch cases where the user cancelled the procedure by hitting the Stop button
                                while (!Telescope.AtPark && Telescope.Slewing) {
                                    if (token.IsCancellationRequested) {
                                        Logger.Warning("Park operation cancelled");
                                        throw new OperationCanceledException();
                                    }

                                    await CoreUtil.Delay(TimeSpan.FromSeconds(2), token);
                                }
                            }
                        } else {
                            Logger.Info("Telescope commanded to park but it is already parked");
                        }
                    } else { // Telescope is incapable of parking. Slew safely to the celestial pole and stop tracking instead
                        Coordinates targetCoords = GetHomeCoordinates(telescopeInfo.Coordinates);
                        Logger.Trace($"Telescope cannot park. Will slew to RA {targetCoords.RAString}, Dec {targetCoords.DecString}");
                        await SlewToCoordinatesAsync(targetCoords, token);

                        Logger.Trace("Telescope will stop tracking");
                        result = SetTrackingEnabled(false);
                    }
                } catch (OperationCanceledException) {
                    Notification.ShowWarning(Loc.Instance["LblTelescopeParkCancelled"]);
                    result = false;
                } catch (Exception e) {
                    Logger.Error($"An error occured while attmepting to park: {e}");
                    Notification.ShowError(e.Message);

                    result = false;
                } finally {
                    IsParkingOrHoming = false;
                    progress?.Report(new ApplicationStatus { Status = string.Empty });
                }

                if (result) {
                    Logger.Trace("Telescope has parked");
                }
            });

            return result;
        }

        public async Task<bool> SetParkPosition() {
            if (Telescope.CanSetPark && !Telescope.AtPark) {
                Logger.Info($"Setting telescope park position to {Telescope.Coordinates}");
                await Task.Run(() => { Telescope.Setpark(); });

                return true;
            }

            return false;
        }

        /// <summary>
        /// Finds a theoretical home position for the telescope to return to. It will be pointing to the Celestial Pole, but in such a way that
        /// CW bar should be nearly vertical, and there is no meridian flip involved.
        /// </summary>
        /// <param name="currentCoordinates"></param>
        /// <returns></returns>
        private Coordinates GetHomeCoordinates(Coordinates currentCoordinates) {
            double siderealTime = AstroUtil.GetLocalSiderealTimeNow(profileService.ActiveProfile.AstrometrySettings.Longitude);
            if (siderealTime > 24) {
                siderealTime -= 24;
            }
            if (siderealTime < 0) {
                siderealTime += 24;
            }
            double timeToMed = currentCoordinates.RA - siderealTime;
            Coordinates returnCoordinates = new Coordinates(Angle.ByHours(0), Angle.ByDegree(0), Epoch.J2000);
            if (profileService.ActiveProfile.AstrometrySettings.HemisphereType == Hemisphere.NORTHERN) {
                returnCoordinates.Dec = 89;
                returnCoordinates.RA = siderealTime + 6 * Math.Sign(timeToMed);
            }
            if (profileService.ActiveProfile.AstrometrySettings.HemisphereType == Hemisphere.SOUTHERN) {
                returnCoordinates.Dec = -89;
                returnCoordinates.RA = siderealTime + 6 * Math.Sign(timeToMed);
            }
            return returnCoordinates;
        }

        private void UnparkTelescope(object o) {
            Telescope.Unpark();
        }

        public void UnparkTelescope() {
            if (Telescope.Connected && Telescope.CanUnpark && Telescope.AtPark) {
                Telescope.Unpark();
            }
        }

        public async Task<bool> FindHome(IProgress<ApplicationStatus> progress, CancellationToken token) {
            bool success = false;
            Logger.Info("Telescope ordered to locate home position");

            await Task.Run(async () => {
                string reason = string.Empty;
                IsParkingOrHoming = true;

                if (Telescope.Connected) {
                    if (Telescope.CanFindHome) {
                        if (!Telescope.AtHome) {
                            if (!Telescope.AtPark) {
                                try {
                                    progress?.Report(new ApplicationStatus { Status = Loc.Instance["LblWaitingForTelescopeToFindHome"] });
                                    Telescope.FindHome();

                                    // Detect if the homing process was cancelled by an external force, such as the user hitting the stop button
                                    // in another app or in the driver's own UI, external to NINA.
                                    if (!Telescope.AtHome && !Telescope.Slewing) {
                                        Logger.Warning("Find home operation appears to have been cancelled by an external process");
                                        throw new OperationCanceledException();
                                    } else {
                                        // Defend against drivers that might surprise us with a non-conformant async FindHome()
                                        // Also catch cases where the user cancelled the procedure by hitting the Stop button
                                        while (!Telescope.AtHome && Telescope.Slewing) {
                                            if (token.IsCancellationRequested) {
                                                Logger.Warning("Find home cancelled");
                                                throw new OperationCanceledException();
                                            }

                                            await CoreUtil.Delay(TimeSpan.FromSeconds(2), token);
                                        }
                                    }
                                    // We are home
                                    success = true;
                                } catch (OperationCanceledException) {
                                    Notification.ShowWarning(Loc.Instance["LblTelescopeFindHomeCancelled"]);
                                } catch (Exception e) {
                                    reason = e.Message;
                                    Notification.ShowError(e.Message);
                                } finally {
                                    IsParkingOrHoming = false;
                                    progress?.Report(new ApplicationStatus { Status = string.Empty });
                                }
                            } else {
                                // AtPark == true
                                Notification.ShowWarning(Loc.Instance["LblTelescopeAtHomeParkedWarn"]);
                                reason = "it is parked";
                            }
                        } else {
                            // AtHome == true
                            Notification.ShowWarning(Loc.Instance["LblTelescopeAtHomeWarn"]);
                            reason = "it is already at the home position";
                        }
                    } else {
                        // CanFindHome == false
                        Notification.ShowError(Loc.Instance["LblTelescopeNoFindHomeError"]);
                        reason = "it is not capable of doing so";
                    }
                }

                if (success) {
                    Logger.Trace("Telescope has located its home position");
                } else {
                    Logger.Error($"Telescope cannot locate home because {reason}");
                }
            });

            return success;
        }

        //private DispatcherTimer _updateTelescope;

        private ITelescope _telescope;

        public ITelescope Telescope {
            get {
                return _telescope;
            }
            private set {
                _telescope = value;
                RaisePropertyChanged();
            }
        }

        private TelescopeChooserVM _telescopeChooserVM;

        public TelescopeChooserVM TelescopeChooserVM {
            get {
                if (_telescopeChooserVM == null) {
                    _telescopeChooserVM = new TelescopeChooserVM(profileService);
                }
                return _telescopeChooserVM;
            }
            set {
                _telescopeChooserVM = value;
            }
        }

        public IWindowService WindowService { get; set; } = new WindowService();

        private readonly SemaphoreSlim ss = new SemaphoreSlim(1, 1);

        private async Task<bool> ChooseTelescope() {
            await ss.WaitAsync();
            try {
                await Disconnect();
                if (updateTimer != null) {
                    await updateTimer.Stop();
                }

                if (TelescopeChooserVM.SelectedDevice.Id == "No_Device") {
                    profileService.ActiveProfile.TelescopeSettings.Id = TelescopeChooserVM.SelectedDevice.Id;
                    return false;
                }

                this.applicationStatusMediator.StatusUpdate(
                    new ApplicationStatus() {
                        Source = Title,
                        Status = Loc.Instance["LblConnecting"]
                    }
                );

                var telescope = (ITelescope)TelescopeChooserVM.SelectedDevice;
                _cancelChooseTelescopeSource?.Dispose();
                _cancelChooseTelescopeSource = new CancellationTokenSource();
                if (telescope != null) {
                    try {
                        var connected = await telescope?.Connect(_cancelChooseTelescopeSource.Token);
                        _cancelChooseTelescopeSource.Token.ThrowIfCancellationRequested();
                        if (connected) {
                            Telescope = telescope;

                            if (Telescope.EquatorialSystem == Epoch.B1950 || Telescope.EquatorialSystem == Epoch.J2050) {
                                Logger.Error($"Mount uses an unsupported equatorial system: {Telescope.EquatorialSystem}");
                                throw new OperationCanceledException(string.Format(Loc.Instance["LblUnsupportedEpoch"], Telescope.EquatorialSystem));
                            }

                            if (Telescope.HasUnknownEpoch) {
                                Logger.Warning($"Mount reported an Unknown or Other equatorial system. Defaulting to {Telescope.EquatorialSystem}");
                                Notification.ShowWarning(string.Format(Loc.Instance["LblUnknownEpochWarning"], Telescope.EquatorialSystem));
                            }

                            if (Math.Abs(Telescope.SiteLatitude - profileService.ActiveProfile.AstrometrySettings.Latitude) > LAT_LONG_TOLERANCE
                                || Math.Abs(Telescope.SiteLongitude - profileService.ActiveProfile.AstrometrySettings.Longitude) > LAT_LONG_TOLERANCE) {
                                var syncVM = new TelescopeLatLongSyncVM(
                                    Telescope.CanSetSiteLatLong,
                                    profileService.ActiveProfile.AstrometrySettings.Latitude,
                                    profileService.ActiveProfile.AstrometrySettings.Longitude,
                                    Telescope.SiteLatitude,
                                    Telescope.SiteLongitude
                                );
                                await WindowService.ShowDialog(syncVM, Loc.Instance["LblSyncLatLong"], System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.ToolWindow);

                                if (syncVM.Mode == LatLongSyncMode.NINA) {
                                    profileService.ChangeLatitude(Telescope.SiteLatitude);
                                    profileService.ChangeLongitude(Telescope.SiteLongitude);
                                } else if (syncVM.Mode == LatLongSyncMode.TELESCOPE) {
                                    Telescope.SiteLatitude = profileService.ActiveProfile.AstrometrySettings.Latitude;
                                    Telescope.SiteLongitude = profileService.ActiveProfile.AstrometrySettings.Longitude;
                                }
                            }

                            TelescopeInfo = new TelescopeInfo {
                                Altitude = Telescope.Altitude,
                                AltitudeString = Telescope.AltitudeString,
                                AtPark = Telescope.AtPark,
                                AtHome = Telescope.AtHome,
                                Azimuth = Telescope.Azimuth,
                                AzimuthString = Telescope.AzimuthString,
                                Connected = true,
                                Coordinates = Telescope.Coordinates,
                                Declination = Telescope.Declination,
                                DeclinationString = Telescope.DeclinationString,
                                HoursToMeridianString = Telescope.HoursToMeridianString,
                                Name = Telescope.Name,
                                RightAscension = Telescope.RightAscension,
                                RightAscensionString = Telescope.RightAscensionString,
                                SiderealTime = Telescope.SiderealTime,
                                SiderealTimeString = Telescope.SiderealTimeString,
                                SiteElevation = Telescope.SiteElevation,
                                SiteLatitude = Telescope.SiteLatitude,
                                SiteLongitude = Telescope.SiteLongitude,
                                TimeToMeridianFlip = Telescope.TimeToMeridianFlip,
                                TimeToMeridianFlipString = Telescope.TimeToMeridianFlipString,
                                SideOfPier = Telescope.SideOfPier,
                                TrackingModes = Telescope.TrackingModes,
                                TrackingRate = Telescope.TrackingRate,
                                TrackingEnabled = Telescope.TrackingEnabled,
                                CanSetTrackingEnabled = Telescope.CanSetTrackingEnabled,
                                CanFindHome = Telescope.CanFindHome,
                                CanPark = Telescope.CanPark,
                                CanSetPark = Telescope.CanSetPark,
                                EquatorialSystem = Telescope.EquatorialSystem,
                                HasUnknownEpoch = Telescope.HasUnknownEpoch,
                                TargetCoordinates = Telescope.TargetCoordinates,
                                TargetSideOfPier = Telescope.TargetSideOfPier,
                                Slewing = Telescope.Slewing,
                                GuideRateRightAscensionArcsecPerSec = Telescope.GuideRateRightAscensionArcsecPerSec,
                                GuideRateDeclinationArcsecPerSec = Telescope.GuideRateDeclinationArcsecPerSec,
                            };

                            // Supporting custom would require an additional dialog box to input the custom rates. We can add that later if there's demand for it
                            SupportedTrackingModes = new AsyncObservableCollection<TrackingMode>(Telescope.TrackingModes.Where(m => m != TrackingMode.Custom));

                            BroadcastTelescopeInfo();

                            updateTimer.Interval = profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval;
                            updateTimer.Start();

                            Notification.ShowSuccess(Loc.Instance["LblTelescopeConnected"]);
                            profileService.ActiveProfile.TelescopeSettings.Id = Telescope.Id;

                            Logger.Info($"Successfully connected Telescope. Id: {telescope.Id} Name: {telescope.Name} Driver Version: {telescope.DriverVersion}");

                            return true;
                        } else {
                            Telescope = null;
                            return false;
                        }
                    } catch (OperationCanceledException ex) {
                        if (telescope?.Connected == true) { await Disconnect(); }
                        Notification.ShowError(ex.Message);
                        return false;
                    }
                } else {
                    return false;
                }
            } finally {
                ss.Release();
                this.applicationStatusMediator.StatusUpdate(
                    new ApplicationStatus() {
                        Source = Title,
                        Status = string.Empty
                    }
                );
            }
        }

        private void BroadcastTelescopeInfo() {
            telescopeMediator.Broadcast(TelescopeInfo);
        }

        private TelescopeInfo telescopeInfo;

        public TelescopeInfo TelescopeInfo {
            get {
                if (telescopeInfo == null) {
                    telescopeInfo = DeviceInfo.CreateDefaultInstance<TelescopeInfo>();
                    telescopeInfo.SideOfPier = PierSide.pierUnknown;
                }
                return telescopeInfo;
            }
            set {
                telescopeInfo = value;
                RaisePropertyChanged();
            }
        }

        private DeviceUpdateTimer updateTimer;

        private void UpdateTelescopeValues(Dictionary<string, object> telescopeValues) {
            object o = null;
            telescopeValues.TryGetValue(nameof(TelescopeInfo.Connected), out o);
            TelescopeInfo.Connected = (bool)(o ?? false);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.Altitude), out o);
            TelescopeInfo.Altitude = (double)(o ?? double.NaN);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.AltitudeString), out o);
            TelescopeInfo.AltitudeString = (string)(o ?? string.Empty);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.Azimuth), out o);
            TelescopeInfo.Azimuth = (double)(o ?? double.NaN);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.AzimuthString), out o);
            TelescopeInfo.AzimuthString = (string)(o ?? string.Empty);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.DeclinationString), out o);
            TelescopeInfo.DeclinationString = (string)(o ?? string.Empty);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.RightAscensionString), out o);
            TelescopeInfo.RightAscensionString = (string)(o ?? string.Empty);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.SiderealTimeString), out o);
            TelescopeInfo.SiderealTimeString = (string)(o ?? string.Empty);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.SiderealTime), out o);
            TelescopeInfo.SiderealTime = (double)(o ?? double.NaN);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.Declination), out o);
            TelescopeInfo.Declination = (double)(o ?? double.NaN);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.RightAscension), out o);
            TelescopeInfo.RightAscension = (double)(o ?? double.NaN);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.HoursToMeridianString), out o);
            TelescopeInfo.HoursToMeridianString = (string)(o ?? string.Empty);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.AtPark), out o);
            TelescopeInfo.AtPark = (bool)(o ?? false);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.AtHome), out o);
            TelescopeInfo.AtHome = (bool)(o ?? false);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.SiteLatitude), out o);
            TelescopeInfo.SiteLatitude = (double)(o ?? double.NaN);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.SiteLongitude), out o);
            TelescopeInfo.SiteLongitude = (double)(o ?? double.NaN);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.SiteElevation), out o);
            TelescopeInfo.SiteElevation = (double)(o ?? double.NaN);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.TrackingRate), out o);
            TelescopeInfo.TrackingRate = (TrackingRate)(o ?? TrackingRate.STOPPED);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.CanSetTrackingEnabled), out o);
            TelescopeInfo.CanSetTrackingEnabled = (bool)(o ?? false);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.TrackingEnabled), out o);
            TelescopeInfo.TrackingEnabled = (bool)(o ?? false);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.TrackingModes), out o);
            TelescopeInfo.TrackingModes = (IList<TrackingMode>)(o ?? ImmutableList<TrackingMode>.Empty);

            telescopeValues.TryGetValue(nameof(Coordinates), out o);
            TelescopeInfo.Coordinates = (Coordinates)(o ?? null);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.TimeToMeridianFlip), out o);
            TelescopeInfo.TimeToMeridianFlip = (double)(o ?? double.NaN);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.TimeToMeridianFlipString), out o);
            TelescopeInfo.TimeToMeridianFlipString = (string)(o ?? string.Empty);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.SideOfPier), out o);
            TelescopeInfo.SideOfPier = (PierSide)(o ?? new PierSide());

            telescopeValues.TryGetValue(nameof(TelescopeInfo.TargetCoordinates), out o);
            TelescopeInfo.TargetCoordinates = (Coordinates)(o ?? null);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.TargetSideOfPier), out o);
            TelescopeInfo.TargetSideOfPier = (PierSide?)(o ?? null);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.Slewing), out o);
            TelescopeInfo.Slewing = (bool)(o ?? false);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.GuideRateRightAscensionArcsecPerSec), out o);
            TelescopeInfo.GuideRateRightAscensionArcsecPerSec = (double)(o ?? double.NaN);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.GuideRateDeclinationArcsecPerSec), out o);
            TelescopeInfo.GuideRateDeclinationArcsecPerSec = (double)(o ?? double.NaN);

            BroadcastTelescopeInfo();
        }

        private Dictionary<string, object> GetTelescopeValues() {
            Dictionary<string, object> telescopeValues = new Dictionary<string, object>();

            telescopeValues.Add(nameof(TelescopeInfo.Connected), _telescope?.Connected ?? false);
            telescopeValues.Add(nameof(TelescopeInfo.AtPark), _telescope?.AtPark ?? false);
            telescopeValues.Add(nameof(TelescopeInfo.AtHome), _telescope?.AtHome ?? false);
            telescopeValues.Add(nameof(TelescopeInfo.CanSetTrackingEnabled), _telescope?.CanSetTrackingEnabled ?? false);
            telescopeValues.Add(nameof(TelescopeInfo.TrackingRate), _telescope?.TrackingRate ?? TrackingRate.STOPPED);
            telescopeValues.Add(nameof(TelescopeInfo.TrackingEnabled), _telescope?.TrackingEnabled ?? false);
            telescopeValues.Add(nameof(TelescopeInfo.TrackingModes), _telescope?.TrackingModes ?? ImmutableList<TrackingMode>.Empty);
            telescopeValues.Add(nameof(TelescopeInfo.Altitude), _telescope?.Altitude ?? double.NaN);
            telescopeValues.Add(nameof(TelescopeInfo.AltitudeString), _telescope?.AltitudeString ?? string.Empty);
            telescopeValues.Add(nameof(TelescopeInfo.Azimuth), _telescope?.Azimuth ?? double.NaN);
            telescopeValues.Add(nameof(TelescopeInfo.AzimuthString), _telescope?.AzimuthString ?? string.Empty);
            telescopeValues.Add(nameof(TelescopeInfo.DeclinationString), _telescope?.DeclinationString ?? string.Empty);
            telescopeValues.Add(nameof(TelescopeInfo.RightAscensionString), _telescope?.RightAscensionString ?? string.Empty);
            telescopeValues.Add(nameof(TelescopeInfo.SiderealTimeString), _telescope?.SiderealTimeString ?? string.Empty);
            telescopeValues.Add(nameof(TelescopeInfo.RightAscension), _telescope?.RightAscension ?? double.NaN);
            telescopeValues.Add(nameof(TelescopeInfo.Declination), _telescope?.Declination ?? double.NaN);
            telescopeValues.Add(nameof(TelescopeInfo.SiderealTime), _telescope?.SiderealTime ?? double.NaN);
            telescopeValues.Add(nameof(TelescopeInfo.HoursToMeridianString), _telescope?.HoursToMeridianString ?? string.Empty);
            telescopeValues.Add(nameof(TelescopeInfo.SiteLongitude), _telescope?.SiteLongitude ?? double.NaN);
            telescopeValues.Add(nameof(TelescopeInfo.SiteLatitude), _telescope?.SiteLatitude ?? double.NaN);
            telescopeValues.Add(nameof(TelescopeInfo.SiteElevation), _telescope?.SiteElevation ?? double.NaN);
            telescopeValues.Add(nameof(TelescopeInfo.Coordinates), _telescope?.Coordinates ?? null);
            telescopeValues.Add(nameof(TelescopeInfo.TimeToMeridianFlip), _telescope?.TimeToMeridianFlip ?? double.NaN);
            telescopeValues.Add(nameof(TelescopeInfo.TimeToMeridianFlipString), _telescope?.TimeToMeridianFlipString ?? string.Empty);
            telescopeValues.Add(nameof(TelescopeInfo.SideOfPier), _telescope?.SideOfPier ?? new PierSide());
            telescopeValues.Add(nameof(TelescopeInfo.TargetCoordinates), _telescope?.TargetCoordinates ?? null);
            telescopeValues.Add(nameof(TelescopeInfo.TargetSideOfPier), _telescope?.TargetSideOfPier ?? null);
            telescopeValues.Add(nameof(TelescopeInfo.Slewing), _telescope?.Slewing ?? false);
            telescopeValues.Add(nameof(TelescopeInfo.GuideRateRightAscensionArcsecPerSec), _telescope?.GuideRateRightAscensionArcsecPerSec ?? double.NaN);
            telescopeValues.Add(nameof(TelescopeInfo.GuideRateDeclinationArcsecPerSec), _telescope?.GuideRateDeclinationArcsecPerSec ?? double.NaN);

            return telescopeValues;
        }

        private void CancelChooseTelescope(object o) {
            _cancelChooseTelescopeSource?.Cancel();
        }

        private CancellationTokenSource _cancelChooseTelescopeSource;

        private CancellationTokenSource _cancelSlewTelescopeSource;

        private void InitCancelSlewTelescope() {
            _cancelSlewTelescopeSource?.Dispose();
            _cancelSlewTelescopeSource = new CancellationTokenSource();
        }

        private void CancelSlewTelescope() {
            _cancelSlewTelescopeSource?.Cancel();
        }

        private async Task<bool> DisconnectTelescope() {
            var diag = MyMessageBox.Show(Loc.Instance["LblDisconnectTelescope"], "", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);
            if (diag == System.Windows.MessageBoxResult.OK) {
                await Disconnect();
            }
            return true;
        }

        public async Task Disconnect() {
            if (Telescope != null) { Logger.Info("Disconnected Telescope"); }
            if (updateTimer != null) {
                await updateTimer.Stop();
            }
            Telescope?.Disconnect();
            Telescope = null;
            TelescopeInfo = DeviceInfo.CreateDefaultInstance<TelescopeInfo>();
            BroadcastTelescopeInfo();
        }

        public void MoveAxis(TelescopeAxes axis, double rate) {
            if (TelescopeInfo.Connected) {
                Telescope.MoveAxis(axis, rate);
            }
        }

        public void PulseGuide(GuideDirections direction, int duration) {
            if (TelescopeInfo.Connected) {
                Telescope.PulseGuide(direction, duration);
            }
        }

        public async Task<bool> Sync(Coordinates coordinates) {
            try {
                var transform = coordinates.Transform(TelescopeInfo.EquatorialSystem);
                if (!profileService.ActiveProfile.TelescopeSettings.NoSync && TelescopeInfo.Connected) {
                    progress.Report(new ApplicationStatus() { Status = Loc.Instance["LblSync"] });

                    var position = GetCurrentPosition();
                    bool result = Telescope.Sync(transform);
                    Logger.Info($"{(result ? string.Empty : "FAILED - ")}Syncing scope from {position} to {transform}");
                    await Task.Delay(TimeSpan.FromSeconds(Math.Max(2, profileService.ActiveProfile.TelescopeSettings.SettleTime)));
                    return result;
                } else {
                    return false;
                }
            } finally {
                progress.Report(new ApplicationStatus() { Status = string.Empty });
            }
        }

        private void Move(object obj) {
            string cmd = obj.ToString();
            if (cmd == "W") {
                MoveAxis(TelescopeAxes.Primary, -Telescope.PrimaryMovingRate);
            }
            if (cmd == "O") {
                MoveAxis(TelescopeAxes.Primary, Telescope.PrimaryMovingRate);
            }
            if (cmd == "N") {
                MoveAxis(TelescopeAxes.Secondary, Telescope.SecondaryMovingRate);
            }
            if (cmd == "S") {
                MoveAxis(TelescopeAxes.Secondary, -Telescope.SecondaryMovingRate);
            }
        }

        private void StopMove(object obj) {
            string cmd = obj.ToString();
            if (cmd == "W") {
                MoveAxis(TelescopeAxes.Primary, 0);
            }
            if (cmd == "O") {
                MoveAxis(TelescopeAxes.Primary, 0);
            }
            if (cmd == "N") {
                MoveAxis(TelescopeAxes.Secondary, 0);
            }
            if (cmd == "S") {
                MoveAxis(TelescopeAxes.Secondary, 0);
            }
        }

        private void StopSlew(object obj) {
            try {
                Telescope.StopSlew();
                CancelSlewTelescope();
            } catch (Exception e) {
                Logger.Error(e);
                Notification.ShowError(e.Message);
            }
        }

        private int _targetDeclinationDegrees;

        public int TargetDeclinationDegrees {
            get {
                return _targetDeclinationDegrees;
            }

            set {
                _targetDeclinationDegrees = value;
                RaisePropertyChanged();
            }
        }

        private int _targetDeclinationMinutes;

        public int TargetDeclinationMinutes {
            get {
                return _targetDeclinationMinutes;
            }

            set {
                _targetDeclinationMinutes = value;
                RaisePropertyChanged();
            }
        }

        private double _targetDeclinationSeconds;

        public double TargetDeclinationSeconds {
            get {
                return _targetDeclinationSeconds;
            }

            set {
                _targetDeclinationSeconds = value;
                RaisePropertyChanged();
            }
        }

        private int _targetRightAscencionHours;

        public int TargetRightAscencionHours {
            get {
                return _targetRightAscencionHours;
            }

            set {
                _targetRightAscencionHours = value;
                RaisePropertyChanged();
            }
        }

        private int _targetRightAscencionMinutes;

        public int TargetRightAscencionMinutes {
            get {
                return _targetRightAscencionMinutes;
            }

            set {
                _targetRightAscencionMinutes = value;
                RaisePropertyChanged();
            }
        }

        private double _targetRightAscencionSeconds;
        private ITelescopeMediator telescopeMediator;
        private IApplicationStatusMediator applicationStatusMediator;
        private IDomeMediator domeMediator;
        private IProgress<ApplicationStatus> progress;

        public double TargetRightAscencionSeconds {
            get {
                return _targetRightAscencionSeconds;
            }

            set {
                _targetRightAscencionSeconds = value;
                RaisePropertyChanged();
            }
        }

        private bool isParkingOrHoming = false;

        /// <summary>
        ///  Is <see langword="true"/> only while the telescope is in the process of parking. Is <see langword="false"/> at any other time, including when the telescope is parked.
        /// </summary>
        public bool IsParkingOrHoming {
            get => isParkingOrHoming;
            private set {
                if (isParkingOrHoming != value) {
                    isParkingOrHoming = value;
                    RaisePropertyChanged();
                }
            }
        }

        public async Task<bool> SlewToCoordinatesAsync(Coordinates coords, CancellationToken token) {
            try {
                coords = coords.Transform(TelescopeInfo.EquatorialSystem);
                if (Telescope?.Connected == true) {
                    progress.Report(
                        new ApplicationStatus() {
                            Status = Loc.Instance["LblSlew"]
                        }
                    );

                    if (Telescope?.CanPark == true && Telescope?.AtPark == true) {
                        Logger.Warning("Telescope is parked");
                        Notification.ShowWarning(Loc.Instance["LblTelescopeParkedWarning"]);
                    }

                    var position = GetCurrentPosition();
                    Logger.Info($"Slewing from {position} to {coords}");

                    await Telescope.SlewToCoordinates(coords, token);
                    BroadcastTelescopeInfo();
                    await Task.WhenAll(
                        CoreUtil.Wait(TimeSpan.FromSeconds(profileService.ActiveProfile.TelescopeSettings.SettleTime), token, progress, Loc.Instance["LblSettle"]),
                        this.domeMediator.WaitForDomeSynchronization(token));
                    return true;
                } else {
                    Logger.Warning("Telescope is not connected to slew");
                    return false;
                }
            } finally {
                progress.Report(new ApplicationStatus() { Status = string.Empty });
            }
        }

        private Task<bool> SlewToCoordinatesInternal(object obj) {
            var targetRightAscencion = TargetRightAscencionHours + AstroUtil.ArcminToDegree(TargetRightAscencionMinutes) + AstroUtil.ArcsecToDegree(TargetRightAscencionSeconds);
            var targetDeclination = TargetDeclinationDegrees + Math.Sign(TargetDeclinationDegrees) * AstroUtil.ArcminToDegree(TargetDeclinationMinutes) + Math.Sign(TargetDeclinationDegrees) * AstroUtil.ArcsecToDegree(TargetDeclinationSeconds);

            var coords = new Coordinates(targetRightAscencion, targetDeclination, Epoch.J2000, Coordinates.RAType.Hours);
            coords = coords.Transform(TelescopeInfo.EquatorialSystem);
            return SlewToCoordinatesAsync(coords, CancellationToken.None);
        }

        public async Task<bool> MeridianFlip(Coordinates targetCoordinates, CancellationToken token) {
            var coords = targetCoordinates.Transform(TelescopeInfo.EquatorialSystem);
            if (TelescopeInfo.Connected) {
                var flipResult = await Telescope.MeridianFlip(coords, token);
                await this.domeMediator.WaitForDomeSynchronization(CancellationToken.None);
                return flipResult;
            } else {
                return false;
            }
        }

        public Task<bool> Connect() {
            return ChooseTelescope();
        }

        public TelescopeInfo GetDeviceInfo() {
            return TelescopeInfo;
        }

        public Task<bool> SlewToCoordinatesAsync(TopocentricCoordinates coordinates, CancellationToken token) {
            var transformed = coordinates.Transform(TelescopeInfo.EquatorialSystem);
            return this.SlewToCoordinatesAsync(transformed, token);
        }

        public Coordinates GetCurrentPosition() {
            return Telescope?.Coordinates;
        }

        public async Task WaitForSlew(CancellationToken cancellationToken) {
            if (Telescope?.Connected == true) {
                while (Telescope?.Slewing == true && !cancellationToken.IsCancellationRequested) {
                    await Task.Delay(1000);
                }
            }
        }

        private void HandleSetTrackingEnabledCommand(object p) {
            var enabled = (bool)p;
            SetTrackingEnabled(enabled);
        }

        private void HandleSetTrackingModeCommand(object p) {
            if (p != null) {
                SetTrackingMode((TrackingMode)p);
            }
        }

        public bool SetTrackingEnabled(bool tracking) {
            if (TelescopeInfo.Connected) {
                Telescope.TrackingEnabled = tracking;
                return Telescope.TrackingEnabled;
            } else {
                return false;
            }
        }

        public bool SetTrackingMode(TrackingMode trackingMode) {
            if (Telescope?.Connected == true && trackingMode != TrackingMode.Custom) {
                Telescope.TrackingMode = trackingMode;
                return Telescope.TrackingMode == trackingMode;
            }
            return false;
        }

        public bool SetCustomTrackingRate(double rightAscensionRate, double declinationRate) {
            if (Telescope?.Connected == true) {
                Telescope.SetCustomTrackingRate(rightAscensionRate, declinationRate);
                return true;
            }
            return false;
        }

        private AsyncObservableCollection<TrackingMode> supportedTrackingModes = new AsyncObservableCollection<TrackingMode>();

        public AsyncObservableCollection<TrackingMode> SupportedTrackingModes {
            get {
                return supportedTrackingModes;
            }
            set {
                supportedTrackingModes = value;
                RaisePropertyChanged();
            }
        }

        public IAsyncCommand SlewToCoordinatesCommand { get; private set; }

        public IAsyncCommand ChooseTelescopeCommand { get; private set; }
        public ICommand CancelChooseTelescopeCommand { get; private set; }
        public ICommand DisconnectCommand { get; private set; }

        public ICommand MoveCommand { get; private set; }

        public ICommand StopMoveCommand { get; private set; }

        public IAsyncCommand ParkCommand { get; private set; }

        public IAsyncCommand SetParkPositionCommand { get; private set; }

        public ICommand UnparkCommand { get; private set; }

        public ICommand StopSlewCommand { get; private set; }

        public ICommand RefreshTelescopeListCommand { get; private set; }

        public ICommand SetTrackingEnabledCommand { get; private set; }

        public ICommand SetTrackingModeCommand { get; private set; }

        public IAsyncCommand FindHomeCommand { get; private set; }
    }
}