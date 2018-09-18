using System;

namespace Escc.Umbraco.Expiry
{
    /// <summary>
    /// A rule which determines the maximum expiry date allowed for a piece of content
    /// </summary>
    public interface IExpiryRule
    {
        /// <summary>
        /// Gets the length of time a pages is allowed to be published before it expires. <c>null</c> means no limit.
        /// </summary>
        TimeSpan? MaximumExpiry { get; }
    }
}