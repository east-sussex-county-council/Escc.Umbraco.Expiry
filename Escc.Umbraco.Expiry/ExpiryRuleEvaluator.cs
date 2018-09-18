using Escc.Umbraco.Expiry.Configuration;
using System;
using System.Collections.Generic;
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
    /// Checks if any expiry rule of any type applies to a node
    /// </summary>
    class ExpiryRuleEvaluator
    {
        /// <summary>
        /// Check if an override exists
        /// </summary>
        /// <param name="expiryRules">The expiry rules.</param>
        /// <param name="contentItem">Content Item to check</param>
        /// <returns>
        /// True if an override exists
        /// </returns>
        public IExpiryRule CheckOverride(IExpiryRuleProvider expiryRules, IContent contentItem)
        {
            if (expiryRules == null) return null;
            if (contentItem == null) return null;

            // Check for a ContentType override
            IExpiryRule matchedRule = new DocumentTypeRuleMatcher(expiryRules.DocumentTypeRules, contentItem.ContentType.Alias, contentItem.Level.ToString()).MatchRule();
            if (matchedRule != null) return matchedRule;

            // Check for an override based on the Url
            matchedRule = new PathRuleMatcher(expiryRules.PathRules, GetNodeUrl(contentItem).ToLower()).MatchRule();
            if (matchedRule != null) return matchedRule;

            return null;
        }

        /// <summary>
        /// Used where IContent is not available
        /// </summary>
        /// <param name="expiryRules">The expiry rules.</param>
        /// <param name="contentItem">Content Item to Check</param>
        /// <returns>
        /// True if an override exists
        /// </returns>
        public IExpiryRule CheckOverride(IExpiryRuleProvider expiryRules, ContentItemDisplay contentItem)
        {
            if (expiryRules == null) return null;
            if (contentItem == null) return null;

            var contentService = ApplicationContext.Current.Services.ContentService;
            var content = contentService.GetById(contentItem.Key);

            return CheckOverride(expiryRules, content);
        }

        /// <summary>
        /// Used where IContent is not available
        /// </summary>
        /// <param name="expiryRules">The expiry rules.</param>
        /// <param name="contentItem">Content Item to Check</param>
        /// <returns>
        /// True if an override exists
        /// </returns>
        public IExpiryRule CheckOverride(IExpiryRuleProvider expiryRules, IPublishedContent contentItem)
        {
            if (expiryRules == null) return null;
            if (contentItem == null) return null;

            // Check for a ContentType override
            IExpiryRule matchedRule = new DocumentTypeRuleMatcher(expiryRules.DocumentTypeRules, contentItem.ContentType.Alias, contentItem.Level.ToString()).MatchRule();
            if (matchedRule != null) return matchedRule;

            // Check for an override based on the Url
            matchedRule = new PathRuleMatcher(expiryRules.PathRules, contentItem.Url.ToLower()).MatchRule();
            if (matchedRule != null) return matchedRule;

            return null;
        }

        /// <summary>
        /// Get or construct the node Url
        /// </summary>
        /// <param name="node">Node to process</param>
        /// <returns>Node Url</returns>
        private static string GetNodeUrl(IContent node)
        {
            // Make sure we have a current Umbraco Context
            if (UmbracoContext.Current == null)
            {
                var dummyContext = new HttpContextWrapper(new HttpContext(new SimpleWorkerRequest("/", string.Empty, new StringWriter())));
                UmbracoContext.EnsureContext(
                    dummyContext,
                    ApplicationContext.Current,
                    new WebSecurity(dummyContext, ApplicationContext.Current),
                    false);
            }
            var helper = new UmbracoHelper(UmbracoContext.Current);

            var entityUrl = helper.NiceUrl(node.Id);

            if (!string.IsNullOrEmpty(entityUrl) && entityUrl != "#") return entityUrl;

            // Just need the Url of the parent node...
            entityUrl = helper.Url(node.ParentId);
            if (entityUrl == "#") entityUrl = string.Empty;
            if (!entityUrl.EndsWith("/")) entityUrl += "/";

            // Then add the current node name
            var nodeName = node.Name;
            if (node.HasProperty("umbracoUrlName") && !string.IsNullOrEmpty(node.GetValue<string>("umbracoUrlName")))
            {
                nodeName = node.GetValue<string>("umbracoUrlName");
            }

            nodeName = umbraco.cms.helpers.url.FormatUrl(nodeName);
            entityUrl = string.Format("{0}{1}/", entityUrl, nodeName);

            return entityUrl;
        }
    }
}
