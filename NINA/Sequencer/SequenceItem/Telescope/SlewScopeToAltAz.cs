#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Model;
using NINA.Profile;
using NINA.Sequencer.Container;
using NINA.Sequencer.Exceptions;
using NINA.Sequencer.Validations;
using NINA.Utility.Astrometry;
using NINA.Utility.Mediator.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.SequenceItem.Telescope {

    [ExportMetadata("Name", "Lbl_SequenceItem_Telescope_SlewScopeToAltAz_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Telescope_SlewScopeToAltAz_Description")]
    [ExportMetadata("Icon", "SlewToAltAzSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Telescope")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SlewScopeToAltAz : SequenceItem, IValidatable {

        [ImportingConstructor]
        public SlewScopeToAltAz(IProfileService profileService, ITelescopeMediator telescopeMediator) {
            this.profileService = profileService;
            this.telescopeMediator = telescopeMediator;
            Coordinates = new InputTopocentricCoordinates(Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Latitude), Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Longitude));
        }

        private IProfileService profileService;
        private ITelescopeMediator telescopeMediator;

        [JsonProperty]
        public InputTopocentricCoordinates Coordinates { get; set; }

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (Validate()) {
                // Todo - this interface lacks progress and token
                return telescopeMediator.SlewToCoordinatesAsync(Coordinates.Coordinates);
            } else {
                throw new SequenceItemSkippedException(string.Join(",", Issues));
            }
        }

        public override void AfterParentChanged() {
            Validate();
        }

        public override object Clone() {
            return new SlewScopeToAltAz(profileService, telescopeMediator) {
                Icon = Icon,
                Name = Name,
                Category = Category,
                Description = Description,
                Coordinates = new InputTopocentricCoordinates(Coordinates.Coordinates.Copy()),
            };
        }

        public bool Validate() {
            var i = new List<string>();
            if (!telescopeMediator.GetInfo().Connected) {
                i.Add(Locale.Loc.Instance["LblTelescopeNotConnected"]);
            }
            Issues = i;
            return i.Count == 0;
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(SlewScopeToAltAz)}, Coordinates: {Coordinates}";
        }
    }
}