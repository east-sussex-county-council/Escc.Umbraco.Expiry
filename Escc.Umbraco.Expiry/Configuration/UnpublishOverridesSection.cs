using System;
using System.Configuration;
using System.Globalization;

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

        [ConfigurationProperty("defaultExpiryDays", DefaultValue = 0, IsRequired = false)]
        public int MaximumExpiryDays
        {
            get { return Int32.Parse(this["defaultExpiryDays"].ToString(), CultureInfo.InvariantCulture); }
            set { this["defaultExpiryDays"] = value; }
        }

        [ConfigurationProperty("defaultExpiryMonths", DefaultValue = 6, IsRequired = false)]
        public int MaximumExpiryMonths
        {
            get { return Int32.Parse(this["defaultExpiryMonths"].ToString(), CultureInfo.InvariantCulture); }
            set { this["defaultExpiryMonths"] = value; }
        }

        [ConfigurationProperty(ContentTypesCollectionName)]
        [ConfigurationCollection(typeof(UnpublishOverridesContentTypesCollection), AddItemName = "add")]
        public UnpublishOverridesContentTypesCollection ContentTypes { get { return (UnpublishOverridesContentTypesCollection)base[ContentTypesCollectionName]; } }

        [ConfigurationProperty(PathsCollectionName)]
        [ConfigurationCollection(typeof(UnpublishOverridesPathsCollection), AddItemName = "add")]
        public UnpublishOverridesPathsCollection Paths { get { return (UnpublishOverridesPathsCollection)base[PathsCollectionName]; } }
    }
}