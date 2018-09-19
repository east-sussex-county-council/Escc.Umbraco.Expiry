using System;
using System.Collections.Generic;

namespace Escc.Umbraco.Expiry
{
    /// <summary>
    /// Reads expiry rules from a rule repository
    /// </summary>
    public interface IExpiryRuleProvider
    {
        /// <summary>
        /// Gets whether rule checking is enabled.
        /// </summary>
        bool IsEnabled { get; }
        
        /// <summary>
        /// Gets rules based on the document type alias of the node.
        /// </summary>
        IList<DocumentTypeExpiryRule> DocumentTypeRules { get; }

        /// <summary>
        /// Gets rules based on the path portial of the node URL.
        /// </summary>
        IList<PathExpiryRule> PathRules { get; }

        /// <summary>
        /// Gets the default length of time pages are allowed to be published before they expire.
        /// </summary>
        TimeSpan? DefaultMaximumExpiry { get; }
    }
}