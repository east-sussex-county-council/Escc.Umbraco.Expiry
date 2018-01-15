using System;
using System.Collections.Generic;

namespace Escc.Umbraco.Expiry
{
    /// <summary>
    /// A content node in Umbraco
    /// </summary>
    public class UmbracoPage
    {
        /// <summary>
        /// Gets or sets the page identifier.
        /// </summary>
        public int PageId { get; set; }

        /// <summary>
        /// Gets or sets the name of the page.
        /// </summary>
        public string PageName { get; set; }

        /// <summary>
        /// Gets or sets the page path.
        /// </summary>
        public string PagePath { get; set; }

        /// <summary>
        /// Gets or sets the page URL.
        /// </summary>
        public string PageUrl { get; set; }

        /// <summary>
        /// Gets or sets the expiry date.
        /// </summary>
        public DateTime? ExpiryDate { get; set; }

        /// <summary>
        /// Gets or sets the web authors responsible for the page.
        /// </summary>
        public List<string> Authors { get; set; }
    }
}