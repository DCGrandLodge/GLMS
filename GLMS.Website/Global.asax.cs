using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using GLMS.MVC.Extensions;
using GLMS.MVC.Extensions.jqAutoComplete;
using GLMS.Website.Controllers;

namespace GLMS.Website
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            AuthConfig.RegisterAuth();


            JQAutoCompleteRequestBinder.RegisterBinder();
            GlobalFilters.Filters.Add(new ExceptionFilter());
            foreach (var filter in GlobalFilters.Filters.OfType<HandleErrorAttribute>().ToList())
            {
                GlobalFilters.Filters.Remove(filter);
            }
            GLMSControllerFactory.ErrorController = typeof(ErrorController);
            ControllerBuilder.Current.SetControllerFactory(typeof(GLMSControllerFactory));
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            Application["AppVersion"] = String.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
        }
    }
}