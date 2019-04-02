using Exceptionless;
using log4net;
using log4net.Config;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace Escc.Umbraco.Expiry.Notifier
{
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));

        static async Task Main(string[] args)
        {
            try
            {
                ExceptionlessClient.Current.Startup();
                XmlConfigurator.Configure();

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

                var users = await dataSource.GetExpiringPagesByUser(inTheNextHowManyDays);

                log.Info("Starting expiry email process");

                SendEmailToWebAuthors(emailService, logRepository, users);

                SendEmailToAdmin(emailService, users);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                ex.ToExceptionless().Submit();
            }
            finally
            {
                ExceptionlessClient.Current.ProcessQueue();
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
                    if (sentTo != null) log.Info("Warning email sent to: " + sentTo);
                }
                catch (Exception ex)
                {
                    log.Error("Failure sending warning email to admins - " + ex.Message);
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
                    string sentTo = null;
                    try
                    {
                        sentTo = emailService.UserPageExpiryEmail(user);
                        if (!String.IsNullOrEmpty(sentTo))
                        {
                            log.Info("Expiry email sent to: " + sentTo);
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error("Failure sending email to:" + user.User.Email + " - " + ex.Message);
                        ex.ToExceptionless().Submit();
                    }

                    try
                    {
                        if (!String.IsNullOrEmpty(sentTo))
                        {
                            var jsonPages = JsonConvert.SerializeObject(user.Pages);
                            logRepository.SetExpiryLogDetails(new ExpiryLogEntry(0, sentTo, DateTime.Now, true, jsonPages));
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error("Failure saving to expiry log: " + ex.Message);
                        ex.ToExceptionless().Submit();
                    }
                }
            }
        }
    }
}
