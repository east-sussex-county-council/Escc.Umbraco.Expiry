using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Web;
using Umbraco.Web.Security;

namespace Escc.Umbraco.Expiry
{
    public class NodeUrlBuilder : INodeUrlBuilder
    {
        /// <summary>
        /// Get or construct the node Url
        /// </summary>
        /// <param name="node">Node to process</param>
        /// <returns>Node Url</returns>
        public string GetNodeUrl(IContent node)
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
    }
}
