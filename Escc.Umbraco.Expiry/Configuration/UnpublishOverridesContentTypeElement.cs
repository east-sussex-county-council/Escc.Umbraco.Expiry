using System.Configuration;

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
    }
}