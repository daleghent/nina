#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry.Interfaces;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.PlateSolving.Interfaces;
using NINA.Plugin.Interfaces;
using NINA.Plugin.ManifestDefinition;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Utility.DateTimeProvider;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using Trinet.Core.IO.Ntfs;

namespace NINA.Plugin {

    public class PluginLoader : IPluginLoader {

        public PluginLoader(IProfileService profileService,
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
                              IFramingAssistantVM framingAssistantVM,
                              IPlateSolverFactory plateSolverFactory,
                              IWindowServiceFactory windowServiceFactory) {
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
            this.platesolverFactory = plateSolverFactory;
            this.windowServiceFactory = windowServiceFactory;

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

        private void DeployFromStaging() {
            var staging = Constants.StagingFolder;
            var destination = Constants.UserExtensionsFolder;

            if (Directory.Exists(staging)) {
                try {
                    var sourcePath = staging.TrimEnd('\\', ' ');
                    var targetPath = destination.TrimEnd('\\', ' ');
                    var files = Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories)
                                         .GroupBy(s => Path.GetDirectoryName(s));
                    foreach (var folder in files) {
                        try {
                            var targetFolder = folder.Key.Replace(sourcePath, targetPath);
                            Directory.CreateDirectory(targetFolder);
                            foreach (var file in folder) {
                                var targetFile = Path.Combine(targetFolder, Path.GetFileName(file));
                                if (File.Exists(targetFile)) {
                                    File.Delete(targetFile);
                                }

                                File.Move(file, targetFile);
                            }
                        } catch (Exception ex) {
                            Logger.Error("Failed to deploy plugin file to destination", ex);
                        }
                    }
                    Directory.Delete(staging, true);
                } catch (Exception ex) {
                    Logger.Error("Pluging deployment from staging failed", ex);
                }
            }
        }

        private void CleanupEmptyFolders() {
            try {
                if (Directory.Exists(Constants.UserExtensionsFolder)) {
                    foreach (var dir in Directory.GetDirectories(Constants.UserExtensionsFolder)) {
                        if (!Directory.EnumerateFileSystemEntries(dir, "*.*", SearchOption.AllDirectories).Any()) {
                            Directory.Delete(dir);
                        }
                    }
                }
            } catch (Exception ex) {
                Logger.Error($"Error occured on plugin folder cleanup", ex);
            }
        }

        private void DeleteFromDeletion() {
            try {
                if (Directory.Exists(Constants.DeletionFolder)) {
                    Directory.Delete(Constants.DeletionFolder, true);
                }
            } catch (Exception ex) {
                Logger.Error("Plugin deletion from deletion folder failed", ex);
            }
        }

        public Task Load() {
            return Task.Run(() => {
                lock (lockobj) {
                    if (!initialized) {
                        Stopwatch sw = Stopwatch.StartNew();

                        //Check for pending plugin updates and deploy them
                        CleanupEmptyFolders();
                        DeployFromStaging();
                        DeleteFromDeletion();

                        Items = new List<ISequenceItem>();
                        Conditions = new List<ISequenceCondition>();
                        Triggers = new List<ISequenceTrigger>();
                        Container = new List<ISequenceContainer>();
                        DockableVMs = new List<IDockableVM>();
                        Plugins = new Dictionary<IPluginManifest, bool>();

                        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

                        /* Compose the core catalog */
                        var types = GetCoreSequencerTypes();
                        var coreCatalog = new TypeCatalog(types);

                        Compose(coreCatalog);

                        /* Compose the plugin catalog */

                        var files = new List<string>();

                        if (Directory.Exists(Constants.CoreExtensionsFolder)) {
                            files.AddRange(Directory.GetFiles(Constants.CoreExtensionsFolder, "*.dll"));
                        }

                        foreach (var file in files) {
                            LoadPlugin(file);
                        }

                        if (Directory.Exists(Constants.UserExtensionsFolder)) {
                            files.AddRange(Directory.GetFiles(Constants.UserExtensionsFolder, "*.dll", SearchOption.AllDirectories));
                        }

                        foreach (var file in files) {
                            LoadPlugin(file);
                        }

                        initialized = true;
                        Debug.Print($"Time to load all plugins {sw.Elapsed}");
                    }
                }
            });
        }

        private void LoadPlugin(string file) {
            Stopwatch sw = Stopwatch.StartNew();
            try {
                FileInfo fileInfo = new FileInfo(file);
                if (fileInfo.AlternateDataStreamExists("Zone.Identifier")) {
                    fileInfo.DeleteAlternateDataStream("Zone.Identifier");
                }

                var assembly = Assembly.LoadFrom(file);
                var plugin = new AssemblyCatalog(assembly);

                var references = plugin.Assembly.GetReferencedAssemblies();
                if (references.FirstOrDefault(x => x.FullName.Contains("NINA")) != null) {
                    try {
                        var manifestImport = new ManifestImport();
                        var container = GetContainer(plugin);
                        container.ComposeParts(manifestImport);

                        var manifest = manifestImport.PluginManifestImport;

                        try {
                            Compose(plugin);
                            Plugins[manifest] = true;
                            //Add the loaded plugin assembly to the assembly resolver
                            Assemblies.Add(plugin.Assembly);
                            Logger.Info($"Successfully loaded plugin {manifest.Name} version {manifest.Version}");
                        } catch (Exception ex) {
                            //Manifest ok - plugin composition failed
                            var failedManifest = new PluginManifest {
                                Author = manifest.Author,
                                Identifier = file,
                                Name = manifest.Name,
                                Version = manifest.Version,
                                Descriptions = new PluginDescription {
                                    ShortDescription = $"Failed to load {file}",
                                    LongDescription = ex.Message
                                }
                            };
                            Logger.Error($"Failed to load plugin at {file} - {failedManifest.Name} version {failedManifest.Version}", ex);
                            Plugins[failedManifest] = false;
                        }
                    } catch (Exception ex) {
                        var message = ex.Message;
                        if (ex is ReflectionTypeLoadException typeLoadException) {
                            var loaderExceptions = typeLoadException.LoaderExceptions;
                            message = string.Join(Environment.NewLine, loaderExceptions.ToList());
                        }

                        var reflectionAssembly = Assembly.ReflectionOnlyLoadFrom(file);

                        var attr = CustomAttributeData.GetCustomAttributes(reflectionAssembly);
                        var id = attr.First(x => x.AttributeType == typeof(GuidAttribute)).ConstructorArguments.First().Value.ToString();
                        var author = attr.FirstOrDefault(x => x.AttributeType == typeof(AssemblyCompanyAttribute))?.ConstructorArguments.FirstOrDefault().Value.ToString() ?? string.Empty;
                        var version = new PluginVersion(attr.FirstOrDefault(x => x.AttributeType == typeof(AssemblyFileVersionAttribute))?.ConstructorArguments.First().Value.ToString() ?? "1.0.0.0");
                        var name = attr.FirstOrDefault(x => x.AttributeType == typeof(AssemblyTitleAttribute))?.ConstructorArguments.First().Value.ToString() ?? string.Empty;

                        //Manifest failed - Create a fake manifest using all available file meta info
                        var fvi = FileVersionInfo.GetVersionInfo(file);
                        var fileVersion = new Version(fvi.FileVersion);
                        var failedManifest = new PluginManifest {
                            Author = fvi.CompanyName,
                            Identifier = id,
                            Name = name,
                            Version = version,
                            Descriptions = new PluginDescription {
                                ShortDescription = $"Failed to load {file}",
                                LongDescription = message
                            }
                        };

                        Plugins[failedManifest] = false;
                        Logger.Error($"Failed to load plugin at {file} - {failedManifest.Name} version {failedManifest.Version} {message}");
                    }
                }
            } catch (Exception ex) {
                //This should only happen for non plugin assemblies, that are not even targeting .NET
                Logger.Trace($"The dll inside the plugins folder failed to load. Most likely it is not a pugin but an external non .NET dependency. File: {file}, Error: {ex}");
            } finally {
                Debug.Print($"Time to load plugin {Path.GetFileNameWithoutExtension(file)} {sw.Elapsed}");
            }
        }

        private void Compose(ComposablePartCatalog catalog) {
            try {
                var container = GetContainer(catalog);
                var parts = new PartsImport();
                container.ComposeParts(parts);

                foreach (var template in parts.DataTemplateImports) {
                    Application.Current?.Resources.MergedDictionaries.Add(template);
                }

                Items = Items.Concat(AssignSequenceEntity(parts.ItemImports, resourceDictionary)).ToList();
                Conditions = Conditions.Concat(AssignSequenceEntity(parts.ConditionImports, resourceDictionary)).ToList();
                Triggers = Triggers.Concat(AssignSequenceEntity(parts.TriggerImports, resourceDictionary)).ToList();
                Container = Container.Concat(AssignSequenceEntity(parts.ContainerImports, resourceDictionary)).ToList();

                DockableVMs = DockableVMs.Concat(parts.DockableVMImports).ToList();
            } catch (Exception ex) {
                Logger.Error(ex);
                throw;
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
            container.ComposeExportedValue(platesolverFactory);
            container.ComposeExportedValue(windowServiceFactory);

            return container;
        }

        private object lockobj = new object();
        private bool initialized = false;
        public IList<IDateTimeProvider> DateTimeProviders { get; }
        public IList<ISequenceItem> Items { get; private set; }
        public IList<ISequenceCondition> Conditions { get; private set; }
        public IList<ISequenceTrigger> Triggers { get; private set; }
        public IList<ISequenceContainer> Container { get; private set; }
        public IList<IDockableVM> DockableVMs { get; private set; }
        public IDictionary<IPluginManifest, bool> Plugins { get; private set; }
        public IList<Assembly> Assemblies { get; private set; } = new List<Assembly>();

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
        private readonly IPlateSolverFactory platesolverFactory;
        private readonly IWindowServiceFactory windowServiceFactory;

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
                loadableTypes = Assembly.GetAssembly(typeof(ISequenceItem)).GetTypes();
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

        private IOrderedEnumerable<T> AssignSequenceEntity<T>(IEnumerable<Lazy<T, Dictionary<string, object>>> imports, IApplicationResourceDictionary resourceDictionary) where T : ISequenceEntity {
            var items = new List<T>();
            foreach (var importItem in imports) {
                try {
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
                } catch (Exception ex) {
                    // Skip item if anything fails
                    Logger.Error("Plugin Item failed to load ", ex);
                }
            }
            return items.OrderBy(item => item.Category + item.Name);
        }

        private string GrabLabel(string label) {
            if (label.StartsWith("Lbl_")) {
                return Loc.Instance[label];
            } else {
                return label;
            }
        }
    }

    public class ManifestImport {

        [Import(typeof(IPluginManifest))]
        public IPluginManifest PluginManifestImport { get; private set; }
    }

    public class PartsImport {

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

        [ImportMany(typeof(IDockableVM))]
        public IEnumerable<IDockableVM> DockableVMImports { get; private set; }
    }
}