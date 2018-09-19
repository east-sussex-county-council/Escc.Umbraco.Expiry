using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Events;

namespace Escc.Umbraco.Expiry
{
    /// <summary>
    /// The result of evaluating a set of expiry rules against a content node
    /// </summary>
    public class ExpiryRuleResult
    {
        /// <summary>
        /// Gets or sets the message to be returned if the save should be cancelled.
        /// </summary>
        public string CancellationMessage { get; set; }

        /// <summary>
        /// Gets or sets the message to be returned if the expiry date was changed by expiry rules.
        /// </summary>
        public string ExpireDateChangedMessage { get; set; }

        /// <summary>
        /// Gets or sets the expire date.
        /// </summary>
        public DateTime? ExpireDate { get; set; }
    }
}
