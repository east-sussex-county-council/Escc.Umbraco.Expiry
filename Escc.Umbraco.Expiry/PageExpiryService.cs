using System;
using System.Collections.Generic;
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
    public class PageExpiryService : IPageExpiryService
    {
        private readonly IContentService _contentService;
        private readonly UmbracoHelper _umbracoHelper;
        private readonly IUserService _userService;
        private readonly string _adminAccountName;
        private readonly string _adminAccountEmail;

        /// <summary>
        /// Initializes a new instance of the <see cref="PageExpiryService" /> class.
        /// </summary>
        /// <param name="userService">The user service.</param>
        /// <param name="contentService">The content service.</param>
        /// <param name="umbracoHelper">An instance of UmbracoHelper.</param>
        /// <param name="adminAccountName">Name of an Umbraco admin account.</param>
        /// <param name="adminAccountEmail">The email address of an Umbraco admin account.</param>
        public PageExpiryService(IUserService userService, IContentService contentService, UmbracoHelper umbracoHelper, string adminAccountName, string adminAccountEmail)
        {
            _userService = userService;
            _contentService = ApplicationContext.Current.Services.ContentService;
            _umbracoHelper = umbracoHelper;
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
            // Get all content at the root
            var rootnodes = _contentService.GetRootContent();
            // Create a list to store expiring content
            List<IContent> expiringNodes = new List<IContent>();
            // for each content node at the root
            foreach (var node in rootnodes)
            {
                // if the node is expiring within the declared period, add it to the list
                // if the node has a null expire date and is published, also add it to the list as it is a neverexpiring page
                if(node.ExpireDate > DateTime.Now && node.ExpireDate < DateTime.Now.AddDays(inTheNextHowManyDays) || node.ExpireDate == null && node.HasPublishedVersion == true)
                {
                    expiringNodes.Add(node);
                }
                // get the root nodes children that are expiring within the declared period. Or have a null expiry date and are published
                var descendants = node.Descendants().Where(nn => nn.ExpireDate > DateTime.Now && nn.ExpireDate < DateTime.Now.AddDays(inTheNextHowManyDays) || nn.ExpireDate == null && nn.HasPublishedVersion == true).OrderBy(nn => nn.ExpireDate);
                foreach (var child in descendants)
                {
                    // add each one to the list
                    expiringNodes.Add(child);
                }
            }
            // once done, order by expire date.
            expiringNodes.OrderBy(nn => nn.ExpireDate);

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
                        PageId = expiringNode.Id,
                        PageName = expiringNode.Name,
                        PagePath = expiringNode.Path,
                        PageUrl = _umbracoHelper.NiceUrl(expiringNode.Id),
                        ExpiryDate = (DateTime?)expiringNode.ExpireDate
                    };

                // Get Web Authors with permission
                // if no permissions at all, then there will be only one element which will contain a "-"
                // If only the default permission then there will only be one element which will contain "F" (Browse Node)
                var perms =
                    _contentService.GetPermissionsForEntity(expiringNode)
                        .Where(
                            x =>
                                x.AssignedPermissions.Count() > 1 ||
                                (x.AssignedPermissions[0] != "-" && x.AssignedPermissions[0] != "F"));
                
                var nodeAuthors = perms as IList<EntityPermission> ?? perms.ToList();

                // if no Web Authors, add this page to the WebStaff list
                if (!nodeAuthors.Any())
                {

                    userPages.Where(p => p.User.UserId == -1).ForEach(u => u.Pages.Add(userPage));
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
                    userPages.Where(p => p.User.UserId == -1).ForEach(u => u.Pages.Add(userPage));
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
                    userPages.Where(p => p.User.UserId == user.User.UserId).ForEach(u => u.Pages.Add(userPage));
                }
            }

            // Return a list of users to email, along with the page details
            return userPages;
        }
    }
}