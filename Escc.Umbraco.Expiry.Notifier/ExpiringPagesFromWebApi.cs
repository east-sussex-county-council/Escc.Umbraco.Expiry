using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Escc.Umbraco.Expiry.Notifier
{
    /// <summary>
    /// Reads the information on pages due to expire from a web API, with an interface that matches <see cref="ExpiryController"/>
    /// </summary>
    /// <seealso cref="Escc.Umbraco.Expiry.Notifier.IExpiringPagesProvider" />
    public class ExpiringPagesFromWebApi : IExpiringPagesProvider
    {
        private readonly HttpClient _client;

        public ExpiringPagesFromWebApi()
        {
            var siteUri = ConfigurationManager.AppSettings["SiteUri"];

            siteUri = string.Format("{0}Api/Expiry/", siteUri);
            var handler = new HttpClientHandler
            {
                Credentials =
                    new NetworkCredential(ConfigurationManager.AppSettings["apiuser"],
                        ConfigurationManager.AppSettings["apikey"])
            };

            // Set a long timeout because some queries have to check all pages and can take a long time
            _client = new HttpClient(handler) { BaseAddress = new Uri(siteUri), Timeout = TimeSpan.FromMinutes(5)};
        }

        /// <summary>
        /// Gets the pages due to expire in the next <c>inTheNextHowManyDays</c> days, collated by the users responsible for the pages.
        /// </summary>
        /// <param name="inTheNextHowManyDays">The date range to look for expiring pages, based on the number of days from today.</param>
        /// <returns></returns>
        /// <exception cref="WebException"></exception>
        public async Task<IList<UmbracoPagesForUser>> GetExpiringPagesByUser(int inTheNextHowManyDays)
        {
            var response = await _client.GetAsync(string.Format("CheckForExpiringPages?inTheNextHowManyDays={0}", inTheNextHowManyDays));
            if (!response.IsSuccessStatusCode) throw new WebException(((int)response.StatusCode).ToString(CultureInfo.InvariantCulture) + " " + response.ReasonPhrase);
            var pages = await response.Content.ReadAsAsync<IEnumerable<UmbracoPage>>();

            // For each page:
            var allUsersWithPages = new Dictionary<int, UmbracoPagesForUser>();

            // Create a admin account record. Use -1 as an Id as there won't be a valid Umbraco user with that value.
            var admin = new UmbracoPagesForUser()
            {
                User = new UmbracoUser
                {
                    Id = -1,
                    Email = ConfigurationManager.AppSettings["AdminEmail"]
                }
            };
            allUsersWithPages.Add(admin.User.Id, admin);

            foreach (var userPage in pages)
            {
                response = await _client.GetAsync(string.Format("GroupsWithPermissionsForPage?pageId={0}", userPage.PageId));
                if (!response.IsSuccessStatusCode) throw new WebException(((int)response.StatusCode).ToString(CultureInfo.InvariantCulture) + " " + response.ReasonPhrase);
                var groupsWithPermissionsForNode = await response.Content.ReadAsAsync<IEnumerable<int>>();

                var usersInGroups = new Dictionary<int, IList<UmbracoUser>>();

                // If there are no active users with permissions to the page, add the page to the admin list
                var pageHasActiveUserWithPermissions = false;
                foreach (var groupId in groupsWithPermissionsForNode)
                {
                    if (!usersInGroups.ContainsKey(groupId))
                    {
                        response = await _client.GetAsync(string.Format("ActiveUsersInGroup?groupId={0}", groupId));
                        if (!response.IsSuccessStatusCode) throw new WebException(((int)response.StatusCode).ToString(CultureInfo.InvariantCulture) + " " + response.ReasonPhrase);
                        usersInGroups[groupId] = await response.Content.ReadAsAsync<IList<UmbracoUser>>();
                    }
                    pageHasActiveUserWithPermissions = (pageHasActiveUserWithPermissions || usersInGroups[groupId].Count > 0);
                }
                if (!pageHasActiveUserWithPermissions)
                {
                    admin.Pages.Add(userPage);
                    continue;
                }

                // Add the current page to each user that has edit rights
                foreach (var groupId in groupsWithPermissionsForNode)
                {
                    foreach (var user in usersInGroups[groupId])
                    {
                        // Create a User record if one does not yet exist
                        if (!allUsersWithPages.ContainsKey(user.Id))
                        { 
                            var pagesForUser = new UmbracoPagesForUser
                            {
                                User = user
                            };
                            allUsersWithPages.Add(user.Id, pagesForUser);
                        }

                        // Assign the current page to this author, unless they already have it 
                        // through their membership of another group.
                        if (!allUsersWithPages[user.Id].Pages.Any(page => page.PageId == userPage.PageId))
                        {
                            allUsersWithPages[user.Id].Pages.Add(userPage);
                        }
                    }
                }
            }

            // Return a list of users responsible, along with the page details
            return allUsersWithPages.Values.ToList();
        }
    }
}