using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;

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
        public IList<UmbracoPagesForUser> GetExpiringPagesByUser(int inTheNextHowManyDays)
        {
            var response = _client.GetAsync(string.Format("CheckForExpiringNodes?inTheNextHowManyDays={0}", inTheNextHowManyDays)).Result;
            if (!response.IsSuccessStatusCode) throw new WebException(((int)response.StatusCode).ToString(CultureInfo.InvariantCulture) + " " + response.ReasonPhrase);
            var pages = response.Content.ReadAsAsync<IList<UmbracoPage>>().Result;

            // For each page:
            IList<UmbracoPagesForUser> allUsersPages = new List<UmbracoPagesForUser>();

            // Create a admin account record. Use -1 as an Id as there won't be a valid Umbraco user with that value.
            var admin = new UmbracoPagesForUser();
            var adminUser = new UmbracoUser
            {
                UserId = -1,
                EmailAddress = ConfigurationManager.AppSettings["AdminEmail"]
            };
            admin.User = adminUser;
            allUsersPages.Add(admin);

            foreach (var page in pages)
            {
                response = _client.GetAsync(string.Format("PermissionsForPage?pageId={0}", page.PageId)).Result;
                if (!response.IsSuccessStatusCode) throw new WebException(((int)response.StatusCode).ToString(CultureInfo.InvariantCulture) + " " + response.ReasonPhrase);
                var userIdsWithPermission = response.Content.ReadAsAsync<IEnumerable<int>>().Result;

                // if no Web Authors, add this page to the WebStaff list
                if (!userIdsWithPermission.Any())
                {

                    foreach (var adminPages in allUsersPages.Where(p => p.User.UserId == -1))
                    {
                        adminPages.Pages.Add(page);
                    }
                    continue;
                }

                // if all Authors of a page are disabled, add page to the webStaff list
                List<int> disabledUsers = new List<int>();
                foreach (var userId in userIdsWithPermission)
                {
                    response = _client.GetAsync(string.Format("UserById?userId={0}", userId)).Result;
                    if (!response.IsSuccessStatusCode) throw new WebException(((int)response.StatusCode).ToString(CultureInfo.InvariantCulture) + " " + response.ReasonPhrase);
                    var user = response.Content.ReadAsAsync<UmbracoUser>().Result;

                    // Check that this author is not Disabled / Locked Out
                    if (user.IsApproved)
                    {
                        var userPages = allUsersPages.FirstOrDefault(f => f.User.UserId == userId);

                        if (userPages == null)
                        {
                            userPages = new UmbracoPagesForUser { User = user };
                            allUsersPages.Add(userPages);
                        }
                    }
                    else
                    { 
                        disabledUsers.Add(userId);
                    }
                }

                if (disabledUsers.Count == userIdsWithPermission.Count())
                {
                    admin.Pages.Add(page);
                    continue;
                }

                // Add the current page to each user that has edit rights
                foreach (var userId in userIdsWithPermission)
                {
                    var user = allUsersPages.FirstOrDefault(f => f.User.UserId == userId);
                    if (user != null)
                    {
                        user.Pages.Add(page);
                    }
                }
            }

            // Return a list of users to email, along with the page details
            return allUsersPages;
        }
    }
}