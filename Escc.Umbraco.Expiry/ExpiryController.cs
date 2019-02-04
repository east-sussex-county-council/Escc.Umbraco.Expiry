using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web.Http;
using Exceptionless;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Web;
using Umbraco.Web.WebApi;
using System.Net;
using System.Configuration;
using Examine;

namespace Escc.Umbraco.Expiry
{
    /// <summary>
    /// API to report on Umbraco content expiry dates, and manipulate them according to the policy specified in the &lt;UnpublishOverrides&gt; section of web.config
    /// </summary>
    [Authorize]
    public class ExpiryController : UmbracoApiController
    {
        /// <summary>
        /// Checks for expiring pages, collated according to the user responsible for them
        /// </summary>
        /// <param name="inTheNextHowManyDays">The date range, beginning today, during which the pages we want to know about are due to expire.</param>
        /// <returns></returns>
        [HttpGet]
        [Obsolete("Takes too long on large sites. Use GetExpiringNodes(), PermissionsForPage() and UserId() instead.")]
        public HttpResponseMessage CheckForExpiringNodesByUser(int inTheNextHowManyDays)
        {
            try
            {
                var expiringPagesService = new ExaminePageExpiryService(ExamineManager.Instance.SearchProviderCollection["ExternalSearcher"], Services.UserService, Services.ContentService,  Umbraco, ConfigurationManager.AppSettings["AdminAccountName"], ConfigurationManager.AppSettings["AdminAccountEmail"]);
                var nodes = expiringPagesService.GetExpiringNodesByUser(inTheNextHowManyDays);

                return Request.CreateResponse(HttpStatusCode.OK, nodes);
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().Submit();
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        /// <summary>
        /// Checks for expiring pages, collated according to the user responsible for them
        /// </summary>
        /// <param name="inTheNextHowManyDays">The date range, beginning today, during which the pages we want to know about are due to expire.</param>
        /// <returns></returns>
        [HttpGet]
        public HttpResponseMessage CheckForExpiringNodes(int inTheNextHowManyDays)
        {
            try
            {
                var expiringPagesService = new ExaminePageExpiryService(ExamineManager.Instance.SearchProviderCollection["ExternalSearcher"], Services.UserService, Services.ContentService, Umbraco, ConfigurationManager.AppSettings["AdminAccountName"], ConfigurationManager.AppSettings["AdminAccountEmail"]);
                var nodes = expiringPagesService.GetExpiringNodes(inTheNextHowManyDays);

                return Request.CreateResponse(HttpStatusCode.OK, nodes);
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().Submit();
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        /// <summary>
        /// Gets the permissions information for a page
        /// </summary>
        /// <param name="pageId">The id of the content node.</param>
        /// <returns>Ids of users with permissions for the page</returns>
        [HttpGet]
        public HttpResponseMessage PermissionsForPage(int pageId)
        {
            try
            {
                var expiringPagesService = new ExaminePageExpiryService(ExamineManager.Instance.SearchProviderCollection["ExternalSearcher"], Services.UserService, Services.ContentService, Umbraco, ConfigurationManager.AppSettings["AdminAccountName"], ConfigurationManager.AppSettings["AdminAccountEmail"]);
                var permissions = expiringPagesService.PermissionsForPage(pageId);

                return Request.CreateResponse(HttpStatusCode.OK, permissions);
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().Submit();
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        /// <summary>
        /// Gets email and approval status of an Umbraco user
        /// </summary>
        /// <param name="userId">The id of the user.</param>
        /// <returns></returns>
        [HttpGet]
        public HttpResponseMessage UserById(int userId)
        {
            try
            {
                var expiringPagesService = new ExaminePageExpiryService(ExamineManager.Instance.SearchProviderCollection["ExternalSearcher"], Services.UserService, Services.ContentService, Umbraco, ConfigurationManager.AppSettings["AdminAccountName"], ConfigurationManager.AppSettings["AdminAccountEmail"]);
                var user = expiringPagesService.UserById(userId);

                return Request.CreateResponse(HttpStatusCode.OK, user);
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().Submit();
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        /// <summary>
        /// Ensures the unpublish dates for all published content match the policy specified in web.config
        /// </summary>
        [HttpPost]
        public void EnsureUnpublishDatesMatchPolicy()
        {
            var rootnodes = UmbracoContext.ContentCache.GetAtRoot();

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = Request.Headers.Authorization;


            foreach (var node in rootnodes)
            {
                SetOrRemoveUnpublishDate(node);

                LogHelper.Info<ExpiryController>($"Starting firing unpublish dates for node {node.Id} {node.Name}");
                client.PostAsync(Request.RequestUri.ToString().Replace("EnsureUnpublishDatesMatchPolicy", "SetOrRemoveUnpublishDateForChildNodes") + "?parentNodeId=" + node.Id, new StringContent(String.Empty));
                LogHelper.Info<ExpiryController>($"Completed firing unpublish dates for node {node.Id} {node.Name}");
            }
        }

        [HttpPost]
        public void SetOrRemoveUnpublishDateForChildNodes(int parentNodeId)
        {

            var parentNode = UmbracoContext.ContentCache.GetById(parentNodeId);

            if (parentNode != null)
            {
                LogHelper.Info<ExpiryController>($"Starting updating unpublish dates for node {parentNode.Id} {parentNode.Name}");
                SetOrRemoveUnpublishDateForChildNodes(parentNode);
                LogHelper.Info<ExpiryController>($"Completed updating unpublish dates for node {parentNode.Id} {parentNode.Name}");
            }
        }

        /// <summary>
        /// Sets or removes the unpublish date for child nodes.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <remarks>Use recursion rather than .Descendants() to avoid generating a long-running query that times out when there's a lot of content</remarks>
        private void SetOrRemoveUnpublishDateForChildNodes(IPublishedContent node)
        {
            foreach (var child in node.Children())
            {
                SetOrRemoveUnpublishDate(child);
                if (node.Level > 2)
                {
                    SetOrRemoveUnpublishDateForChildNodes(child);
                }
                else
                {
                    LogHelper.Info<ExpiryController>($"Starting firing unpublish dates for node {child.Id} {child.Name}");
                    HttpClient client = new HttpClient();
                    client.DefaultRequestHeaders.Authorization = Request.Headers.Authorization;
                    client.PostAsync(Regex.Replace(Request.RequestUri.ToString(), "parentNodeId=[0-9]+", "parentNodeId=" + child.Id), new StringContent(String.Empty));
                    LogHelper.Info<ExpiryController>($"Completed firing unpublish dates for node {child.Id} {child.Name}");
                }
            }

        }

        private void SetOrRemoveUnpublishDate(IPublishedContent publishedContent)
        {
            var contentService = ApplicationContext.Current.Services.ContentService;
            var shouldBeNever = ExpiryRules.CheckOverride(publishedContent);

            try
            {

                var unpublishDate = new ExpiryDateFromExamine(publishedContent.Id, ExamineManager.Instance.SearchProviderCollection["ExternalSearcher"], new ExpiryDateMemoryCache(TimeSpan.FromHours(1))).ExpiryDate;
                if (unpublishDate.HasValue && shouldBeNever)
                {
                    var node = contentService.GetById(publishedContent.Id);
                    node.ExpireDate = null;
                    contentService.SaveAndPublishWithStatus(node);
                }
                else if (!unpublishDate.HasValue && !shouldBeNever)
                {
                    var node = contentService.GetById(publishedContent.Id);
                    node.ExpireDate = DateTime.Now.AddMonths(6);
                    contentService.SaveAndPublishWithStatus(node);
                }
            }
            catch (Exception e)
            {
                e.ToExceptionless().Submit();
            }
        }
    }
}