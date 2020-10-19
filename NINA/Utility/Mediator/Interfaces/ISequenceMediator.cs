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
using NINA.ViewModel.Interfaces;
using NINA.ViewModel.Sequencer;
using System.Collections.Generic;

namespace NINA.Utility.Mediator.Interfaces {

    public interface ISequenceMediator {

        void AddTargetToOldSequencer(DeepSkyObject deepSkyObject);

        void AddTargetToSequencer(IDeepSkyObjectContainer container);

        void RegisterConstructor(ISequenceVM constructor);

        void RegisterSequencer(ISequence2VM sequencer);

        void SetRootContainer(ISequenceRootContainer container);

        SequencerFactory GetFactory();
        IList<IDeepSkyObjectContainer> GetDeepSkyObjectContainerTemplates();
    }
}