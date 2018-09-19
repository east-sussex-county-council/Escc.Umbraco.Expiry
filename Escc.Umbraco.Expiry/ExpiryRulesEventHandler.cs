using System;
using Umbraco.Core;
using Umbraco.Core.Events;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Publishing;
using Umbraco.Core.Services;
using Umbraco.Web;

namespace Escc.Umbraco.Expiry
{
    /// <summary>
    /// Wire up events which enforce page expiry rules
    /// </summary>
    class ExpiryRulesEventHandler : IApplicationEventHandler
    {
        public void OnApplicationInitialized(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
        }

        public void OnApplicationStarting(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            // Check that node is OK to publish
            ContentService.Publishing += ContentService_Publishing;
        }

        public void OnApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
        }

        static void ContentService_Publishing(IPublishingStrategy sender, PublishEventArgs<IContent> e)
        {
            // Check if the node has an override for the UnPublish date.
            // Need to check in the Publishing event as the URL is not assigned until now.
            try
            {
                var expiryRuleProvider = new ExpiryRulesFromUmbraco(UmbracoContext.Current.ContentCache);
                if (expiryRuleProvider.IsEnabled)
                {
                    // Create the rule evaluator
                    var ruleEvaluator = new ExpiryRuleEvaluator();

                    // Check if there is an override for this content element. 
                    // If not, check that the unPublish date is within allowed date range.
                    foreach (var entity in e.PublishedEntities)
                    {
                        if (entity.Id == 0)
                        {
                            // Do a save to get the Id and other info
                            ApplicationContext.Current.Services.ContentService.Save(entity);
                        }

                        var result = ruleEvaluator.ApplyExpiryRules(DateTime.Now, expiryRuleProvider.DefaultMaximumExpiry, new DocumentTypeRuleMatcher(expiryRuleProvider.DocumentTypeRules), new PathRuleMatcher(expiryRuleProvider.PathRules), entity, new NodeUrlBuilder());

                        if (!String.IsNullOrEmpty(result.CancellationMessage))
                        {
                            e.CancelOperation(new EventMessage("Publish Failed", result.CancellationMessage, EventMessageType.Error));
                            return;
                        }

                        if (!String.IsNullOrEmpty(result.ExpireDateChangedMessage))
                        {
                            entity.ExpireDate = result.ExpireDate;
                            e.Messages.Add(new EventMessage("Warning", result.ExpireDateChangedMessage, EventMessageType.Warning));
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error<ExpiryRulesEventHandler>("Error checking page expiry date.", ex);
                e.CancelOperation(new EventMessage("Publish Failed", ex.Message, EventMessageType.Error));
            }
        }
    }
}
