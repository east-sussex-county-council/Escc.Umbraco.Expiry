using System;
using System.Configuration;
using System.Globalization;

namespace Escc.Umbraco.Expiry.Configuration
{
    public class UnpublishOverridesPathElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }

        [ConfigurationProperty("children", IsRequired = true)]
        public string Children
        {
            get { return (string)this["children"]; }
            set { this["children"] = value; }
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
