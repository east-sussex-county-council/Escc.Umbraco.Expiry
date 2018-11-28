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
        /// Checks for pages expiring soon and those due never to expire
        /// </summary>
        /// <param name="inTheNextHowManyDays">The date range, beginning today, during which the pages we want to know about are due to expire.</param>
        /// <returns></returns>
        [HttpGet]
        public HttpResponseMessage CheckForExpiringPages(int inTheNextHowManyDays)
        {
            try
            {
                var expiringPagesService = new ExaminePageExpiryService(ExamineManager.Instance.SearchProviderCollection["ExternalSearcher"],  Umbraco);
                var nodes = expiringPagesService.GetExpiringPages(inTheNextHowManyDays);

                return Request.CreateResponse(HttpStatusCode.OK, nodes);
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().Submit();
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        /// <summary>
        /// Gets the ids of all user groups with permissions to a page
        /// </summary>
        /// <param name="pageId"></param>
        /// <returns></returns>
        [HttpGet]
        public HttpResponseMessage GroupsWithPermissionsForPage(int pageId)
        {
            try
            {
                var expiringPagesService = new UmbracoPermissionsService(Services.UserService, Services.ContentService);
                var nodes = expiringPagesService.GroupsWithPermissionsForPage(pageId);

                return Request.CreateResponse(HttpStatusCode.OK, nodes);
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().Submit();
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }


        /// <summary>
        /// Gets the ids and emails of all users in a group
        /// </summary>
        /// <param name="groupId"></param>
        /// <returns></returns>
        [HttpGet]
        public HttpResponseMessage ActiveUsersInGroup(int groupId)
        {
            try
            {
                var expiringPagesService = new UmbracoPermissionsService(Services.UserService, Services.ContentService);
                var nodes = expiringPagesService.ActiveUsersInGroup(groupId);

                return Request.CreateResponse(HttpStatusCode.OK, nodes);
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
          /// <summary>
        /// Sets or removes the unpublish date for child nodes.
        /// </summary>
        /// <param name="node">The node.</param>
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
            var expiryRuleProvider = new ExpiryRulesFromUmbraco(UmbracoContext.ContentCache);
            var expiryRule = new ExpiryRuleEvaluator().MatchExpiryRules(new DocumentTypeRuleMatcher(expiryRuleProvider.DocumentTypeRules), new PathRuleMatcher(expiryRuleProvider.PathRules), publishedContent);

            try
            {

                var unpublishDate = new ExpiryDateFromExamine(publishedContent.Id, ExamineManager.Instance.SearchProviderCollection["ExternalSearcher"]).ExpiryDate;
                if (unpublishDate.HasValue && expiryRule != null && !expiryRule.MaximumExpiry.HasValue)
                {
                    // Page should never expire
                    var node = contentService.GetById(publishedContent.Id);
                    node.ExpireDate = null;
                    contentService.SaveAndPublishWithStatus(node);
                }
                else if (!unpublishDate.HasValue && (expiryRule == null || expiryRule.MaximumExpiry.HasValue))
                {
                    var node = contentService.GetById(publishedContent.Id);
                    if (expiryRule != null && expiryRule.MaximumExpiry.HasValue)
                    {
                        node.ExpireDate = DateTime.Now.Add(expiryRule.MaximumExpiry.Value);
                       contentService.SaveAndPublishWithStatus(node);
                    }
                    else if (expiryRuleProvider.DefaultMaximumExpiry.HasValue)
                    {
                        node.ExpireDate = DateTime.Now.Add(expiryRuleProvider.DefaultMaximumExpiry.Value);
                        contentService.SaveAndPublishWithStatus(node);
                    }
                }
            }
            catch (Exception e)
            {
                e.ToExceptionless().Submit();
            }
        }
    }
}