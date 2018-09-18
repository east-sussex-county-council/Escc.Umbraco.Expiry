﻿using System;
using Umbraco.Core;
using Umbraco.Core.Events;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Publishing;
using Umbraco.Core.Services;

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
                var expiryRuleProvider = new ExpiryRulesFromConfig();
                if (expiryRuleProvider.IsEnabled)
                {
                    // Get default time period. Expiry time will be the same as the node creation time.
                    var defaultMaximumDate = DateTime.Now.Add(expiryRuleProvider.DefaultMaximumExpiry);

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


                        if (entity.ExpireDate.HasValue)
                        {
                            // Check for override
                            var expiryRule = ruleEvaluator.CheckOverride(expiryRuleProvider, entity);

                            // Date not allowed because there is an override with no date
                            if (expiryRule != null && !expiryRule.MaximumExpiry.HasValue)
                            {
                                e.CancelOperation(new EventMessage("Publish Failed", "You cannot enter an 'Unpublish at' date for this page", EventMessageType.Error));
                            }

                            // Date cannot be more than a set timespan into the future
                            else
                            {
                                var maximumDate = expiryRule != null && expiryRule.MaximumExpiry.HasValue ? DateTime.Now.Add(expiryRule.MaximumExpiry.Value) : defaultMaximumDate;
                                if (entity.ExpireDate > maximumDate)
                                {
                                    // Default the date to the maximum allowed and continue publishing.
                                    entity.ExpireDate = maximumDate;
                                    e.Messages.Add(new EventMessage("Warning", "The 'Unpublish at' date is too far into the future. The date has been set to: " + DisplayDate(defaultMaximumDate) + ". You can refresh the page to see the new date.", EventMessageType.Warning));
                                }
                            }
                        }
                        else
                        {
                            // Check for no override
                            if (ruleEvaluator.CheckOverride(expiryRuleProvider, entity) == null)
                            {
                                // Date is required as no override exists
                                // As no date has been provided and there is no override, default the date to the maximum allowed and continue publishing.
                                entity.ExpireDate = defaultMaximumDate;
                                e.Messages.Add(new EventMessage("Warning", "The 'Unpublish at' date is a required field. The date has been set to " + DisplayDate(defaultMaximumDate) + ". You can refresh the page to see the new date.", EventMessageType.Warning));
                            }

                            // No date is OK because there is an override
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

        private static string DisplayDate(DateTime defaultMaximumDate)
        {
            return defaultMaximumDate.ToString("dd MMMM yyyy") + defaultMaximumDate.ToString(" h.mmtt").ToLower();
        }
    }
}
