#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using NINA.Sequencer;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NINA.Utility;
using NINA.Profile;
using NINA.Utility.Mediator.Interfaces;
using NINA.Model.MyPlanetarium;
using NINA.Utility.Astrometry;
using NINA.ViewModel.ImageHistory;
using NINA.ViewModel;
using NINA.ViewModel.FramingAssistant;
using NINA.Sequencer.Utility.DateTimeProvider;
using System.ComponentModel.Composition.Primitives;

namespace NINA.Plugin {

    public class PluginProvider : IPluginProvider {

        public PluginProvider(IProfileService profileService,
                              ICameraMediator cameraMediator,
                              ITelescopeMediator telescopeMediator,
                              IFocuserMediator focuserMediator,
                              IFilterWheelMediator filterWheelMediator,
                              IGuiderMediator guiderMediator,
                              IRotatorMediator rotatorMediator,
                              IFlatDeviceMediator flatDeviceMediator,
                              IWeatherDataMediator weatherDataMediator,
                              IImagingMediator imagingMediator,
                              IApplicationStatusMediator applicationStatusMediator,
                              INighttimeCalculator nighttimeCalculator,
                              IPlanetariumFactory planetariumFactory,
                              IImageHistoryVM imageHistoryVM,
                              IDeepSkyObjectSearchVM deepSkyObjectSearchVM,
                              IDomeMediator domeMediator,
                              IImageSaveMediator imageSaveMediator,
                              ISwitchMediator switchMediator,
                              ISafetyMonitorMediator safetyMonitorMediator,
                              IApplicationResourceDictionary resourceDictionary,
                              IApplicationMediator applicationMediator,
                              IFramingAssistantVM framingAssistantVM) {
            this.profileService = profileService;
            this.cameraMediator = cameraMediator;
            this.telescopeMediator = telescopeMediator;
            this.focuserMediator = focuserMediator;
            this.filterWheelMediator = filterWheelMediator;
            this.guiderMediator = guiderMediator;
            this.rotatorMediator = rotatorMediator;
            this.flatDeviceMediator = flatDeviceMediator;
            this.weatherDataMediator = weatherDataMediator;
            this.imagingMediator = imagingMediator;
            this.applicationStatusMediator = applicationStatusMediator;
            this.nighttimeCalculator = nighttimeCalculator;
            this.planetariumFactory = planetariumFactory;
            this.imageHistoryVM = imageHistoryVM;
            this.deepSkyObjectSearchVM = deepSkyObjectSearchVM;
            this.domeMediator = domeMediator;
            this.imageSaveMediator = imageSaveMediator;
            this.switchMediator = switchMediator;
            this.safetyMonitorMediator = safetyMonitorMediator;
            this.resourceDictionary = resourceDictionary;
            this.applicationMediator = applicationMediator;
            this.framingAssistantVM = framingAssistantVM;

            DateTimeProviders = new List<IDateTimeProvider>() {
                new TimeProvider(),
                new SunsetProvider(nighttimeCalculator),
                new NauticalDuskProvider(nighttimeCalculator),
                new DuskProvider(nighttimeCalculator),
                new DawnProvider(nighttimeCalculator),
                new NauticalDawnProvider(nighttimeCalculator),
                new SunriseProvider(nighttimeCalculator),
                new MeridianProvider(profileService)
            };
        }

        public Task Load() {
            return Task.Run(() => {
                lock (lockobj) {
                    if (!initialized) {
                        Items = new List<ISequenceItem>();
                        Conditions = new List<ISequenceCondition>();
                        Triggers = new List<ISequenceTrigger>();
                        Container = new List<ISequenceContainer>();
                        Plugins = new List<IPlugin>();

                        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

                        /* Compose the core catalog */
                        var types = GetCoreSequencerTypes();
                        var coreCatalog = new TypeCatalog(types);

                        Compose(coreCatalog);

                        /* Compose the plugin catalog */
                        var pluginCatalog = new AggregateCatalog();
                        var coreExtensionsFolder = Path.Combine(NINA.Utility.Utility.APPLICATIONDIRECTORY, "Plugins");
                        var userExtensionsFolder = Path.Combine(NINA.Utility.Utility.APPLICATIONTEMPPATH, "Plugins");

                        var files = new List<string>();

                        if (Directory.Exists(coreExtensionsFolder)) {
                            files.AddRange(Directory.GetFiles(coreExtensionsFolder, "*.dll"));
                        }

                        if (Directory.Exists(userExtensionsFolder)) {
                            files.AddRange(Directory.GetFiles(userExtensionsFolder, "*.dll"));
                        }

                        foreach (var file in files) {
                            try {
                                var plugin = new AssemblyCatalog(file);
                                plugin.Parts.ToArray();

                                pluginCatalog.Catalogs.Add(plugin);
                                Assemblies.Add(plugin.Assembly);
                            } catch (Exception ex) {
                                Logger.Error($"Failed to load plugin {file}", ex);
                            }
                        }

                        Compose(pluginCatalog);

                        initialized = true;
                    }
                }
            });
        }

        private void Compose(ComposablePartCatalog catalog) {
            try {
                var container = GetContainer(catalog);

                container.ComposeParts(this);

                foreach (var template in DataTemplateImports) {
                    Application.Current?.Resources.MergedDictionaries.Add(template);
                }

                Items = Items.Concat(Assign(ItemImports, resourceDictionary)).ToList();
                Conditions = Conditions.Concat(Assign(ConditionImports, resourceDictionary)).ToList();
                Triggers = Triggers.Concat(Assign(TriggerImports, resourceDictionary)).ToList();
                Container = Container.Concat(Assign(ContainerImports, resourceDictionary)).ToList();
                Plugins = Plugins.Concat(PluginImports).ToList();
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        private CompositionContainer GetContainer(ComposablePartCatalog catalog) {
            var container = new CompositionContainer(catalog);
            container.ComposeExportedValue(profileService);
            container.ComposeExportedValue(cameraMediator);
            container.ComposeExportedValue(telescopeMediator);
            container.ComposeExportedValue(focuserMediator);
            container.ComposeExportedValue(filterWheelMediator);
            container.ComposeExportedValue(guiderMediator);
            container.ComposeExportedValue(rotatorMediator);
            container.ComposeExportedValue(flatDeviceMediator);
            container.ComposeExportedValue(weatherDataMediator);
            container.ComposeExportedValue(imagingMediator);
            container.ComposeExportedValue(applicationStatusMediator);
            container.ComposeExportedValue(nighttimeCalculator);
            container.ComposeExportedValue(planetariumFactory);
            container.ComposeExportedValue(imageHistoryVM);
            container.ComposeExportedValue(deepSkyObjectSearchVM);
            container.ComposeExportedValue(domeMediator);
            container.ComposeExportedValue(imageSaveMediator);
            container.ComposeExportedValue(switchMediator);
            container.ComposeExportedValue(resourceDictionary);
            container.ComposeExportedValue(DateTimeProviders);
            container.ComposeExportedValue(safetyMonitorMediator);
            container.ComposeExportedValue(applicationMediator);
            container.ComposeExportedValue(framingAssistantVM);

            return container;
        }

        private object lockobj = new object();
        private bool initialized = false;
        public IList<IDateTimeProvider> DateTimeProviders { get; }
        public IList<ISequenceItem> Items { get; private set; }
        public IList<ISequenceCondition> Conditions { get; private set; }
        public IList<ISequenceTrigger> Triggers { get; private set; }
        public IList<ISequenceContainer> Container { get; private set; }
        public IList<IPlugin> Plugins { get; private set; }
        public IList<Assembly> Assemblies { get; private set; } = new List<Assembly>();

        [ImportMany(typeof(ISequenceItem))]
        public IEnumerable<Lazy<ISequenceItem, Dictionary<string, object>>> ItemImports { get; private set; }

        [ImportMany(typeof(ISequenceCondition))]
        public IEnumerable<Lazy<ISequenceCondition, Dictionary<string, object>>> ConditionImports { get; private set; }

        [ImportMany(typeof(ISequenceTrigger))]
        public IEnumerable<Lazy<ISequenceTrigger, Dictionary<string, object>>> TriggerImports { get; private set; }

        [ImportMany(typeof(ISequenceContainer))]
        public IEnumerable<Lazy<ISequenceContainer, Dictionary<string, object>>> ContainerImports { get; private set; }

        [ImportMany(typeof(ResourceDictionary))]
        public IEnumerable<ResourceDictionary> DataTemplateImports { get; private set; }

        [ImportMany(typeof(IPlugin))]
        public IEnumerable<IPlugin> PluginImports { get; private set; }

        private readonly IProfileService profileService;
        private readonly ICameraMediator cameraMediator;
        private readonly ITelescopeMediator telescopeMediator;
        private readonly IFocuserMediator focuserMediator;
        private readonly IFilterWheelMediator filterWheelMediator;
        private readonly IGuiderMediator guiderMediator;
        private readonly IRotatorMediator rotatorMediator;
        private readonly IFlatDeviceMediator flatDeviceMediator;
        private readonly IWeatherDataMediator weatherDataMediator;
        private readonly IImagingMediator imagingMediator;
        private readonly IApplicationStatusMediator applicationStatusMediator;
        private readonly INighttimeCalculator nighttimeCalculator;
        private readonly IPlanetariumFactory planetariumFactory;
        private readonly IImageHistoryVM imageHistoryVM;
        private readonly IDeepSkyObjectSearchVM deepSkyObjectSearchVM;
        private readonly IDomeMediator domeMediator;
        private readonly IImageSaveMediator imageSaveMediator;
        private readonly ISwitchMediator switchMediator;
        private readonly ISafetyMonitorMediator safetyMonitorMediator;
        private readonly IApplicationResourceDictionary resourceDictionary;
        private readonly IApplicationMediator applicationMediator;
        private readonly IFramingAssistantVM framingAssistantVM;

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args) {
            return this.Assemblies.FirstOrDefault(x => x.GetName().Name == args.Name);
        }

        /// <summary>
        /// This returns a list of types in the NINA.Sequencer namespace to load the core plugins
        /// Furthermore this safeguards against the ASCOM assembly that is not required to load when the platform is not installed
        /// </summary>
        /// <returns></returns>
        private static IEnumerable<Type> GetCoreSequencerTypes() {
            IEnumerable<Type> loadableTypes;
            try {
                loadableTypes = System.Reflection.Assembly.GetExecutingAssembly().GetTypes();
            } catch (ReflectionTypeLoadException e) {
                loadableTypes = e.Types.Where(t => t != null);
            }

            List<Type> sequencerTypes = new List<Type>();
            foreach (Type t in loadableTypes) {
                try {
                    if (t.IsClass && t.Namespace?.StartsWith("NINA.Sequencer") == true) {
                        sequencerTypes.Add(t);
                    }
                } catch (Exception) {
                }
            }
            return sequencerTypes;
        }

        private IOrderedEnumerable<T> Assign<T>(IEnumerable<Lazy<T, Dictionary<string, object>>> imports, IApplicationResourceDictionary resourceDictionary) where T : ISequenceEntity {
            var items = new List<T>();
            foreach (var importItem in imports) {
                var item = importItem.Value;
                if (importItem.Metadata.TryGetValue("Name", out var nameObj)) {
                    string name = nameObj.ToString();
                    item.Name = GrabLabel(name);
                }
                if (importItem.Metadata.TryGetValue("Description", out var descriptionObj)) {
                    string description = descriptionObj.ToString();
                    item.Description = GrabLabel(description);
                }
                if (importItem.Metadata.TryGetValue("Icon", out var iconObj)) {
                    string icon = iconObj.ToString();
                    item.Icon = (System.Windows.Media.GeometryGroup)resourceDictionary[icon];
                }
                if (importItem.Metadata.TryGetValue("Category", out var categoryObj)) {
                    string category = categoryObj.ToString();
                    item.Category = GrabLabel(category);
                }
                items.Add(item);
            }
            return items.OrderBy(item => item.Category + item.Name);
        }

        private string GrabLabel(string label) {
            if (label.StartsWith("Lbl_")) {
                return Locale.Loc.Instance[label];
            } else {
                return label;
            }
        }
    }
}