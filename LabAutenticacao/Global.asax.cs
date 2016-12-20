using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;

namespace LabAutenticacao
{
    public class MvcApplication : MvcMultiAuthApplication
    {
        public MvcApplication()
            : base(MvcApplication.GetUserRolesFor)
        {
        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            
            RegisterRoutes(RouteTable.Routes);
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }

        private static String[] GetUserRolesFor(String userName)
        {
            return new String[] { "Documentador" };
        }
    }
}
