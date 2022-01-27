#region "copyright"

/*
    Copyright © 2016 - 2022 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Model;
using NINA.Sequencer.Container;
using NINA.Equipment.Interfaces.Mediator;
using NINA.ViewModel.Sequencer;
using System;
using System.Collections.Generic;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.Astrometry;

namespace NINA.Sequencer.Mediator {

    // This mediator can only be used after it is initialized to prevent cyclic event issues
    public class SequenceMediator : ISequenceMediator {
        private ISequenceNavigationVM sequenceNavigation;
        private readonly Exception SequenceMediatorException = new Exception("SequencerNavigation not initialized yet!");

        public void RegisterSequenceNavigation(ISequenceNavigationVM sequenceNavigation) {
            if (this.sequenceNavigation != null) {
                throw new Exception("Sequencer already registered!");
            }
            this.sequenceNavigation = sequenceNavigation;
        }

        public bool Initialized => sequenceNavigation.Initialized;

        public void AddSimpleTarget(DeepSkyObject deepSkyObject) {
            if (Initialized) {
                sequenceNavigation.AddSimpleTarget(deepSkyObject);
            } else {
                throw SequenceMediatorException;
            }
        }

        public void AddAdvancedTarget(IDeepSkyObjectContainer container) {
            if (Initialized) {
                sequenceNavigation.AddAdvancedTarget(container);
            } else {
                throw SequenceMediatorException;
            }
        }

        public void SetAdvancedSequence(ISequenceRootContainer container) {
            if (Initialized) {
                sequenceNavigation.SetAdvancedSequence(container);
            } else {
                throw SequenceMediatorException;
            }
        }

        public IList<IDeepSkyObjectContainer> GetDeepSkyObjectContainerTemplates() {
            if (Initialized) {
                return sequenceNavigation.GetDeepSkyObjectContainerTemplates();
            } else {
                throw SequenceMediatorException;
            }
        }

        public void SwitchToAdvancedView() {
            if (Initialized) {
                sequenceNavigation.SwitchToAdvancedView();
            } else {
                throw SequenceMediatorException;
            }
        }

        public void SwitchToOverview() {
            if (Initialized) {
                sequenceNavigation.SwitchToOverview();
            } else {
                throw SequenceMediatorException;
            }
        }

        public void AddTargetToTargetList(IDeepSkyObjectContainer container) {
            if (Initialized) {
                sequenceNavigation.AddTargetToTargetList(container);
            } else {
                throw SequenceMediatorException;
            }
        }

        public IList<IDeepSkyObjectContainer> GetAllTargetsInAdvancedSequence() {
            if (Initialized) {
                return sequenceNavigation.GetAllTargetsInAdvancedSequence();
            } else {
                throw SequenceMediatorException;
            }
        }

        public IList<IDeepSkyObjectContainer> GetAllTargetsInSimpleSequence() {
            if (Initialized) {
                return sequenceNavigation.GetAllTargetsInSimpleSequence();
            } else {
                throw SequenceMediatorException;
            }
        }
    }
}