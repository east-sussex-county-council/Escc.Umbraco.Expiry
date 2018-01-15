using System.Collections.Generic;

namespace Escc.Umbraco.Expiry
{
    /// <summary>
    /// A set of Umbraco content nodes and the back office user responsible for them
    /// </summary>
    public class UmbracoPagesForUser
    {
        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        public UmbracoUser User { get; set; }

        /// <summary>
        /// Gets the pages.
        /// </summary>
        public IList<UmbracoPage> Pages { get; private set; } = new List<UmbracoPage>();

    }
}