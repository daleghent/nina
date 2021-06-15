using NINA.Plugin.ManifestDefinition;

namespace NINA.Plugin.Interfaces {

    public interface IPluginInstallerDetails {

        /// <summary>
        /// The url where the plugin can be downloaded.
        /// </summary>
        string URL { get; }

        /// <summary>
        /// The type of installer, for the application to determine how the plugin can be installed.
        /// </summary>
        InstallerType Type { get; }

        /// <summary>
        /// The checksum of the installer file.
        /// </summary>
        string Checksum { get; }

        /// <summary>
        /// The type of the checksum.
        /// </summary>
        InstallerChecksum ChecksumType { get; }
    }
}