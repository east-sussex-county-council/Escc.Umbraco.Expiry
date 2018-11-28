using Examine;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Membership;
using Umbraco.Core.Services;
using Umbraco.Web;

namespace Escc.Umbraco.Expiry
{
    /// <summary>
    /// A service that provides information about pages due to expire by querying the Umbraco content service
    /// </summary>
    /// <seealso cref="Escc.Umbraco.Expiry.IPageExpiryService" />
    public class ExaminePageExpiryService : IPageExpiryService
    {
        private readonly UmbracoHelper _umbracoHelper;
        private readonly ISearcher _examineSearcher;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExaminePageExpiryService" /> class.
        /// </summary>
        /// <param name="examineSearcher">The examine searcher.</param>
        /// <param name="umbracoHelper">An instance of UmbracoHelper.</param>
        /// <exception cref="ArgumentNullException">
        /// examineSearcher
        /// or
        /// umbracoHelper
        /// </exception>
        public ExaminePageExpiryService(ISearcher examineSearcher, UmbracoHelper umbracoHelper)
        {
            _examineSearcher = examineSearcher ?? throw new ArgumentNullException(nameof(examineSearcher));
            _umbracoHelper = umbracoHelper ?? throw new ArgumentNullException(nameof(umbracoHelper));
        }

        /// <summary>
        /// Get a list of expiring pages expiring soon, or due never to expire
        /// </summary>
        /// <param name="inTheNextHowManyDays">
        /// How many days to look forward
        /// </param>
        public IEnumerable<UmbracoPage> GetExpiringPages(int inTheNextHowManyDays)
        {
            // if the node is expiring within the declared period, add it to the list
            // if the node has a null expire date and is published, also add it to the list as it is a never expiring page
            // (Note: This should be the External index, so all results are published nodes)
            var query = _examineSearcher.CreateSearchCriteria(Examine.SearchCriteria.BooleanOperation.Or);
            query.Range("expireDate", DateTime.Today, DateTime.Today.AddDays(inTheNextHowManyDays)).Or().Field("expireDate", "99991231235959");

            // Sorting using Examine would be faster but was not working, so sort the results in .NET
            var expiringNodes = _examineSearcher.Search(query).OrderBy(result => result.Fields["expireDate"].ToString());

            var pages = new List<UmbracoPage>();
            foreach (var expiringNode in expiringNodes)
            {
                var page = new UmbracoPage
                {
                    PageId = Int32.Parse(expiringNode.Fields["__NodeId"], CultureInfo.InvariantCulture),
                    PageName = expiringNode.Fields["nodeName"],
                    PagePath = expiringNode.Fields["path"]
                };
                page.PageUrl = _umbracoHelper.NiceUrl(page.PageId);
                if (expiringNode.Fields["expireDate"] != "99991231235959")
                {
                    var expireDate = expiringNode.Fields["expireDate"].ToString();
                    page.ExpiryDate = new DateTime(Int32.Parse(expireDate.Substring(0, 4)), Int32.Parse(expireDate.Substring(4, 2)), Int32.Parse(expireDate.Substring(6, 2)), Int32.Parse(expireDate.Substring(8, 2)), Int32.Parse(expireDate.Substring(10, 2)), Int32.Parse(expireDate.Substring(12, 2)));
                }
                pages.Add(page);
            }

            return pages;
        }
    }
}