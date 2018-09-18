using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Escc.Umbraco.Expiry
{
    /// <summary>
    /// A rule which sets the expiry policy for nodes with a specific document type
    /// </summary>
    public class DocumentTypeExpiryRule : IExpiryRule
    {
        /// <summary>
        /// Gets or sets the document type alias this rule applies to.
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// Gets or sets the level in the content tree at which the rule should be applied. * means all levels.
        /// </summary>
        public string Level { get; set; }

        /// <summary>
        /// Gets or sets the length of time pages of this document type are allowed to be published before they expire.
        /// </summary>
        /// <value>A length of time, or <c>null</c> to allow publishing without expiry</value>
        public TimeSpan? MaximumExpiry { get; set; }
    }
}
