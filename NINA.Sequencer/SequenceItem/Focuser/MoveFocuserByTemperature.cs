#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.Validations;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Locale;
using NINA.Core.Utility;

namespace NINA.Sequencer.SequenceItem.Focuser {

    [ExportMetadata("Name", "Lbl_SequenceItem_Focuser_MoveFocuserByTemperature_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Focuser_MoveFocuserByTemperature_Description")]
    [ExportMetadata("Icon", "MoveFocuserByTemperatureSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Focuser")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class MoveFocuserByTemperature : SequenceItem, IValidatable, IFocuserConsumer {

        [ImportingConstructor]
        public MoveFocuserByTemperature(IFocuserMediator focuserMediator) {
            this.focuserMediator = focuserMediator;
            this.focuserMediator.RegisterConsumer(this);
        }

        private MoveFocuserByTemperature(MoveFocuserByTemperature cloneMe) : this(cloneMe.focuserMediator) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new MoveFocuserByTemperature(this) {
                Slope = Slope,
                Intercept = Intercept,
                Absolute = Absolute
            };
        }

        private IFocuserMediator focuserMediator;

        private double slope = 1;

        [JsonProperty]
        public double Slope {
            get => slope;
            set {
                slope = value;
                RaisePropertyChanged();
            }
        }

        private static double lastTemperature = -1000;

        private static double lastRoundoff = 0;

        private bool absolute = true;

        [JsonProperty]
        public bool Absolute {
            get => absolute;
            set {
                absolute = value;
                RaisePropertyChanged();
            }
        }

        private double intercept = 0;

        [JsonProperty]
        public double Intercept {
            get => intercept;
            set {
                intercept = value;
                RaisePropertyChanged();
            }
        }

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
                var info = focuserMediator.GetInfo();
                double position = info.Position, delta = 0;
                int deltaInt = 0;
                double thisTemperature = 0;
                Task<int> result;
                if (absolute) {
                    position = Slope * info.Temperature + Intercept;
                    Logger.Info($"Moving Focuser By Temperature - Slope {Slope} * Temperature {info.Temperature} °C + Intercept {Intercept} = Position {position}");
                    result = focuserMediator.MoveFocuser((int)position, token);
                } else {
                    if (lastTemperature == -1000) {
                        thisTemperature = info.Temperature;
                        delta = 0;
                        deltaInt = 0;
                        Logger.Info($"Moving Focuser By Temperature - Slope {Slope} * ( DeltaT ) °C (relative mode) - lastTemperature initialized to {lastTemperature}");
                    } else {
                        delta = lastRoundoff + (info.Temperature - lastTemperature) * Slope;
                        deltaInt = (int)Math.Round(delta);
                        thisTemperature = info.Temperature;
                        Logger.Info($"Moving Focuser By Temperature - LastRoundoff {lastRoundoff} + Slope {Slope} * ( Temperature {thisTemperature} - PrevTemperature {lastTemperature} ) °C (relative mode) = Delta {delta} / DeltaInt {deltaInt}");
                    }
                    result = focuserMediator.MoveFocuserRelative((int)delta, token);
                    lastTemperature = thisTemperature;
                    lastRoundoff = delta - deltaInt;
                }
                return result;
            } else {
                throw new SequenceItemSkippedException(string.Join(",", Issues));
            }
        }

        public bool Validate() {
            var i = new List<string>();
            var info = focuserMediator.GetInfo();
            if (!info.Connected) {
                i.Add(Loc.Instance["LblFocuserNotConnected"]);
            } else {
                if (double.IsNaN(info.Temperature)) {
                    i.Add(Loc.Instance["Lbl_SequenceItem_Focuser_MoveFocuserByTemperature_Validation_NoTemperature"]);
                }
            }
            Issues = i;
            return i.Count == 0;
        }

        public string MiniDescription {
            get {
                return $"{Slope} * " + (absolute ? $"T + {Intercept}" : "DeltaT");
            }
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(MoveFocuserByTemperature)}, Slope: {Slope}" + (absolute ? $", Intercept {Intercept}" : " (Relative mode)");
        }

        public void UpdateDeviceInfo(Equipment.Equipment.MyFocuser.FocuserInfo deviceInfo) {
            ;
        }

        public void Dispose() {
            focuserMediator.RemoveConsumer(this);
        }

        public void UpdateEndAutoFocusRun(AutoFocusInfo info) {
            Logger.Info($"Autofocus notification received - Temperature {info.Temperature}");
            lastTemperature = info.Temperature;
            lastRoundoff = 0;
        }

        public void UpdateUserFocused(Equipment.Equipment.MyFocuser.FocuserInfo info) {
            Logger.Info($"User Focused notification received - Temperature {info.Temperature}");
            lastTemperature = info.Temperature;
            lastRoundoff = 0;
        }
    }
}