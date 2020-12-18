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
using NINA.Model.MyPlanetarium;
using NINA.Profile;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container.ExecutionStrategy;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Exceptions;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using NINA.ViewModel;
using NINA.ViewModel.FramingAssistant;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.Sequencer.Container {

    [ExportMetadata("Name", "Lbl_SequenceContainer_DeepSkyObjectContainer_Name")]
    [ExportMetadata("Description", "Lbl_SequenceContainer_DeepSkyObjectContainer_Description")]
    [ExportMetadata("Icon", "TelescopeSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Container")]
    [Export(typeof(ISequenceItem))]
    [Export(typeof(ISequenceContainer))]
    [JsonObject(MemberSerialization.OptIn)]
    public class DeepSkyObjectContainer : SequenceContainer, IDeepSkyObjectContainer {
        private readonly IProfileService profileService;
        private readonly IFramingAssistantVM framingAssistantVM;
        private readonly IPlanetariumFactory planetariumFactory;
        private readonly IApplicationMediator applicationMediator;
        private INighttimeCalculator nighttimeCalculator;
        private InputTarget target;

        [ImportingConstructor]
        public DeepSkyObjectContainer(
                IProfileService profileService,
                INighttimeCalculator nighttimeCalculator,
                IFramingAssistantVM framingAssistantVM,
                IApplicationMediator applicationMediator,
                IPlanetariumFactory planetariumFactory) : base(new SequentialStrategy()) {
            this.profileService = profileService;
            this.nighttimeCalculator = nighttimeCalculator;
            this.applicationMediator = applicationMediator;
            this.framingAssistantVM = framingAssistantVM;
            this.planetariumFactory = planetariumFactory;
            NighttimeData = nighttimeCalculator.Calculate();
            Target = new InputTarget(Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Latitude), Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Longitude), profileService.ActiveProfile.AstrometrySettings.Horizon);
            CoordsToFramingCommand = new AsyncCommand<bool>(() => Task.Run(CoordsToFraming));
            CoordsFromPlanetariumCommand = new AsyncCommand<bool>(() => Task.Run(CoordsFromPlanetarium));
            DropTargetCommand = new RelayCommand(DropTarget);

            profileService.LocationChanged += (object sender, EventArgs e) => {
                Target?.SetPosition(Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Latitude), Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Longitude));
            };

            profileService.HorizonChanged += (object sender, EventArgs e) => {
                Target?.DeepSkyObject?.SetCustomHorizon(profileService.ActiveProfile.AstrometrySettings.Horizon);
            };
        }

        private void DropTarget(object obj) {
            var p = obj as DragDrop.DropIntoParameters;
            if (p != null) {
                var con = p.Source as TargetSequenceContainer;
                if (con != null) {
                    var dropTarget = con.Container.Target;
                    if (dropTarget != null) {
                        this.Name = dropTarget.TargetName;
                        this.Target.TargetName = dropTarget.TargetName;
                        this.Target.InputCoordinates = dropTarget.InputCoordinates.Clone();
                        this.Target.Rotation = dropTarget.Rotation;
                    }
                }
            }
        }

        public NighttimeData NighttimeData { get; }

        public ICommand CoordsToFramingCommand { get; set; }
        public ICommand CoordsFromPlanetariumCommand { get; set; }
        public ICommand DropTargetCommand { get; set; }

        [JsonProperty]
        public InputTarget Target {
            get => target;
            set {
                if (Target != null) {
                    Target.PropertyChanged -= Target_PropertyChanged;
                }
                target = value;
                if (Target != null) {
                    Target.PropertyChanged += Target_PropertyChanged;
                }
                RaisePropertyChanged();
            }
        }

        private void Target_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            AfterParentChanged();
        }

        public override object Clone() {
            var clone = new DeepSkyObjectContainer(profileService, nighttimeCalculator, framingAssistantVM, applicationMediator, planetariumFactory) {
                Icon = Icon,
                Name = Name,
                Category = Category,
                Description = Description,
                Items = new ObservableCollection<ISequenceItem>(Items.Select(i => i.Clone() as ISequenceItem)),
                Triggers = new ObservableCollection<ISequenceTrigger>(Triggers.Select(t => t.Clone() as ISequenceTrigger)),
                Conditions = new ObservableCollection<ISequenceCondition>(Conditions.Select(t => t.Clone() as ISequenceCondition)),
                Target = new InputTarget(Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Latitude), Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Longitude), profileService.ActiveProfile.AstrometrySettings.Horizon)
            };

            clone.Target.TargetName = this.Target.TargetName;
            clone.Target.InputCoordinates.Coordinates = this.Target.InputCoordinates.Coordinates.Transform(Epoch.J2000);
            clone.Target.Rotation = this.Target.Rotation;

            foreach (var item in clone.Items) {
                item.AttachNewParent(clone);
            }

            foreach (var condition in clone.Conditions) {
                condition.AttachNewParent(clone);
            }

            foreach (var trigger in clone.Triggers) {
                trigger.AttachNewParent(clone);
            }

            return clone;
        }

        public override string ToString() {
            var baseString = base.ToString();
            return $"{baseString}, Target: {Target?.TargetName} {Target?.InputCoordinates?.Coordinates} {Target?.Rotation}";
        }

        private async Task<bool> CoordsToFraming() {
            if (Target.DeepSkyObject?.Coordinates != null) {
                var dso = new DeepSkyObject(Target.DeepSkyObject.Name, Target.DeepSkyObject.Coordinates, profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository, profileService.ActiveProfile.AstrometrySettings.Horizon);
                dso.Rotation = Target.Rotation;
                applicationMediator.ChangeTab(ApplicationTab.FRAMINGASSISTANT);
                return await framingAssistantVM.SetCoordinates(dso);
            }
            return false;
        }

        private async Task<bool> CoordsFromPlanetarium() {
            IPlanetarium s = planetariumFactory.GetPlanetarium();
            DeepSkyObject resp = null;

            try {
                resp = await s.GetTarget();

                if (resp != null) {
                    Target.InputCoordinates.Coordinates = resp.Coordinates;
                    Target.TargetName = resp.Name;
                    this.Name = resp.Name;

                    Target.Rotation = 0;

                    if (s.CanGetRotationAngle) {
                        double rotationAngle = await s.GetRotationAngle();

                        if (!double.IsNaN(rotationAngle)) {
                            Target.Rotation = rotationAngle;
                        }
                    }

                    Notification.ShowSuccess(string.Format(Locale.Loc.Instance["LblPlanetariumCoordsOk"], s.Name));
                }
            } catch (PlanetariumObjectNotSelectedException) {
                Logger.Error($"Attempted to get coordinates from {s.Name} when no object was selected");
                Notification.ShowError(string.Format(Locale.Loc.Instance["LblPlanetariumObjectNotSelected"], s.Name));
            } catch (PlanetariumFailedToConnect ex) {
                Logger.Error($"Unable to connect to {s.Name}: {ex}");
                Notification.ShowError(string.Format(Locale.Loc.Instance["LblPlanetariumFailedToConnect"], s.Name));
            } catch (Exception ex) {
                Logger.Error($"Failed to get coordinates from {s.Name}: {ex}");
                Notification.ShowError(string.Format(Locale.Loc.Instance["LblPlanetariumCoordsError"], s.Name));
            }

            return (resp != null);
        }
    }
}