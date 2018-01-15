using System.Configuration;

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
    }
}
