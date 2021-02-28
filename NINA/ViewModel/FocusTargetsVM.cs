#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Model.MyTelescope;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.Profile;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using NINA.Database;
using Nito.AsyncEx;
using NINA.ViewModel.Interfaces;
using System.Threading;

namespace NINA.ViewModel {

    public class FocusTargetsVM : DockableVM, ITelescopeConsumer, IFocusTargetsVM {
        private ObservableCollection<FocusTarget> focusTargets;
        private FocusTarget selectedFocusTarget;
        private bool telescopeConnected;
        private readonly System.Timers.Timer updateTimer;

        public FocusTargetsVM(IProfileService profileService, ITelescopeMediator telescopeMediator, IApplicationResourceDictionary resourceDictionary) : base(profileService) {
            Title = "LblManualFocusTargets";
            ImageGeometry = (System.Windows.Media.GeometryGroup)resourceDictionary["FocusTargetsSVG"];

            this.telescopeMediator = telescopeMediator;
            telescopeMediator.RegisterConsumer(this);

            AsyncContext.Run(LoadFocusTargets);

            updateTimer = new System.Timers.Timer(TimeSpan.FromMinutes(1).TotalMilliseconds) { AutoReset = true };
            updateTimer.Elapsed += (sender, args) => CalculateVisibleStars();
            if (IsVisible) {
                updateTimer.Start();
            }

            SlewToCoordinatesCommand = new AsyncCommand<bool>(
                async () => await telescopeMediator.SlewToCoordinatesAsync(SelectedFocusTarget.Coordinates, CancellationToken.None));
        }

        public bool TelescopeConnected {
            get => telescopeConnected;
            set {
                telescopeConnected = value;
                RaisePropertyChanged();
            }
        }

        public FocusTarget SelectedFocusTarget {
            get => selectedFocusTarget;
            set {
                selectedFocusTarget = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<FocusTarget> FocusTargets {
            get => focusTargets;
            set {
                focusTargets = value;
                RaisePropertyChanged();
            }
        }

        private List<FocusTarget> allFocusTargets = new List<FocusTarget>();
        private ITelescopeMediator telescopeMediator;

        public IAsyncCommand SlewToCoordinatesCommand { get; }

        private async Task LoadFocusTargets() {
            try {
                var db = new DatabaseInteraction();
                allFocusTargets = await db.GetBrightStars();
                CalculateVisibleStars();
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        private void CalculateVisibleStars() {
            var selectedBrightStarName = selectedFocusTarget?.Name;
            var longitude = profileService.ActiveProfile.AstrometrySettings.Longitude;
            var latitude = profileService.ActiveProfile.AstrometrySettings.Latitude;
            foreach (var target in allFocusTargets) {
                target.CalculateAltAz(latitude, longitude);
            }

            FocusTargets = new ObservableCollection<FocusTarget>(allFocusTargets.Where(b => b.Altitude > 10).OrderByDescending(b => b.Altitude));
            SelectedFocusTarget = FocusTargets.SingleOrDefault(b => b.Name == selectedBrightStarName) ?? FocusTargets.FirstOrDefault();
        }

        public void UpdateDeviceInfo(TelescopeInfo deviceInfo) {
            if (TelescopeConnected != deviceInfo.Connected) {
                TelescopeConnected = deviceInfo.Connected;
            }
        }

        public override void Hide(object o) {
            IsVisible = !IsVisible;
            if (IsVisible) {
                CalculateVisibleStars();
                updateTimer.Start();
            } else {
                updateTimer.Stop();
            }
        }

        public void Dispose() {
            telescopeMediator.RemoveConsumer(this);
        }
    }
}