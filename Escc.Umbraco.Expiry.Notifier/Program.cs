using Exceptionless;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace Escc.Umbraco.Expiry.Notifier
{
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger("RollingFileAppender");

        static void Main(string[] args)
        {
            try
            {
                log.Info("Checking for expiring nodes");

                int inTheNextHowManyDays;
                int emailAdminAtDays;
                if (String.IsNullOrEmpty(ConfigurationManager.AppSettings["InTheNextHowManyDays"]) || !int.TryParse(ConfigurationManager.AppSettings["InTheNextHowManyDays"], out inTheNextHowManyDays))
                {
                    inTheNextHowManyDays = 14;
                }
                if (String.IsNullOrEmpty(ConfigurationManager.AppSettings["EmailAdminAtDays"]) || !int.TryParse(ConfigurationManager.AppSettings["EmailAdminAtDays"], out emailAdminAtDays))
                {
                    emailAdminAtDays = 3;
                }

                IExpiringPagesProvider dataSource = new ExpiringPagesFromWebApi();
                IEmailService emailService = new EmailService(emailAdminAtDays, ConfigurationManager.AppSettings["AdminEmail"], ConfigurationManager.AppSettings["ForceSendTo"], ConfigurationManager.AppSettings["WebsiteName"], new Uri(ConfigurationManager.AppSettings["SiteUri"]), new Uri(ConfigurationManager.AppSettings["WebAuthorsGuidanceUrl"]));
                IExpiryLogRepository logRepository = new SqlServerExpiryLogRepository();

                var users = dataSource.GetExpiringPagesByUser(inTheNextHowManyDays);

                log.Info("Starting expiry email process");

                SendEmailToWebAuthors(emailService, logRepository, users);

                SendEmailToAdmin(emailService, users);
            }
            catch (Exception ex)
            {
                log.Error("Process failed - check Exceptionless");
                ex.ToExceptionless().Submit();
            }
        }

        private static void SendEmailToAdmin(IEmailService emailService, IList<UmbracoPagesForUser> users)
        {
            // Check for pages expiring soon and email Web Staff
            var warningList = new List<UmbracoPage>();
            var soonDate = DateTime.Now.AddDays(emailService.EmailAdminAtDays + 1);

            var expiringSoon = users.Where(u => u.Pages.Any(p => p.ExpiryDate <= soonDate));
            foreach (var expiring in expiringSoon)
            {
                // Add the specific pages that will expire soon ... not all of them!
                foreach (var expiringPage in expiring.Pages.Where(p => p.ExpiryDate <= soonDate))
                {
                    // Check we haven't already added this page to the list
                    if (warningList.All(n => n.PageId != expiringPage.PageId))
                    {
                        warningList.Add(expiringPage);
                    }
                }
            }

            if (warningList.Any())
            {
                try
                {
                    var sentTo = emailService.UserPageLastWarningEmail(warningList.OrderBy(o => o.ExpiryDate).ToList());
                    if (sentTo != null) log.Info("Warning Email Sent to: " + sentTo);
                }
                catch (Exception ex)
                {
                    log.Error("Failure sending warning email to admins - Check Exceptionless");
                    ex.ToExceptionless().Submit();
                }
            }
        }

        private static void SendEmailToWebAuthors(IEmailService emailService, IExpiryLogRepository logRepository, IList<UmbracoPagesForUser> users)
        {
            foreach (var user in users)
            {
                if (user.Pages.Any())
                {
                    try
                    {
                        var sentTo = emailService.UserPageExpiryEmail(user);
                        if (!String.IsNullOrEmpty(sentTo))
                        {
                            var jsonPages = JsonConvert.SerializeObject(user.Pages);
                            logRepository.SetExpiryLogDetails(new ExpiryLogEntry(0, sentTo, DateTime.Now, true, jsonPages));
                            log.Info("Expiry Email Sent to: " + sentTo);
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error("Failure sending email to:" + user.User.EmailAddress);
                        var jsonPages = JsonConvert.SerializeObject(user.Pages);
                        logRepository.SetExpiryLogDetails(new ExpiryLogEntry(0, user.User.EmailAddress, DateTime.Now, false, jsonPages));
                        ex.ToExceptionless().Submit();
                    }
                }
            }
        }
    }
}
