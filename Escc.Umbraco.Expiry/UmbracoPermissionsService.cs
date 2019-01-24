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
    /// A service that provides information about Umbraco permissions
    /// </summary>
    public class UmbracoPermissionsService : IPermissionsService
    {
        private readonly IContentService _contentService;
        private readonly IUserService _userService;

        /// <summary>
        /// Initializes a new instance of the <see cref="UmbracoPermissionsService" /> class.
        /// </summary>
        /// <param name="userService">The user service.</param>
        /// <param name="contentService">The content service.</param>
        /// <exception cref="ArgumentNullException">
        /// userService
        /// or
        /// contentService
        /// </exception>
        public UmbracoPermissionsService(IUserService userService, IContentService contentService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _contentService = contentService ?? throw new ArgumentNullException(nameof(contentService));
        }

        /// <summary>
        /// Gets the id and email address of users in the specified group who are approved and not locked out or disabled
        /// </summary>
        /// <param name="groupId"></param>
        /// <returns></returns>
        public IList<UmbracoUser> ActiveUsersInGroup(int groupId)
        {
            var users = new List<UmbracoUser>();
            var usersInGroup = _userService.GetAllInGroup(groupId);
            foreach (var user in usersInGroup)
            {
                if (user.IsApproved)
                {
                    users.Add(new UmbracoUser
                    {
                        UserId = user.Id,
                        EmailAddress = user.Email
                    });
                }
            }

            return users;
        }

        /// <summary>
        /// Gets the ids of all user groups with permissions to a page
        /// </summary>
        /// <param name="pageId">The integer id of a content node</param>
        /// <exception cref="ArgumentException">Thrown if pageId does not refer to a content node</exception>
        /// <returns></returns>
        public List<int> GroupsWithPermissionsForPage(int pageId)
        {
            var groupsWithAllowPermissionsForNode = new List<int>();
            var groupsWithDenyPermissionsForNode = new List<int>();
            var contentNode = _contentService.GetById(pageId);
            if (contentNode == null) throw new ArgumentException($"pageId {pageId} was not found", nameof(pageId));
            GetPermissionsForNodeWithInheritance(contentNode, groupsWithAllowPermissionsForNode, groupsWithDenyPermissionsForNode);
            return groupsWithAllowPermissionsForNode;
        }

        private void GetPermissionsForNodeWithInheritance(IContent entity, List<int> groupIdsWithAllowPermission, List<int> groupIdsWithDenyPermission)
        {
            const string BROWSE_NODE = "F";
            const string UPDATE = "A";

            // if no permissions at all, then there will be only one element which will contain a "-" so exclude those
            var entityPermissions = _contentService.GetPermissionsForEntity(entity)
                    .Where(x => x.AssignedPermissions.Count() > 1 || x.AssignedPermissions[0] != "-");

            foreach (var entityPermission in entityPermissions)
            {
                // If only the default permission then there will only be one element which will contain "F" (Browse Node)
                // This is effectively a Deny permission for editing.
                if (entityPermission.AssignedPermissions.Count() == 1 && entityPermission.AssignedPermissions[0] == BROWSE_NODE)
                {
                    groupIdsWithDenyPermission.Add(entityPermission.UserGroupId);
                }

                if (entityPermission.AssignedPermissions[0].Contains(UPDATE) && !groupIdsWithDenyPermission.Contains(entityPermission.UserGroupId))
                {
                    groupIdsWithAllowPermission.Add(entityPermission.UserGroupId);
                }
            }

            // Permissions in Umbraco are inherited from ancestor nodes, so look up the tree for further permissions
            entity = _contentService.GetById(entity.ParentId);
            if (entity != null)
            {
                GetPermissionsForNodeWithInheritance(entity, groupIdsWithAllowPermission, groupIdsWithDenyPermission);
            }
        }
    }
}