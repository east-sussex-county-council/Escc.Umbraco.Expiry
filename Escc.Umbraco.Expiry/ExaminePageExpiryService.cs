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
        public IList<UmbracoPagesForUser> GetExpiringNodesByUser(int inTheNextHowManyDays)
        {
            // if the node is expiring within the declared period, add it to the list
            // if the node has a null expire date and is published, also add it to the list as it is a never expiring page
            // (Note: This should be the External index, so all results are published nodes)
            var query = _examineSearcher.CreateSearchCriteria(Examine.SearchCriteria.BooleanOperation.Or);
            query.Range("expireDate", DateTime.Today, DateTime.Today.AddDays(inTheNextHowManyDays)).Or().Field("expireDate", "99991231235959");

            // Sorting using Examine would be faster but was not working, so sort the results in .NET
            var expiringNodes = _examineSearcher.Search(query).OrderBy(result => result.Fields["expireDate"].ToString());

            // For each page:
            IList<UmbracoPagesForUser> userPages = new List<UmbracoPagesForUser>();

            // Create a admin account record. Use -1 as an Id as there won't be a valid Umbraco user with that value.
            var admin = new UmbracoPagesForUser();
            var adminUser = new UmbracoUser
            {
                UserId = -1,
                UserName = _adminAccountName.Replace(" ", ""),
                FullName = _adminAccountName,
                EmailAddress = _adminAccountEmail
            };
            admin.User = adminUser;
            userPages.Add(admin);

            foreach (var expiringNode in expiringNodes)
            {
                var userPage = new UmbracoPage
                    {
                        PageId = Int32.Parse(expiringNode.Fields["__NodeId"], CultureInfo.InvariantCulture),
                        PageName = expiringNode.Fields["nodeName"],
                        PagePath = expiringNode.Fields["path"]
                    };
                userPage.PageUrl = _umbracoHelper.NiceUrl(userPage.PageId);
                if (expiringNode.Fields["expireDate"] != "99991231235959")
                {
                    var expireDate = expiringNode.Fields["expireDate"].ToString();
                    userPage.ExpiryDate = new DateTime(Int32.Parse(expireDate.Substring(0, 4)), Int32.Parse(expireDate.Substring(4, 2)), Int32.Parse(expireDate.Substring(6, 2)), Int32.Parse(expireDate.Substring(8, 2)), Int32.Parse(expireDate.Substring(10, 2)), Int32.Parse(expireDate.Substring(12, 2)));
                }

                // Get Web Authors with permission
                // if no permissions at all, then there will be only one element which will contain a "-"
                // If only the default permission then there will only be one element which will contain "F" (Browse Node)
                var perms =
                    _contentService.GetPermissionsForEntity(_contentService.GetById(userPage.PageId))
                        .Where(
                            x =>
                                x.AssignedPermissions.Count() > 1 ||
                                (x.AssignedPermissions[0] != "-" && x.AssignedPermissions[0] != "F"));
                
                var nodeAuthors = perms as IList<EntityPermission> ?? perms.ToList();

                // if no Web Authors, add this page to the WebStaff list
                if (!nodeAuthors.Any())
                {

                    foreach (var adminPages in userPages.Where(p => p.User.UserId == -1))
                    {
                        adminPages.Pages.Add(userPage);
                    }
                    continue;
                }

                // if all Authors of a page are disabled, add page to the webStaff list
                List<EntityPermission> disabledUsers = new List<EntityPermission>();
                foreach (var user in nodeAuthors)
                {
                    var tempUser = _userService.GetUserById(user.UserId);
                    if (!tempUser.IsApproved)
                    {
                        disabledUsers.Add(user);
                    }
                }
                if(disabledUsers.Count == nodeAuthors.Count)
                {
                    foreach (var adminPages in userPages.Where(p => p.User.UserId == -1))
                    {
                        adminPages.Pages.Add(userPage);
                    }
                    continue;
                }

                // Add the current page to each user that has edit rights
                foreach (var author in nodeAuthors)
                {
                    var user = userPages.FirstOrDefault(f => f.User.UserId == author.UserId);

                    // Create a User record if one does not yet exist
                    if (user == null)
                    {
                        var pUser = _userService.GetUserById(author.UserId);

                        // Check that this author is not Disabled / Locked Out
                        // If they are, end this loop and move onto the next author
                        if (!pUser.IsApproved) continue;

                        var p = new UmbracoUser
                        {
                            UserId = author.UserId,
                            UserName = pUser.Username,
                            FullName = pUser.Name,
                            EmailAddress = pUser.Email
                        };

                        user = new UmbracoPagesForUser {User = p};
                        userPages.Add(user);
                    }

                    // Assign the current page (outer loop) to this author
                    foreach (var authorPages in userPages.Where(p => p.User.UserId == user.User.UserId))
                    {
                        authorPages.Pages.Add(userPage);
                    }
                }
            }

            // Return a list of users to email, along with the page details
            return userPages;
        }
    }
}