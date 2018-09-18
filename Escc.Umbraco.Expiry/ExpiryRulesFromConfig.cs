using Escc.Umbraco.Expiry.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Web;
using Umbraco.Web.Models.ContentEditing;
using Umbraco.Web.Security;

namespace Escc.Umbraco.Expiry
{
    /// <summary>
    /// Reads expiry rules from web.config
    /// </summary>
    /// <seealso cref="Escc.Umbraco.Expiry.IExpiryRuleProvider" />
    public class ExpiryRulesFromConfig : IExpiryRuleProvider
    {
        public IList<DocumentTypeExpiryRule> DocumentTypeRules { get; private set; } = new List<DocumentTypeExpiryRule>();

        public IList<PathExpiryRule> PathRules { get; private set; } = new List<PathExpiryRule>();

        public bool IsEnabled { get { return CheckEnabled(); } }

        /// <summary>
        /// Initializes the <see cref="ExpiryRules"/> class.
        /// </summary>
        public ExpiryRulesFromConfig()
        {
            GetContentTypes();
            GetPaths();
        }

        private void GetContentTypes()
        {
            var unpublishOverridesSection = ConfigurationManager.GetSection(UnpublishOverridesSection.SectionName) as UnpublishOverridesSection;
            if (unpublishOverridesSection != null)
            {
                foreach (UnpublishOverridesContentTypeElement templateElement in unpublishOverridesSection.ContentTypes)
                {
                    var template = new UnpublishOverridesContentTypeElement { Name = templateElement.Name, Level = templateElement.Level };
                    AddDocumentType(template);
                }
            }
        }

        private void AddDocumentType(UnpublishOverridesContentTypeElement documentTypeElement)
        {
            if (documentTypeElement == null) return;

            var rule = new DocumentTypeExpiryRule() { Alias = documentTypeElement.Name, Level = documentTypeElement.Level };

            if (DocumentTypeRules.FirstOrDefault(existingRule => existingRule.Alias == rule.Alias && existingRule.Level == rule.Level) == null)
            {
                DocumentTypeRules.Add(rule);
            }
        }

        private void GetPaths()
        {
            var unpublishOverridesSection = ConfigurationManager.GetSection(UnpublishOverridesSection.SectionName) as UnpublishOverridesSection;
            if (unpublishOverridesSection != null)
            {
                foreach (UnpublishOverridesPathElement pathElement in unpublishOverridesSection.Paths)
                {
                    var path = new UnpublishOverridesPathElement
                    {
                        Name = pathElement.Name.ToLower(),
                        Children = pathElement.Children
                    };
                    AddPath(path);
                }
            }
        }

        private void AddPath(UnpublishOverridesPathElement pathElement)
        {
            if (pathElement == null) return;

            var rule = new PathExpiryRule() { Path = pathElement.Name, Children = pathElement.Children };

            if (PathRules.FirstOrDefault(existingRule => existingRule.Path == rule.Path && existingRule.Children == rule.Children) == null)
            {
                PathRules.Add(rule);
            }
        }

        private static bool CheckEnabled()
        {
            var unpublishOverridesSection = ConfigurationManager.GetSection(UnpublishOverridesSection.SectionName) as UnpublishOverridesSection;

            if (unpublishOverridesSection == null) return false;

            return unpublishOverridesSection.Enabled;
        }
    }
}
