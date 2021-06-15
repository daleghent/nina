using NINA.Core.Utility;
using NINA.Plugin.Interfaces;
using NINA.ViewModel.Plugins;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NINA.Interfaces {

    public interface IPluginsVM {
        IList<ExtendedPluginManifest> AvailablePlugins { get; set; }
        IDictionary<IPluginManifest, bool> InstalledPlugins { get; set; }
        IAsyncCommand FetchPluginsCommand { get; }
        IAsyncCommand InstallPluginCommand { get; }
        IAsyncCommand UninstallPluginCommand { get; }
        ExtendedPluginManifest SelectedAvailablePlugin { get; set; }
        KeyValuePair<IPluginManifest, bool> SelectedInstalledPlugin { get; set; }
    }
}