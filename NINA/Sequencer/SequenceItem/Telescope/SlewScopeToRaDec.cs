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

    [ExportMetadata("Name", "Lbl_SequenceItem_Telescope_SlewScopeToRaDec_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Telescope_SlewScopeToRaDec_Description")]
    [ExportMetadata("Icon", "SlewToRaDecSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Telescope")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SlewScopeToRaDec : SequenceItem, IValidatable {

        [ImportingConstructor]
        public SlewScopeToRaDec(ITelescopeMediator telescopeMediator) {
            this.telescopeMediator = telescopeMediator;
            Coordinates = new InputCoordinates();
        }

        private ITelescopeMediator telescopeMediator;

        private bool inherited;

        [JsonProperty]
        public bool Inherited {
            get => inherited;
            set {
                inherited = value;
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public InputCoordinates Coordinates { get; set; }

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
                return telescopeMediator.SlewToCoordinatesAsync(Coordinates.Coordinates, token);
            } else {
                throw new SequenceItemSkippedException(string.Join(",", Issues));
            }
        }

        public override void AfterParentChanged() {
            var coordinates = RetrieveContextCoordinates(this.Parent);
            if (coordinates != null) {
                Coordinates.Coordinates = coordinates;
                Inherited = true;
            } else {
                Inherited = false;
            }
            Validate();
        }

        private Coordinates RetrieveContextCoordinates(ISequenceContainer parent) {
            if (parent != null) {
                var container = parent as IDeepSkyObjectContainer;
                if (container != null) {
                    return container.Target.InputCoordinates.Coordinates;
                } else {
                    return RetrieveContextCoordinates(parent.Parent);
                }
            } else {
                return null;
            }
        }

        public override object Clone() {
            return new SlewScopeToRaDec(telescopeMediator) {
                Icon = Icon,
                Name = Name,
                Category = Category,
                Description = Description,
                Coordinates = new InputCoordinates(Coordinates.Coordinates),
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
            return $"Category: {Category}, Item: {nameof(SlewScopeToRaDec)}, Coordinates: {Coordinates}";
        }
    }
}