using Escc.Umbraco.Expiry.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace Escc.Umbraco.Expiry
{
    /// <summary>
    /// Reads expiry rules from web.config
    /// </summary>
    /// <seealso cref="Escc.Umbraco.Expiry.IExpiryRuleProvider" />
    public class ExpiryRulesFromConfig : IExpiryRuleProvider
    {
        /// <summary>
        /// Gets rules based on the document type alias of the node.
        /// </summary>
        public IList<DocumentTypeExpiryRule> DocumentTypeRules { get; private set; } = new List<DocumentTypeExpiryRule>();

        /// <summary>
        /// Gets rules based on the path portial of the node URL.
        /// </summary>
        public IList<PathExpiryRule> PathRules { get; private set; } = new List<PathExpiryRule>();

        /// <summary>
        /// Gets whether rule checking is enabled.
        /// </summary>
        public bool IsEnabled { get { return CheckEnabled(); } }

        /// <summary>
        /// Gets the default length of time pages are allowed to be published before they expire.
        /// </summary>
        public TimeSpan DefaultMaximumExpiry { get { return ReadExpiry(); }  }

        /// <summary>
        /// Initializes the <see cref="ExpiryRulesFromConfig"/> class.
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
                if (documentTypeElement.MaximumExpiryDays.HasValue || documentTypeElement.MaximumExpiryMonths.HasValue)
                {
                    var baseDate = DateTime.UtcNow;
                    var basePlusExpiry = baseDate;
                    if (documentTypeElement.MaximumExpiryDays.HasValue) basePlusExpiry = basePlusExpiry.AddDays(documentTypeElement.MaximumExpiryDays.Value);
                    if (documentTypeElement.MaximumExpiryMonths.HasValue) basePlusExpiry = basePlusExpiry.AddMonths(documentTypeElement.MaximumExpiryMonths.Value);
                    rule.MaximumExpiry = basePlusExpiry.Subtract(baseDate);
                }

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

                if (pathElement.MaximumExpiryDays.HasValue || pathElement.MaximumExpiryMonths.HasValue)
                {
                    var baseDate = DateTime.UtcNow;
                    var basePlusExpiry = baseDate;
                    if (pathElement.MaximumExpiryDays.HasValue) basePlusExpiry = basePlusExpiry.AddDays(pathElement.MaximumExpiryDays.Value);
                    if (pathElement.MaximumExpiryMonths.HasValue) basePlusExpiry = basePlusExpiry.AddMonths(pathElement.MaximumExpiryMonths.Value);
                    rule.MaximumExpiry = basePlusExpiry.Subtract(baseDate);
                }

                PathRules.Add(rule);
            }
        }

        private static bool CheckEnabled()
        {
            var unpublishOverridesSection = ConfigurationManager.GetSection(UnpublishOverridesSection.SectionName) as UnpublishOverridesSection;

            if (unpublishOverridesSection == null) return false;

            return unpublishOverridesSection.Enabled;
        }

        private TimeSpan ReadExpiry()
        {
            var unpublishOverridesSection = ConfigurationManager.GetSection(UnpublishOverridesSection.SectionName) as UnpublishOverridesSection;

            // Note: This shouldn't happen because CheckEnabled() would always be run before this method
            if (unpublishOverridesSection == null) throw new ConfigurationErrorsException("Default expiry period was not set on the <UnpublishOverrides /> section");

            var baseDate = DateTime.UtcNow;
            var basePlusExpiry = baseDate.AddDays(unpublishOverridesSection.MaximumExpiryDays).AddMonths(unpublishOverridesSection.MaximumExpiryMonths);
            return basePlusExpiry.Subtract(baseDate);
        }
    }
}
