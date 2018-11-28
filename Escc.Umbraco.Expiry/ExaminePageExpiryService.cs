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
        private readonly IContentService _contentService;
        private readonly UmbracoHelper _umbracoHelper;
        private readonly ISearcher _examineSearcher;
        private readonly IUserService _userService;
        private readonly string _adminAccountName;
        private readonly string _adminAccountEmail;

        /// <summary>
        /// Initializes a new instance of the <see cref="PageExpiryService" /> class.
        /// </summary>
        /// <param name="examineSearcher">The examine searcher.</param>
        /// <param name="userService">The user service.</param>
        /// <param name="contentService">The content service.</param>
        /// <param name="umbracoHelper">An instance of UmbracoHelper.</param>
        /// <param name="adminAccountName">Name of an Umbraco admin account.</param>
        /// <param name="adminAccountEmail">The email address of an Umbraco admin account.</param>
        public ExaminePageExpiryService(ISearcher examineSearcher, IUserService userService, IContentService contentService, UmbracoHelper umbracoHelper, string adminAccountName, string adminAccountEmail)
        {
            if (string.IsNullOrEmpty(adminAccountName))
            {
                throw new ArgumentException("message", nameof(adminAccountName));
            }

            if (string.IsNullOrEmpty(adminAccountEmail))
            {
                throw new ArgumentException("message", nameof(adminAccountEmail));
            }

            _examineSearcher = examineSearcher ?? throw new ArgumentNullException(nameof(examineSearcher));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _contentService = contentService ?? throw new ArgumentNullException(nameof(contentService));
            _umbracoHelper = umbracoHelper ?? throw new ArgumentNullException(nameof(umbracoHelper));
            _adminAccountName = adminAccountName;
            _adminAccountEmail = adminAccountEmail;
        }

        /// <summary>
        /// Get a list of expiring pages, collated by User
        /// </summary>
        /// <param name="inTheNextHowManyDays">
        /// How many days to look forward
        /// </param>
        /// <returns>
        /// List Users with expiring pages they are responsible for
        /// </returns>
        [Obsolete("Takes too long on large sites. Use GetExpiringNodes(), PermissionsForPage() and UserById() instead.")]
        public IList<UmbracoPagesForUser> GetExpiringNodesByUser(int inTheNextHowManyDays)
        {
            List<UmbracoPage> pages = GetExpiringNodes(inTheNextHowManyDays);

            // For each page:
            IList<UmbracoPagesForUser> userPages = new List<UmbracoPagesForUser>();

            // Create a admin account record. Use -1 as an Id as there won't be a valid Umbraco user with that value.
            var admin = new UmbracoPagesForUser();
            var adminUser = new UmbracoUser
            {
                UserId = -1,
                EmailAddress = _adminAccountEmail
            };
            admin.User = adminUser;
            userPages.Add(admin);

            foreach (var page in pages)
            {
                IEnumerable<int> userIdsWithPermission = PermissionsForPage(page.PageId);

                // if no Web Authors, add this page to the WebStaff list
                if (!userIdsWithPermission.Any())
                {

                    foreach (var adminPages in userPages.Where(p => p.User.UserId == -1))
                    {
                        adminPages.Pages.Add(page);
                    }
                    continue;
                }

                // if all Authors of a page are disabled, add page to the webStaff list
                List<int> disabledUsers = new List<int>();
                foreach (var userId in userIdsWithPermission)
                {
                    var tempUser = _userService.GetUserById(userId);
                    if (!tempUser.IsApproved)
                    {
                        disabledUsers.Add(userId);
                    }
                }
                if (disabledUsers.Count == userIdsWithPermission.Count())
                {
                    foreach (var adminPages in userPages.Where(p => p.User.UserId == -1))
                    {
                        adminPages.Pages.Add(page);
                    }
                    continue;
                }

                // Add the current page to each user that has edit rights
                foreach (var userId in userIdsWithPermission)
                {
                    var user = userPages.FirstOrDefault(f => f.User.UserId == userId);

                    // Create a User record if one does not yet exist
                    if (user == null)
                    {
                        UmbracoUser p = UserById(userId);

                        // Check that this author is not Disabled / Locked Out
                        // If they are, end this loop and move onto the next author
                        if (!p.IsApproved) continue;

                        user = new UmbracoPagesForUser { User = p };
                        userPages.Add(user);
                    }

                    // Assign the current page (outer loop) to this author
                    foreach (var authorPages in userPages.Where(p => p.User.UserId == user.User.UserId))
                    {
                        authorPages.Pages.Add(page);
                    }
                }
            }

            // Return a list of users to email, along with the page details
            return userPages;
        }

        /// <summary>
        /// Gets name, email and approval status of an Umbraco user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public UmbracoUser UserById(int userId)
        {
            var user = _userService.GetUserById(userId);

            return new UmbracoUser
            {
                UserId = userId,
                UserName = user.Username,
                FullName = user.Name,
                EmailAddress = user.Email,
                IsApproved = user.IsApproved
            };
        }



        /// <summary>
        /// Gets the permissions for a page, so long as it's more than just Browse Node
        /// </summary>
        /// <param name="pageId"></param>
        /// <returns>Ids of users with permissions to the page</returns>
        public IEnumerable<int> PermissionsForPage(int pageId)
        {
            // Get Web Authors with permission
            // if no permissions at all, then there will be only one element which will contain a "-"
            // If only the default permission then there will only be one element which will contain "F" (Browse Node)
            return _contentService.GetPermissionsForEntity(_contentService.GetById(pageId))
                    .Where(
                        x =>
                            x.AssignedPermissions.Count() > 1 ||
                            (x.AssignedPermissions[0] != "-" && x.AssignedPermissions[0] != "F")).Select(permission => permission.UserId);
        }

        /// <summary>
        /// Get a list of expiring pages
        /// </summary>
        /// <param name="inTheNextHowManyDays">
        /// How many days to look forward
        /// </param>
        public List<UmbracoPage> GetExpiringNodes(int inTheNextHowManyDays)
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