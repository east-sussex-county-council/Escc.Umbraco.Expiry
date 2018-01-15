using System.Configuration;

namespace Escc.Umbraco.Expiry.Configuration
{
    public class UnpublishOverridesSection : ConfigurationSection
    {
        /// <summary>
        /// The name of this section in the app.config.
        /// </summary>
        public const string SectionName = "UnpublishOverridesSection";

        private const string ContentTypesCollectionName = "ContentTypes";
        private const string PathsCollectionName = "Paths";

        [ConfigurationProperty("enabled", DefaultValue = "true", IsRequired = false)]
        public bool Enabled
        {
            get { return (bool)this["enabled"]; }
            set { this["enabled"] = value; }
        }

        [ConfigurationProperty(ContentTypesCollectionName)]
        [ConfigurationCollection(typeof(UnpublishOverridesContentTypesCollection), AddItemName = "add")]
        public UnpublishOverridesContentTypesCollection ContentTypes { get { return (UnpublishOverridesContentTypesCollection)base[ContentTypesCollectionName]; } }

        [ConfigurationProperty(PathsCollectionName)]
        [ConfigurationCollection(typeof(UnpublishOverridesPathsCollection), AddItemName = "add")]
        public UnpublishOverridesPathsCollection Paths { get { return (UnpublishOverridesPathsCollection)base[PathsCollectionName]; } }
    }
}