﻿using System;
using System.Collections.Generic;
using System.Configuration;
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

        public IList<UmbracoPagesForUser> GetExpiringPagesByUser(int inTheNextHowManyDays)
        {
            var response = _client.GetAsync(string.Format("CheckForExpiringNodesByUser?inTheNextHowManyDays={0}", inTheNextHowManyDays)).Result;

            if (!response.IsSuccessStatusCode) return null;
            var modelList = response.Content.ReadAsAsync<IList<UmbracoPagesForUser>>().Result;
            return modelList;
        }
    }
}