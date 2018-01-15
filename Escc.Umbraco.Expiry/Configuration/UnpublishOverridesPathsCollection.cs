using System.Configuration;

namespace Escc.Umbraco.Expiry.Configuration
{
    public class UnpublishOverridesPathsCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new UnpublishOverridesPathElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((UnpublishOverridesPathElement)element).Name;
        }
    }
}