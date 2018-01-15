using System.Collections.Generic;

namespace Escc.Umbraco.Expiry.Notifier
{
    /// <summary>
    /// A way to get a list of pages due to expire
    /// </summary>
    public interface IExpiringPagesProvider
    {
        /// <summary>
        /// Gets the pages due to expire in the next <c>inTheNextHowManyDays</c> days, collated by the users responsible for the pages.
        /// </summary>
        /// <param name="inTheNextHowManyDays">The date range to look for expiring pages, based on the number of days from today.</param>
        /// <returns></returns>
        IList<UmbracoPagesForUser> GetExpiringPagesByUser(int inTheNextHowManyDays);
    }
}