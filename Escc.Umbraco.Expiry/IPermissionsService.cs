using System.Collections.Generic;

namespace Escc.Umbraco.Expiry
{
    /// <summary>
    /// Gets information on permissions to an instance of Umbraco
    /// </summary>
    public interface IPermissionsService
    {
        /// <summary>
        /// Gets the id and email address of users in the specified group who are approved and not locked out or disabled
        /// </summary>
        /// <param name="groupId"></param>
        /// <returns></returns>
        IList<UmbracoUser> ActiveUsersInGroup(int groupId);

        /// <summary>
        /// Gets the ids of all user groups with permissions to a page
        /// </summary>
        /// <param name="pageId"></param>
        /// <returns></returns>
        List<int> GroupsWithPermissionsForPage(int pageId);
    }
}