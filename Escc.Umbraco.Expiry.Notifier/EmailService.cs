using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using Exceptionless;
using Exceptionless.Extensions;

namespace Escc.Umbraco.Expiry.Notifier
{
    public class EmailService : IEmailService
    {
        private readonly string _forceSendTo;
        private readonly string _websiteName;
        private readonly Uri _websiteUrl;
        private readonly Uri _webAuthorGuidanceUrl;
        public readonly string _adminEmail;
        public int EmailAdminAtDays { get; }

        public EmailService(int emailAdminAtDays, string adminEmail, string forceSendTo, string websiteName, Uri websiteUrl, Uri webAuthorGuidanceUrl)
        {
            _adminEmail = adminEmail;
            _forceSendTo = forceSendTo;
            _websiteName = websiteName;
            _websiteUrl = websiteUrl;
            _webAuthorGuidanceUrl = webAuthorGuidanceUrl;
            EmailAdminAtDays = emailAdminAtDays;
        }

        /// <summary>
        /// Sends and email to the given address
        /// </summary>
        /// <param name="emailTo">Address of user you wish to email</param>
        /// <param name="emailSubject">Subject line of email </param>
        /// <param name="emailBody">Body text of email</param>
        private void SmtpSendEmail(string emailTo, string emailSubject, string emailBody)
        {
            using (var client = new SmtpClient
            {
                UseDefaultCredentials = true
            })
            {
                using (var message = new MailMessage())
                {
                    message.To.Add(emailTo);
                    message.IsBodyHtml = true;
                    message.BodyEncoding = Encoding.UTF8;
                    message.Subject = emailSubject;
                    message.Body = emailBody;

                    try
                    {
                        // send the email
                        client.Send(message);
                    }
                    catch (SmtpException exception)
                    {
                        exception.ToExceptionless().Submit();
                    }
                    catch (InvalidOperationException exception)
                    {
                        exception.ToExceptionless().Submit();
                    }
                }
            }
        }

        /// <summary>
        /// Send expiry warning emails to users (Web Authors) of pages about to expire
        /// </summary>
        /// <param name="emailTo">
        /// who to send the email to
        /// </param>
        /// <param name="userPages">
        /// User details and list of expiring pages for this user
        /// </param>
        public string UserPageExpiryEmail(UmbracoPagesForUser userPages)
        {
            foreach (var item in userPages.Pages)
            {
                if(item.PageUrl == null || item.PageUrl == "#")
                {
                    item.PageUrl = "This page is not visible on the live site.";
                }
            }

            var subject = string.Format("ACTION: Your {0} pages expire in under 14 days", _websiteName);
            var body = new StringBuilder();

            body.AppendFormatLine("<p>Your {0} pages will expire within the next two weeks. After this they will no longer be available to the public. The dates for each page are given below.</p>", _websiteName);
            body.AppendLine("<p>After you’ve logged in, click on each page below and:</p>");
            body.AppendLine("<ul>");
            body.AppendLine("<li>check they are up to date</li>");
            body.AppendLine("<li>check the information is still needed</li>");
            body.AppendLine("<li>go to Properties tab and use the calendar to set a new date in the 'Unpublish at' box</li>");
            body.AppendLine("<li>then click 'Save and publish'.</li>");
            body.AppendLine("</ul>");
            body.AppendLine("<p>For details on updating your pages, see <a href=\"" + _webAuthorGuidanceUrl + "\">Guidance for web authors</a>.</p>");

            var otherTitle = "Expiring Pages:";
            var warningDate = DateTime.Now.AddDays(2);
            var lastWarningPages = userPages.Pages.Where(d => d.ExpiryDate <= warningDate).ToList();
            if (lastWarningPages.Any())
            {
                body.AppendLine("<strong>Pages Expiring Tomorrow:</strong>");
                body.AppendLine("<ol>");
                foreach (var page in lastWarningPages)
                {
                    var linkUrl = string.Format("{0}#/content/content/edit/{1}", _websiteUrl, page.PageId);
                    body.Append("<li>");
                    body.AppendFormat("<a href=\"{0}\">{1}</a> (expires {2}, {3})", linkUrl, page.PageName, page.ExpiryDate.Value.ToLongDateString(), page.ExpiryDate.Value.ToShortTimeString());
                    body.AppendFormat("<br/>{0}", page.PageUrl);
                    body.Append("</li>");
                }
                body.AppendLine("</ol>");

                otherTitle = "Other Pages:";
            }

            // Process remaining pages
            var nonWarningPages = userPages.Pages.Where(d => d.ExpiryDate > warningDate).ToList();
            if (nonWarningPages.Any())
            {
                body.AppendFormatLine("<strong>{0}</strong>", otherTitle);
                body.AppendLine("<ol>");
                foreach (var page in nonWarningPages)
                {
                    var linkUrl = string.Format("{0}#/content/content/edit/{1}", _websiteUrl, page.PageId);
                    body.Append("<li>");
                    body.AppendFormat("<a href=\"{0}\">{1}</a> (expires {2}, {3})", linkUrl, page.PageName, page.ExpiryDate.Value.ToLongDateString(), page.ExpiryDate.Value.ToShortTimeString());
                    body.AppendFormat("<br/>{0}", page.PageUrl);
                    body.Append("</li>");
                }
                body.AppendLine("</ol>");
            }

            var neverExpiringPages = userPages.Pages.Where(d => d.ExpiryDate == null).ToList();
            if (neverExpiringPages.Any() && (nonWarningPages.Any() || lastWarningPages.Any()))
            {
                body.AppendLine("<strong>Pages Never Expiring:</strong>");
                body.AppendLine("<p>As these pages never expire, its important to check them periodically.<p>");
                body.AppendLine("<p>After you’ve logged in, click on each page below and:</p>");
                body.AppendLine("<ul>");
                body.AppendLine("<li>check they are up to date</li>");
                body.AppendLine("<li>check the information is still needed</li>");
                body.AppendLine("<li>then click 'Save and publish'.</li>");
                body.AppendLine("</ul>");
                body.AppendLine("<p>You don't need to worry about setting any dates</p>");
                body.AppendLine("<ol>");
                foreach (var page in neverExpiringPages)
                {
                    var linkUrl = string.Format("{0}#/content/content/edit/{1}", _websiteUrl, page.PageId);
                    body.Append("<li>");
                    body.AppendFormat("<a href=\"{0}\">{1}</a>", linkUrl, page.PageName);
                    body.AppendFormat("<br/>{0}", page.PageUrl);
                    body.Append("</li>");
                }
                body.AppendLine("</ol>");
            }
            if(lastWarningPages.Any() || nonWarningPages.Any())
            {
                var emailTo = userPages.User.Email;

                // If "ForceEmailTo" is set, send all emails there instead (for Testing)
                if (!string.IsNullOrEmpty(_forceSendTo))
                {
                    emailTo = _forceSendTo;
                }

                SmtpSendEmail(emailTo, subject, body.ToString());

                return emailTo;
            }
            return null;
        }

        /// <summary>
        /// Send email to WebStaff highlighting pages that will expire very soon (period set in web.config)
        /// </summary>
        /// <param name="userPages">List of pages that will expire soon</param>
        /// <param name="emailAdminAtDays">Number of days before page expiry</param>
        /// <returns>The email address the email was sent to</returns>
        public string UserPageLastWarningEmail(List<UmbracoPage> userPages)
        {
            var subject = string.Format("ACTION: The following {0} pages expire in under {1} days", _websiteName, EmailAdminAtDays);
            var body = new StringBuilder();

            body.AppendFormatLine("<p>These {0} pages will expire within the next {1} days. After this they will no longer be available to the public.</p>", _websiteName, EmailAdminAtDays.ToString());
            body.AppendLine("<p>After you’ve logged in, click on each page below and:</p>");
            body.AppendLine("<ul>");
            body.AppendLine("<li>check they are up to date</li>");
            body.AppendLine("<li>check the information is still needed</li>");
            body.AppendLine("<li>go to Properties tab and use the calendar to set a new date in the 'Unpublish at' box</li>");
            body.AppendLine("<li>then click 'Save and publish'.</li>");
            body.AppendLine("</ul>");
            body.AppendLine("<p>For details on updating your pages, see <a href=\"" + _webAuthorGuidanceUrl + "\">Guidance for web authors</a>.</p>");

            // Process remaining pages
            body.AppendLine("<strong>Expiring Pages:</strong>");
            body.AppendLine("<ol>");
            foreach (var page in userPages)
            {
                var linkUrl = string.Format("{0}#/content/content/edit/{1}", _websiteUrl, page.PageId);
                body.Append("<li>");
                body.AppendFormat("<a href=\"{0}\">{1}</a> (expires {2}, {3})", linkUrl, page.PageName, page.ExpiryDate.Value.ToLongDateString(), page.ExpiryDate.Value.ToShortTimeString());
                body.AppendFormat("<br/>{0}", page.PageUrl);
                body.Append("</li>");
            }
            body.AppendLine("</ol>");

            // If "ForceEmailTo" is set, send all emails there instead (for Testing)
            var emailTo = _adminEmail;
            if (!string.IsNullOrEmpty(_forceSendTo))
            {
                emailTo = _forceSendTo;
            }
            SmtpSendEmail(emailTo, subject, body.ToString());

            return emailTo;
        }

    }
}