using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Umbraco.Core.Models;
using Umbraco.Web;
using Umbraco.Web.PublishedCache;

namespace Escc.Umbraco.Expiry
{
    /// <summary>
    /// Reads expiry rules from web.config
    /// </summary>
    /// <seealso cref="Escc.Umbraco.Expiry.IExpiryRuleProvider" />
    public class ExpiryRulesFromUmbraco : IExpiryRuleProvider
    {
        private readonly ContextualPublishedContentCache _umbracoCache;
        private readonly IPublishedContent _expiryRules;

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
        public bool IsEnabled { get { return _expiryRules != null; } }

        /// <summary>
        /// Gets the default length of time pages are allowed to be published before they expire.
        /// </summary>
        public TimeSpan? DefaultMaximumExpiry { get; private set; }

        /// <summary>
        /// Initializes the <see cref="ExpiryRulesFromUmbraco" /> class.
        /// </summary>
        /// <param name="umbracoCache">The Umbraco content cache.</param>
        public ExpiryRulesFromUmbraco(ContextualPublishedContentCache umbracoCache)
        {
            _umbracoCache = umbracoCache ?? throw new ArgumentNullException(nameof(umbracoCache));
            _expiryRules = _umbracoCache.GetAtRoot().FirstOrDefault(node => node.DocumentTypeAlias == "expiryRules");

            GetDocumentTypeRules();
            GetPageRules();
            GetDefaults();
        }

        private void GetDocumentTypeRules()
        {
            if (_expiryRules == null)
            {
                return;
            }

            var documentTypeRules = _expiryRules.Children("documentTypeExpiryRule");
            if (documentTypeRules == null) return;
            foreach (var documentTypeRule in documentTypeRules)
            {
                var documentTypes = documentTypeRule.GetPropertyValue<IEnumerable<string>>("documentTypes");
                foreach (var documentType in documentTypes)
                {
                    int? level = null;
                    if (!String.IsNullOrEmpty(documentTypeRule.GetPropertyValue<string>("level")))
                    {
                        level = Int32.Parse(documentTypeRule.GetPropertyValue<string>("level"), CultureInfo.CurrentCulture);
                    }
                    var rule = new DocumentTypeExpiryRule() { Alias = documentType, Level = level };

                    if (DocumentTypeRules.FirstOrDefault(alreadyAdded => alreadyAdded.Alias == rule.Alias && alreadyAdded.Level == rule.Level) == null)
                    {
                        var months = documentTypeRule.GetPropertyValue<int>("months");
                        var days = documentTypeRule.GetPropertyValue<int>("days");
                        var forceNever = documentTypeRule.GetPropertyValue<bool>("forcePagesToNeverExpire");

                        if ((months > 0 || days > 0) && !forceNever)
                        {
                            var baseDate = DateTime.UtcNow;
                            var basePlusExpiry = baseDate.AddMonths(months).AddDays(days);
                            rule.MaximumExpiry = basePlusExpiry.Subtract(baseDate);
                        }

                        DocumentTypeRules.Add(rule);
                    }
                }
            }
        }

        private void GetPageRules()
        {
            if (_expiryRules == null)
            {
                return;
            }

            var pageRules = _expiryRules.Children("pageExpiryRule");
            if (pageRules == null) return;
            foreach (var pageRule in pageRules)
            {
                var pages = pageRule.GetPropertyValue<IEnumerable<IPublishedContent>>("pages");
                foreach (var page in pages)
                {
                    var rule = new PathExpiryRule() { Path = page.Url, ApplyToDescendantPages = pageRule.GetPropertyValue<bool>("applyToDescendantPages") };

                    if (PathRules.FirstOrDefault(alreadyAdded => alreadyAdded.Path == rule.Path && alreadyAdded.ApplyToDescendantPages == rule.ApplyToDescendantPages) == null)
                    {
                        var months = pageRule.GetPropertyValue<int>("months");
                        var days = pageRule.GetPropertyValue<int>("days");
                        var forceNever = pageRule.GetPropertyValue<bool>("forcePagesToNeverExpire");

                        if ((months > 0 || days > 0) && !forceNever)
                        {
                            var baseDate = DateTime.UtcNow;
                            var basePlusExpiry = baseDate.AddMonths(months).AddDays(days);
                            rule.MaximumExpiry = basePlusExpiry.Subtract(baseDate);
                        }

                        PathRules.Add(rule);
                    }
                }
            }
        }


        private void GetDefaults()
        {
            DefaultMaximumExpiry = null;
            if (_expiryRules == null)
            {
                return;
            }

            var months = _expiryRules.GetPropertyValue<int>("months");
            var days = _expiryRules.GetPropertyValue<int>("days");
            var allowNever = _expiryRules.GetPropertyValue<bool>("allowPagesToNeverExpire");

            if ((months > 0 || days > 0) && !allowNever)
            {
                var baseDate = DateTime.UtcNow;
                var basePlusExpiry = baseDate.AddMonths(months).AddDays(days);
                DefaultMaximumExpiry = basePlusExpiry.Subtract(baseDate);
            }
        }
    }
}
