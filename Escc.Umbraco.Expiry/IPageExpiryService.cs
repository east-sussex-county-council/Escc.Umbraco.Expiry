using System.Collections.Generic;

namespace Escc.Umbraco.Expiry
{
    /// <summary>
    /// A service that provides information about pages due to expire
    /// </summary>
    public interface IPageExpiryService
    {
        /// <summary>
        /// Checks for expiring pages, collated according to the user responsible for them
        /// </summary>
        /// <param name="inTheNextHowManyDays">The date range, beginning today, during which the pages we want to know about are due to expire.</param>
        /// <returns></returns>
        IEnumerable<UmbracoPage> GetExpiringPages(int inTheNextHowManyDays);
    }
}