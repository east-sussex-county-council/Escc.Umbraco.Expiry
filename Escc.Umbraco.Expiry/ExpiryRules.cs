using Escc.Umbraco.Expiry.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Web;
using Umbraco.Web.Models.ContentEditing;
using Umbraco.Web.Security;

namespace Escc.Umbraco.Expiry
{
    class ExpiryRules
    {
        // ContentTypes
        public static IEnumerable<UnpublishOverridesContentTypeElement> ContentTypeList { get { return ContentTypes; }}
        private static readonly List<UnpublishOverridesContentTypeElement> ContentTypes = new List<UnpublishOverridesContentTypeElement>();

        // Paths
        public static IEnumerable<UnpublishOverridesPathElement> PathList { get { return Paths; }}
        private static readonly List<UnpublishOverridesPathElement> Paths = new List<UnpublishOverridesPathElement>();

        public static bool IsEnabled { get { return CheckEnabled(); } }

        /// <summary>
        /// Initializes the <see cref="ExpiryRules"/> class.
        /// </summary>
        static ExpiryRules()
        {
            GetContentTypes();
            GetPaths();
        }

        /// <summary>
        /// Check if an override exists
        /// </summary>
        /// <param name="contentItem">Content Item to check</param>
        /// <returns>True if an override exists</returns>
        public static bool CheckOverride(IContent contentItem)
        {
            if (contentItem == null) return false;

            // Check for a ContentType override
            if (DocTypeOverride(contentItem.ContentType.Alias, contentItem.Level.ToString())) return true;

            // Check for an override based on the Url
            if (UrlOverride(GetNodeUrl(contentItem).ToLower())) return true;

            return false;
        }

        /// <summary>
        /// Used where IContent is not available
        /// </summary>
        /// <param name="contentItem">Content Item to Check</param>
        /// <returns>True if an override exists</returns>
        public static bool CheckOverride(ContentItemDisplay contentItem)
        {
            var contentService = ApplicationContext.Current.Services.ContentService;
            var content = contentService.GetById(contentItem.Key);

            return CheckOverride(content);
        }

        /// <summary>
        /// Used where IContent is not available
        /// </summary>
        /// <param name="contentItem">Content Item to Check</param>
        /// <returns>True if an override exists</returns>
        public static bool CheckOverride(IPublishedContent contentItem)
        {
            if (contentItem == null) return false;

            // Check for a ContentType override
            if (DocTypeOverride(contentItem.ContentType.Alias, contentItem.Level.ToString())) return true;

            // Check for an override based on the Url
            if (UrlOverride(contentItem.Url.ToLower())) return true;

            return false;
        }

        /// <summary>
        /// Check for and process an override based on the page Url
        /// </summary>
        /// <param name="pagePath">The Url to check</param>
        /// <returns>True if an override exists</returns>
        private static bool UrlOverride(string pagePath)
        {
            // If a page path was not passed, return false (no override)
            if (string.IsNullOrEmpty(pagePath)) return false;

            // Look for the specific Url in the overrides list
            if (Paths.Any(n => n.Name == pagePath)) return true;

            // ================================================================
            // pagePath may be a child of an override Url
            // E.g.
    		// <add name="/educationandlearning/schools/" children="" />
		    // <add name="/educationandlearning/schools/findingaschool/" children="*" />
            // 
            // pagePath = /educationandlearning/schools/findingaschool/mynewpage
            //
            // Second item is the most specific, find it by ordering the Paths by the number of '/' characters in the string
            // ================================================================
            var entry = Paths.Where(n => pagePath.StartsWith(n.Name)).OrderByDescending(c => c.Name.Count(f => f == '/')).FirstOrDefault();

            // No override(s) found
            if (entry == null) return false;

            // The override applies to all children too, so return true
            if (entry.Children == "*") return true;

            // All other tests failed, so return false (no override)
            return false;
        }

        /// <summary>
        /// Get or construct the node Url
        /// </summary>
        /// <param name="node">Node to process</param>
        /// <returns>Node Url</returns>
        private static string GetNodeUrl(IContent node)
        {
            // Make sure we have a current Umbraco Context
            if (UmbracoContext.Current == null)
            {
                var dummyContext = new HttpContextWrapper(new HttpContext(new SimpleWorkerRequest("/", string.Empty, new StringWriter())));
                UmbracoContext.EnsureContext(
                    dummyContext,
                    ApplicationContext.Current,
                    new WebSecurity(dummyContext, ApplicationContext.Current),
                    false);
            }
            var helper = new UmbracoHelper(UmbracoContext.Current);

            var entityUrl = helper.NiceUrl(node.Id);

            if (!string.IsNullOrEmpty(entityUrl) && entityUrl != "#") return entityUrl;

            // Just need the Url of the parent node...
            entityUrl = helper.Url(node.ParentId);
            if (entityUrl == "#") entityUrl = string.Empty;
            if (!entityUrl.EndsWith("/")) entityUrl += "/";

            // Then add the current node name
            var nodeName = node.Name;
            if (node.HasProperty("umbracoUrlName") && !string.IsNullOrEmpty(node.GetValue<string>("umbracoUrlName")))
            {
                nodeName = node.GetValue<string>("umbracoUrlName");
            }

            nodeName = umbraco.cms.helpers.url.FormatUrl(nodeName);
            entityUrl = string.Format("{0}{1}/", entityUrl, nodeName);

            return entityUrl;
        }

        /// <summary>
        /// Check for and process an override based on the page Doc Type
        /// </summary>
        /// <param name="doctypeAlias">Doc Type to check</param>
        /// <param name="templateLevel">Optional level to check at</param>
        /// <returns>True if an override exists</returns>
        private static bool DocTypeOverride(string doctypeAlias, string templateLevel)
        {
            // If a template name was not passed, return false (no override)
            if (string.IsNullOrEmpty(doctypeAlias)) return false;

            // Look for the template name in the overrides list
            var contenttype = ContentTypeList.FirstOrDefault(n => n.Name == doctypeAlias);

            // If template not found then return false (no override)
            if (contenttype == null) return false;

            // If a specific level was not provided, then the override applies at any level
            if (string.IsNullOrEmpty(templateLevel)) return true;

            // If the template override applies at a specific level
            if (contenttype.Level != "*")
            {
                return contenttype.Level == templateLevel;
            }

            // Template name found and applies at all levels
            return true;
        }

        private static void GetContentTypes()
        {
            var unpublishOverridesSection = ConfigurationManager.GetSection(UnpublishOverridesSection.SectionName) as UnpublishOverridesSection;
            if (unpublishOverridesSection != null)
            {
                foreach (UnpublishOverridesContentTypeElement templateElement in unpublishOverridesSection.ContentTypes)
                {
                    var template = new UnpublishOverridesContentTypeElement { Name = templateElement.Name, Level = templateElement.Level };
                    AddTemplate(template);
                }
            }
        }

        private static void AddTemplate(UnpublishOverridesContentTypeElement templateElement)
        {
            if (templateElement == null)
                return;

            if (!ContentTypes.Contains(templateElement))
                ContentTypes.Add(templateElement);
        }

        private static void GetPaths()
        {
            var unpublishOverridesSection = ConfigurationManager.GetSection(UnpublishOverridesSection.SectionName) as UnpublishOverridesSection;
            if (unpublishOverridesSection != null)
            {
                foreach (UnpublishOverridesPathElement pathElement in unpublishOverridesSection.Paths)
                {
                    var path = new UnpublishOverridesPathElement
                    {
                        Name = pathElement.Name.ToLower(),
                        Children = pathElement.Children
                    };
                    AddPath(path);
                }
            }
        }

        private static void AddPath(UnpublishOverridesPathElement pathElement)
        {
            if (pathElement == null)
                return;

            if (!Paths.Contains(pathElement))
                Paths.Add(pathElement);
        }

        private static bool CheckEnabled()
        {
            var unpublishOverridesSection = ConfigurationManager.GetSection(UnpublishOverridesSection.SectionName) as UnpublishOverridesSection;

            if (unpublishOverridesSection == null) return false;

            return unpublishOverridesSection.Enabled;
        }
    }
}
