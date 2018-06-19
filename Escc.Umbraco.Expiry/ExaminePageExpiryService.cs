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
        private readonly Dictionary<int, IEnumerable<EntityPermission>> _permissions = new Dictionary<int, IEnumerable<EntityPermission>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ExaminePageExpiryService" /> class.
        /// </summary>
        /// <param name="examineSearcher">The examine searcher.</param>
        /// <param name="userService">The user service.</param>
        /// <param name="contentService">The content service.</param>
        /// <param name="umbracoHelper">An instance of UmbracoHelper.</param>
        /// <param name="adminAccountName">Name of an Umbraco admin account.</param>
        /// <param name="adminAccountEmail">The email address of an Umbraco admin account.</param>
        /// <exception cref="ArgumentException">
        /// message - adminAccountName
        /// or
        /// message - adminAccountEmail
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// examineSearcher
        /// or
        /// userService
        /// or
        /// contentService
        /// or
        /// umbracoHelper
        /// </exception>
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
            var admin = new UmbracoPagesForUser()
            {
                User = new UmbracoUser
                {
                    UserId = -1,
                    UserName = _adminAccountName?.Replace(" ", ""),
                    FullName = _adminAccountName,
                    EmailAddress = _adminAccountEmail
                }
            };
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

                var permissionsForNode = GetPermissionsForNodeWithInheritance(_contentService.GetById(userPage.PageId));

                // If there are no active users with permissions to the page, add the page to the admin list
                var pageHasActiveUserWithPermissions = false;
                foreach (var permission in permissionsForNode)
                {
                    var usersInGroup = _userService.GetAllInGroup(permission.UserGroupId);
                    foreach (var user in usersInGroup)
                    {
                        if (user.IsApproved)
                        {
                            pageHasActiveUserWithPermissions = true;
                            break;
                        }
                    }
                    if (pageHasActiveUserWithPermissions) break;
                }
                if (!pageHasActiveUserWithPermissions)
                {
                    admin.Pages.Add(userPage);
                    continue;
                }

                // Add the current page to each user that has edit rights
                foreach (var permission in permissionsForNode)
                {
                    var usersInGroup = _userService.GetAllInGroup(permission.UserGroupId);
                    foreach (var user in usersInGroup)
                    {
                        var pagesForUser = userPages.FirstOrDefault(f => f.User.UserId == user.Id);

                        // Create a User record if one does not yet exist
                        if (pagesForUser == null)
                        {
                            // Check that this author is not Disabled / Locked Out
                            // If they are, end this loop and move onto the next author
                            if (!user.IsApproved) continue;

                            pagesForUser = new UmbracoPagesForUser
                            {
                                User = new UmbracoUser
                                {
                                    UserId = user.Id,
                                    UserName = user.Username,
                                    FullName = user.Name,
                                    EmailAddress = user.Email
                                }
                            };
                            userPages.Add(pagesForUser);
                        }

                        // Assign the current page to this author
                        pagesForUser.Pages.Add(userPage);
                    }
                }
            }

            // Return a list of users responsible, along with the page details
            return userPages;
        }

        private IEnumerable<EntityPermission> GetPermissionsForNodeWithInheritance(IContent entity)
        {
            IEnumerable<EntityPermission> entityPermissions;
            if (_permissions.ContainsKey(entity.Id))
            {
                entityPermissions = _permissions[entity.Id];
            }
            else
            {
                // if no permissions at all, then there will be only one element which will contain a "-"
                // If only the default permission then there will only be one element which will contain "F" (Browse Node)
                entityPermissions = _contentService.GetPermissionsForEntity(entity)
                    .Where(x =>
                                x.AssignedPermissions.Count() > 1 ||
                                (x.AssignedPermissions[0] != "-" && x.AssignedPermissions[0] != "F"));
                _permissions.Add(entity.Id, entityPermissions);
            }

            while (!entityPermissions.Any())
            {
                entity = entity.Parent();
                if (entity != null)
                {
                    entityPermissions = GetPermissionsForNodeWithInheritance(entity);
                }
                else
                {
                    break;
                }
            }
            return entityPermissions;
        }
    }
}