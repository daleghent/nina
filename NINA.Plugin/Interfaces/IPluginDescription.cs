namespace NINA.Plugin.Interfaces {

    public interface IPluginDescription {

        /// <summary>
        /// A short summary of the plugin capabilities.
        /// </summary>
        string ShortDescription { get; }

        /// <summary>
        /// A more in-depth description of the plugin with all of its capabilities explained in detail.
        /// </summary>
        string LongDescription { get; }

        /// <summary>
        /// The most relevant image for the plugin that should be prominently displayed.
        /// </summary>
        string FeaturedImageURL { get; }

        /// <summary>
        /// An example image of using the plugin.
        /// </summary>
        string ScreenshotURL { get; }

        /// <summary>
        /// An alternative image of using the plugin.
        /// </summary>
        string AltScreenshotURL { get; }
    }
}