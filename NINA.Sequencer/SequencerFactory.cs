#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Plugin;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Utility.DateTimeProvider;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace NINA.Sequencer {

    public class SequencerFactory : ISequencerFactory {
        public IList<ISequenceItem> Items { get; private set; }
        public IList<ISequenceCondition> Conditions { get; private set; }
        public IList<ISequenceTrigger> Triggers { get; private set; }
        public IList<ISequenceContainer> Container { get; private set; }
        public IList<IDateTimeProvider> DateTimeProviders { get; private set; }

        public SequencerFactory(
                IPluginProvider pluginProvider
        ) {
            Task.Run(async () => {
                await pluginProvider.Load();

                DateTimeProviders = new List<IDateTimeProvider>(pluginProvider.DateTimeProviders);
                Items = new ObservableCollection<ISequenceItem>(pluginProvider.Items);
                Conditions = new ObservableCollection<ISequenceCondition>(pluginProvider.Conditions);
                Triggers = new ObservableCollection<ISequenceTrigger>(pluginProvider.Triggers);
                Container = new ObservableCollection<ISequenceContainer>(pluginProvider.Container);

                var instructions = new List<ISequenceEntity>();
                foreach (var item in Items) {
                    instructions.Add(item);
                }
                foreach (var condition in Conditions) {
                    instructions.Add(condition);
                }
                foreach (var trigger in Triggers) {
                    instructions.Add(trigger);
                }

                ItemsView = CollectionViewSource.GetDefaultView(instructions);
                ItemsView.GroupDescriptions.Add(new PropertyGroupDescription("Category"));
                ItemsView.SortDescriptions.Add(new SortDescription("Category", ListSortDirection.Ascending));
                ItemsView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
                ItemsView.Filter += new Predicate<object>(ApplyViewFilter);

                Initialized = true;
            });
        }

        private object lockObj = new object();

        private bool initialized;

        public bool Initialized {
            get {
                lock (lockObj) {
                    return initialized;
                }
            }
            set {
                lock (lockObj) {
                    initialized = value;
                }
            }
        }

        private bool ApplyViewFilter(object obj) {
            return (obj as ISequenceEntity).Name.IndexOf(ViewFilter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private string viewFilter = string.Empty;

        public string ViewFilter {
            get => viewFilter;
            set {
                viewFilter = value;
                ItemsView.Refresh();
            }
        }

        public ICollectionView ItemsView { get; set; }

        public T GetContainer<T>() where T : ISequenceContainer {
            return (T)Container.FirstOrDefault(x => x.GetType() == typeof(T)).Clone();
        }

        public T GetItem<T>() where T : ISequenceItem {
            return (T)Items.FirstOrDefault(x => x.GetType() == typeof(T)).Clone();
        }

        public T GetCondition<T>() where T : ISequenceCondition {
            return (T)Conditions.FirstOrDefault(x => x.GetType() == typeof(T)).Clone();
        }

        public T GetTrigger<T>() where T : ISequenceTrigger {
            return (T)Triggers.FirstOrDefault(x => x.GetType() == typeof(T)).Clone();
        }
    }
}