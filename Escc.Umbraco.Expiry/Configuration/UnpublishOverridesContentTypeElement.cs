using System;
using System.Configuration;
using System.Globalization;

namespace Escc.Umbraco.Expiry.Configuration
{
    public class UnpublishOverridesContentTypeElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }

        [ConfigurationProperty("level", IsRequired = true)]
        public string Level
        {
            get { return (string)this["level"]; }
            set { this["level"] = value; }
        }

        /// <summary>
        /// Gets the maximum number of days (in addition to <see cref="MaximumExpiryMonths"/>) pages are allowed to be published before they expire.
        /// </summary>
        [ConfigurationProperty("expiryDays", IsRequired = false)]
        public int? MaximumExpiryDays
        {
            get {
                if (this["expiryDays"] == null) return null;
                return Int32.Parse(this["expiryDays"].ToString(), CultureInfo.InvariantCulture);
            }
            set { this["expiryDays"] = value; }
        }

        /// <summary>
        /// Gets the maximum number of months (in addition to <see cref="MaximumExpiryDays"/>) pages are allowed to be published before they expire.
        /// </summary>
        [ConfigurationProperty("expiryMonths", IsRequired = false)]
        public int? MaximumExpiryMonths
        {
            get {
                if (this["expiryMonths"] == null) return null;
                return Int32.Parse(this["expiryMonths"].ToString(), CultureInfo.InvariantCulture);
            }
            set { this["expiryMonths"] = value; }
        }
    }
}