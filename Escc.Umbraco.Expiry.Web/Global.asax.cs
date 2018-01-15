using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Escc.Umbraco.Expiry.Web
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }
    }
}