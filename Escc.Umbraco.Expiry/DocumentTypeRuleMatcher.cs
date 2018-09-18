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
    /// <seealso cref="Escc.Umbraco.Expiry.IRuleMatcher" />
    public class DocumentTypeRuleMatcher : IRuleMatcher
    {
        private readonly IEnumerable<DocumentTypeExpiryRule> _expiryRules;
        private readonly string _documentTypeAlias;
        private readonly string _levelInContentTree;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentTypeRuleMatcher"/> class.
        /// </summary>
        /// <param name="expiryRules">The expiry rules.</param>
        /// <param name="documentTypeAlias">Doc Type to check</param>
        /// <param name="levelInContentTree">Optional level to check at</param>
        public DocumentTypeRuleMatcher(IEnumerable<DocumentTypeExpiryRule> expiryRules, string documentTypeAlias, string levelInContentTree)
        {
            _expiryRules = expiryRules;
            _documentTypeAlias = documentTypeAlias;
            _levelInContentTree = levelInContentTree;
        }

        /// <summary>
        /// Check for and process an override based on the page document type
        /// </summary>
        /// <returns>
        /// Returns the matched rule, or <c>null</c> if no rule is found
        /// </returns>
        public IExpiryRule MatchRule()
        {
            // If a document type alias was not passed, return false (no override)
            if (string.IsNullOrEmpty(_documentTypeAlias)) return null;

            // Look for the template name in the overrides list
            var documentTypeRule = _expiryRules.FirstOrDefault(n => n.Alias == _documentTypeAlias);

            // If document type not found then return null (no override)
            if (documentTypeRule == null) return null;

            // If a specific level was not provided, then the override applies at any level
            if (string.IsNullOrEmpty(_levelInContentTree)) return documentTypeRule;

            // If the document type override applies at a specific level
            if (!String.IsNullOrEmpty(documentTypeRule.Level) && documentTypeRule.Level != "*")
            {
                return documentTypeRule.Level == _levelInContentTree ? documentTypeRule : null;
            }

            // Document type alias found and applies at all levels
            return documentTypeRule;
        }
    }
}
