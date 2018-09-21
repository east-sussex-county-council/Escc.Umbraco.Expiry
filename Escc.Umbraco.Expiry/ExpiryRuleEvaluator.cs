using System;
using Umbraco.Core.Models;

namespace Escc.Umbraco.Expiry
{
    /// <summary>
    /// Checks if any expiry rule of any type applies to a node
    /// </summary>
    public class ExpiryRuleEvaluator
    {
        public ExpiryRuleResult ApplyExpiryRules(DateTime timePublished, TimeSpan? defaultMaximumExpiry, IDocumentTypeRuleMatcher documentTypeMatcher, IPathRuleMatcher pathMatcher, IContent contentNode, INodeUrlBuilder urlBuilder)
        {
            if (documentTypeMatcher == null)
            {
                throw new ArgumentNullException(nameof(documentTypeMatcher));
            }

            if (pathMatcher == null)
            {
                throw new ArgumentNullException(nameof(pathMatcher));
            }

            if (contentNode == null)
            {
                throw new ArgumentNullException(nameof(contentNode));
            }

            if (urlBuilder == null)
            {
                throw new ArgumentNullException(nameof(urlBuilder));
            }
            // Check for expiry rule
            var expiryRule = MatchExpiryRules(documentTypeMatcher, pathMatcher, contentNode, urlBuilder);

            // There is an rule with no date, meaning it must be set to never, so disallow a date is one is set
            if (expiryRule != null && !expiryRule.MaximumExpiry.HasValue && contentNode.ExpireDate.HasValue)
            {
                return new ExpiryRuleResult() { CancellationMessage = "You cannot enter an 'Unpublish at' date for this page" };
            }

            // Date cannot be more than a set timespan into the future
            DateTime? maximumDate = null;
            if (expiryRule != null)
            {
                if (expiryRule.MaximumExpiry.HasValue)
                {
                    maximumDate = timePublished.Add(expiryRule.MaximumExpiry.Value);
                }
            }
            else if (defaultMaximumExpiry.HasValue)
            {
                maximumDate = timePublished.Add(defaultMaximumExpiry.Value);
            }

            if (maximumDate.HasValue && !contentNode.ExpireDate.HasValue)
            {
                // Default the date to the maximum allowed and continue publishing.
                return new ExpiryRuleResult() {
                    ExpireDate = maximumDate,
                    ExpireDateChangedMessage = "The 'Unpublish at' date is a required field. The date has been set to " + DisplayDate(maximumDate.Value) + ". You can refresh the page to see the new date."
                };
            }
            else if (maximumDate.HasValue && contentNode.ExpireDate > maximumDate)
            {
                // Default the date to the maximum allowed and continue publishing.
                return new ExpiryRuleResult() {
                    ExpireDate = maximumDate,
                    ExpireDateChangedMessage = "The 'Unpublish at' date is too far into the future. The date has been set to: " + DisplayDate(maximumDate.Value) + ". You can refresh the page to see the new date."
                };
            }

            // Current setting is OK - no change
            return new ExpiryRuleResult() { ExpireDate = contentNode.ExpireDate };
        }

        private static string DisplayDate(DateTime defaultMaximumDate)
        {
            return defaultMaximumDate.ToString("dd MMMM yyyy") + defaultMaximumDate.ToString(" h.mmtt").ToLower();
        }

        /// <summary>
        /// Check if an override exists
        /// </summary>
        /// <param name="documentTypeMatcher">The document type rule matcher.</param>
        /// <param name="pathMatcher">The path matcher.</param>
        /// <param name="contentItem">Content Item to check</param>
        /// <returns>
        /// True if an override exists
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// documentTypeMatcher
        /// or
        /// pathMatcher
        /// </exception>
        public IExpiryRule MatchExpiryRules(IDocumentTypeRuleMatcher documentTypeMatcher, IPathRuleMatcher pathMatcher, IContent contentItem, INodeUrlBuilder urlBuilder)
        {
            if (documentTypeMatcher == null)
            {
                throw new ArgumentNullException(nameof(documentTypeMatcher));
            }

            if (pathMatcher == null)
            {
                throw new ArgumentNullException(nameof(pathMatcher));
            }

            if (contentItem == null) return null;
            if (urlBuilder == null)
            {
                throw new ArgumentNullException(nameof(urlBuilder));
            }

            // Check for a ContentType override
            IExpiryRule matchedRule = documentTypeMatcher.MatchRule(contentItem.ContentType.Alias, contentItem.Level);
            if (matchedRule != null) return matchedRule;

            // Check for an override based on the Url
            matchedRule = pathMatcher.MatchRule(urlBuilder.GetNodeUrl(contentItem).ToLower());
            if (matchedRule != null) return matchedRule;

            return null;
        }

        /// <summary>
        /// Used where IContent is not available
        /// </summary>
        /// <param name="documentTypeMatcher">The document type matcher.</param>
        /// <param name="pathMatcher">The path matcher.</param>
        /// <param name="contentItem">Content Item to Check</param>
        /// <returns>
        /// True if an override exists
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// documentTypeMatcher
        /// or
        /// pathMatcher
        /// </exception>
        public IExpiryRule MatchExpiryRules(IDocumentTypeRuleMatcher documentTypeMatcher, IPathRuleMatcher pathMatcher, IPublishedContent contentItem)
        {
            if (documentTypeMatcher == null)
            {
                throw new ArgumentNullException(nameof(documentTypeMatcher));
            }

            if (pathMatcher == null)
            {
                throw new ArgumentNullException(nameof(pathMatcher));
            }

            if (contentItem == null) return null;

            // Check for a ContentType override
            IExpiryRule matchedRule = documentTypeMatcher.MatchRule(contentItem.ContentType.Alias, contentItem.Level);
            if (matchedRule != null) return matchedRule;

            // Check for an override based on the Url
            matchedRule = pathMatcher.MatchRule(contentItem.Url.ToLower());
            if (matchedRule != null) return matchedRule;

            return null;
        }
    }
}
