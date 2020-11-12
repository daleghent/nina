#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Profile;
using NINA.Sequencer.Container;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel.Sequencer {

    public class SequenceNavigationVM : DockableVM, ISequenceNavigationVM {
        private object activeSequencerVM;
        private ISequence2VM sequence2VM;
        private ISequenceMediator sequenceMediator;
        private ISimpleSequenceVM simpleSequenceVM;

        /// <summary>
        /// Backwards compatible ContentId due to sequencer replacement
        /// </summary>
        public new string ContentId {
            get => "SequenceVM";
        }

        public SequenceNavigationVM(IProfileService profileService, ISequenceMediator sequenceMediator, ISimpleSequenceVM simpleSequenceVM, ISequence2VM sequence2VM) : base(profileService) {
            Title = "LblSequence";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current?.Resources["SequenceSVG"];

            this.simpleSequenceVM = simpleSequenceVM;
            this.sequence2VM = sequence2VM;
            this.sequenceMediator = sequenceMediator;
            this.sequenceMediator.RegisterSequenceNavigation(this);

            AddTargetCommand = new RelayCommand((object o) => {
                simpleSequenceVM.AddTargetCommand.Execute(o);
                ActiveSequencerVM = simpleSequenceVM;
            });
            LoadSequenceCommand = new RelayCommand((object o) => {
                if (SimpleSequenceVM.LoadTarget()) {
                    ActiveSequencerVM = simpleSequenceVM;
                }
            });
            LoadTargetSetCommand = new RelayCommand((object o) => {
                if (SimpleSequenceVM.LoadTargetSet()) {
                    ActiveSequencerVM = simpleSequenceVM;
                }
            });
            ImportTargetsCommand = new RelayCommand((object o) => {
                if (SimpleSequenceVM.ImportTargets()) {
                    ActiveSequencerVM = simpleSequenceVM;
                }
            });
            SwitchToAdvancedSequenceCommand = new RelayCommand((object o) => SwitchToAdvancedView());

            if (profileService.ActiveProfile.SequenceSettings.DisableSimpleSequencer) {
                this.ActiveSequencerVM = sequence2VM;
            } else {
                ActiveSequencerVM = this;
            }
        }

        public object ActiveSequencerVM {
            get => activeSequencerVM;
            set {
                activeSequencerVM = value;
                RaisePropertyChanged();
            }
        }

        public ICommand AddTargetCommand { get; private set; }
        public ICommand LoadSequenceCommand { get; private set; }
        public ICommand LoadTargetSetCommand { get; private set; }
        public ICommand ImportTargetsCommand { get; private set; }
        public ICommand SwitchToAdvancedSequenceCommand { get; private set; }

        public ISimpleSequenceVM SimpleSequenceVM {
            get => simpleSequenceVM;
        }

        public ISequence2VM Sequence2VM {
            get => sequence2VM;
        }

        public override string ToString() {
            return string.Empty;
        }

        public void SwitchToAdvancedView() {
            ActiveSequencerVM = sequence2VM;
        }

        public void SwitchToOverview() {
            ActiveSequencerVM = this;
        }

        public void AddSimpleTarget(DeepSkyObject deepSkyObject) {
            SimpleSequenceVM.AddTarget(deepSkyObject);
            ActiveSequencerVM = SimpleSequenceVM;
        }

        public void AddAdvancedTarget(IDeepSkyObjectContainer container) {
            Sequence2VM.AddTarget(container);
            ActiveSequencerVM = Sequence2VM;
        }

        public void SetAdvancedSequence(ISequenceRootContainer container) {
            Sequence2VM.Sequencer.MainContainer = container;
        }

        public IList<IDeepSkyObjectContainer> GetDeepSkyObjectContainerTemplates() {
            return Sequence2VM.GetDeepSkyObjectContainerTemplates();
        }
    }
}