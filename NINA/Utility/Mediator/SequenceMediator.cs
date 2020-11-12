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
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel.Interfaces;
using NINA.ViewModel.Sequencer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator {

    public class SequenceMediator : ISequenceMediator {
        private ISequenceNavigationVM sequenceNavigation;

        public void RegisterSequenceNavigation(ISequenceNavigationVM sequenceNavigation) {
            if (this.sequenceNavigation != null) {
                throw new Exception("Sequencer already registered!");
            }
            this.sequenceNavigation = sequenceNavigation;
        }

        public void AddSimpleTarget(DeepSkyObject deepSkyObject) {
            sequenceNavigation.AddSimpleTarget(deepSkyObject);
        }

        public void AddAdvancedTarget(IDeepSkyObjectContainer container) {
            sequenceNavigation.AddAdvancedTarget(container);
        }

        public void SetAdvancedSequence(ISequenceRootContainer container) {
            sequenceNavigation.SetAdvancedSequence(container);
        }

        public IList<IDeepSkyObjectContainer> GetDeepSkyObjectContainerTemplates() {
            return sequenceNavigation.GetDeepSkyObjectContainerTemplates();
        }

        public void SwitchToAdvancedView() {
            sequenceNavigation.SwitchToAdvancedView();
        }

        public void SwitchToOverview() {
            sequenceNavigation.SwitchToOverview();
        }
    }
}