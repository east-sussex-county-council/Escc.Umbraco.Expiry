using Examine;
using Exceptionless;
using Lucene.Net.Documents;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using Umbraco.Core;
using Umbraco.Core.Logging;
using UmbracoExamine;

namespace Escc.Umbraco.Expiry
{
    /// <summary>
    /// Make the expiry date for a page available to Examine queries using the External indexer
    /// </summary>
    /// <seealso cref="Umbraco.Core.IApplicationEventHandler" />
    public class SaveExpiryDateToExamineEventHandler : IApplicationEventHandler
    {
        /// <summary>
        /// ApplicationContext is created and other static objects that require initialization have been setup
        /// </summary>
        /// <param name="umbracoApplication"></param>
        /// <param name="applicationContext"></param>
        public void OnApplicationInitialized(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
        }

        /// <summary>
        /// Bootup is completed, this allows you to perform any other bootup logic required for the application.
        /// Resolution is frozen so now they can be used to resolve instances.
        /// </summary>
        /// <param name="umbracoApplication"></param>
        /// <param name="applicationContext"></param>
        public void OnApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            ((UmbracoContentIndexer)ExamineManager.Instance.IndexProviderCollection["ExternalIndexer"]).DocumentWriting += SaveExpiryDateToExamine_DocumentWriting;
        }

        private void SaveExpiryDateToExamine_DocumentWriting(object sender, global::Examine.LuceneEngine.DocumentWritingEventArgs e)
        {
            try
            {
                if (e.Fields["__IndexType"] == "content")
                {
                    // Save the expiry date in a format which allows Examine to do range queries.
                    // DateTime.MaxValue is used as a proxy for never expire, so that those pages have a value in the index which can be queried.
                    // Using DocumentWriting and a NOT_ANALYZED field should enable sorting using Lucene, but it doesn't work. It does insert the value correctly though.
                    var contentService = ApplicationContext.Current.Services.ContentService;
                    var content = contentService.GetById(e.NodeId);
                    var expireDate = content.ExpireDate ?? DateTime.MaxValue;
                    var field = new Field("expireDate", expireDate.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture), Field.Store.YES, Field.Index.NOT_ANALYZED);
                    e.Document.Add(field);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error<Exception>($"Error saving expiry date to Examine for node {e.NodeId}", ex);
                ex.ToExceptionless().Submit();
            }
        }

        /// <summary>
        /// All resolvers have been initialized but resolution is not frozen so they can be modified in this method
        /// </summary>
        /// <param name="umbracoApplication"></param>
        /// <param name="applicationContext"></param>
        public void OnApplicationStarting(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
        }
    }
}