#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Utility.DateTimeProvider;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace NINA.Sequencer {

    public interface ISequencerFactory {
        IEnumerable<Lazy<ISequenceCondition, Dictionary<string, object>>> ConditionImports { get; }
        IList<ISequenceCondition> Conditions { get; }
        IList<ISequenceContainer> Container { get; }
        IEnumerable<Lazy<ISequenceContainer, Dictionary<string, object>>> ContainerImports { get; }
        IEnumerable<ResourceDictionary> DataTemplateImports { get; }
        IList<IDateTimeProvider> DateTimeProviders { get; }
        IEnumerable<Lazy<ISequenceItem, Dictionary<string, object>>> ItemImports { get; }
        IList<ISequenceItem> Items { get; }
        ICollectionView ItemsView { get; set; }
        IEnumerable<Lazy<ISequenceTrigger, Dictionary<string, object>>> TriggerImports { get; }
        IList<ISequenceTrigger> Triggers { get; }
        string ViewFilter { get; set; }

        T GetCondition<T>() where T : ISequenceCondition;

        T GetContainer<T>() where T : ISequenceContainer;

        T GetItem<T>() where T : ISequenceItem;

        T GetTrigger<T>() where T : ISequenceTrigger;
    }
}