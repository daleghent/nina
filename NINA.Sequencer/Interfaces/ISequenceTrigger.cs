#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Model;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.Trigger {

    public interface ISequenceTrigger : ISequenceEntity, ISequenceHasChanged {

        /// <summary>
        /// After each Sequence Item was processed this method will be called to determin if the trigger should be executed
        /// </summary>
        /// <param name="nextItem"></param>
        /// <returns></returns>
        bool ShouldTrigger(ISequenceItem nextItem);

        /// <summary>
        /// When the sequencer is started this method is called
        /// </summary>
        void Initialize();

        /// <summary>
        /// When the sequencer is stopped or canceled this method is called
        /// </summary>
        void Teardown();

        /// <summary>
        /// When a sequence container is entered this method is called
        /// </summary>
        void SequenceBlockInitialize();

        /// <summary>
        /// When a sequence container is finished this method is called
        /// </summary>
        void SequenceBlockTeardown();

        /// <summary>
        /// Each time the sequence container starts a loop this is called
        /// </summary>
        void SequenceBlockStarted();

        /// <summary>
        /// Each time the sequence container finishes a loop this is called
        /// </summary>
        void SequenceBlockFinished();

        /// <summary>
        /// Runs the actual trigger logic
        /// </summary>
        /// <param name="context">The container of the next sequence item to be processed. As triggers get called in a cascade the item might be in a child container.</param>
        /// <param name="progress"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task Run(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token);
    }
}