using System.Collections.Generic;

namespace Escc.Umbraco.Expiry.Notifier
{
    public interface IEmailService
    {
        int EmailAdminAtDays { get; }
        string UserPageExpiryEmail(UmbracoPagesForUser userPages);
        string UserPageLastWarningEmail(List<UmbracoPage> userPages);
    }
}