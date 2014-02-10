using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace GLMS.Website.Filters
{
    public class GLMSAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            var isAuthenticated = base.AuthorizeCore(httpContext) && CurrentUser.IsAuthenticated;
            if (!isAuthenticated)
            {
                bool isAjax = (httpContext.Request.Headers["X-Requested-With"] != null
                    && httpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest");
                if (!isAjax)
                {
                    var routeData = httpContext.Request.RequestContext.RouteData;
                    if (routeData.DataTokens.ContainsKey("area"))
                    {
                        routeData.Values.Add("area", routeData.DataTokens["area"]);
                    }
                    else
                    {
                        routeData.Values.Add("area", "");
                    }
                    if (!((string)routeData.Values["area"] == "" &&
                        (string)routeData.Values["controller"] == "Home" &&
                        (string)routeData.Values["action"] == "Index"))
                    {
                        HttpSessionStateBase session = httpContext.Session;
                        session["LoginRedirect.RouteValues"] = routeData.Values.ToDictionary(x => x.Key, x => x.Value);
                        session["LoginRedirect.QueryString"] = httpContext.Request.QueryString;
                    }
                }
            }
            return isAuthenticated;
        }
    }
}