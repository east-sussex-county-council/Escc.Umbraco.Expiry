using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Escc.Umbraco.Expiry
{
    /// <summary>
    /// A rule which sets the expiry policy for nodes with a specific URL path
    /// </summary>
    public class PathExpiryRule : IExpiryRule
    {
        /// <summary>
        /// Gets or sets the path portion of the URL to match.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets whether the rule should be applied to child nodes.
        /// </summary>
        public bool ApplyToDescendantPages { get; set; }

        /// <summary>
        /// Gets or sets the length of time pages of this document type are allowed to be published before they expire.
        /// </summary>
        /// <value>A length of time, or <c>null</c> to allow publishing without expiry</value>
        public TimeSpan? MaximumExpiry { get; set; }
    }
}
