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
        private ISequenceVM oldSequencer;
        private ISequence2VM sequencer;

        public void RegisterConstructor(ISequenceVM constructor) {
            if (this.oldSequencer != null) {
                throw new Exception("Old Sequence already registered!");
            }
            this.oldSequencer = constructor;
        }

        public void RegisterSequencer(ISequence2VM sequencer) {
            if (this.sequencer != null) {
                throw new Exception("Sequencer already registered!");
            }
            this.sequencer = sequencer;
        }

        public void AddTargetToOldSequencer(DeepSkyObject deepSkyObject) {
            oldSequencer.AddTarget(deepSkyObject);
        }

        public void AddTargetToSequencer(IDeepSkyObjectContainer container) {
            sequencer.AddTarget(container);
        }

        public void SetRootContainer(ISequenceRootContainer container) {
            sequencer.Sequencer.MainContainer = container;
        }

        public SequencerFactory GetFactory() {
            return sequencer.SequencerFactory;
        }

        public IList<IDeepSkyObjectContainer> GetDeepSkyObjectContainerTemplates() {
            return sequencer.GetDeepSkyObjectContainerTemplates();
        }
    }
}