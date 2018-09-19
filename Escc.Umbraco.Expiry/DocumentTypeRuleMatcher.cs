using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Escc.Umbraco.Expiry
{
    /// <summary>
    /// Checks whether a specific document type in use at a specific level in the content tree matches any expiry rules
    /// </summary>
    public class DocumentTypeRuleMatcher : IDocumentTypeRuleMatcher
    {
        private readonly IEnumerable<DocumentTypeExpiryRule> _expiryRules;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentTypeRuleMatcher"/> class.
        /// </summary>
        /// <param name="expiryRules">The expiry rules.</param>
        public DocumentTypeRuleMatcher(IEnumerable<DocumentTypeExpiryRule> expiryRules)
        {
            _expiryRules = expiryRules;
        }

        /// <summary>
        /// Check for and process an override based on the page document type
        /// </summary>
        /// <param name="documentTypeAlias">Doc Type to check</param>
        /// <param name="levelInContentTree">Optional level to check at</param>
        /// <returns>
        /// Returns the matched rule, or <c>null</c> if no rule is found
        /// </returns>
        public IExpiryRule MatchRule(string documentTypeAlias, int? levelInContentTree)
        {
            // If a document type alias was not passed, return false (no override)
            if (string.IsNullOrEmpty(documentTypeAlias)) return null;

            // Look for the template name in the overrides list
            var documentTypeRule = _expiryRules.FirstOrDefault(n => n.Alias == documentTypeAlias);

            // If document type not found then return null (no override)
            if (documentTypeRule == null) return null;

            // If a specific level was not provided, then the override applies at any level
            if (!documentTypeRule.Level.HasValue) return documentTypeRule;

            // If the document type override applies at a specific level
            return documentTypeRule.Level == levelInContentTree ? documentTypeRule : null;
        }
    }
}
