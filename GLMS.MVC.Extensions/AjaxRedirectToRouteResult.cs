using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web;
using System.Web.Script.Serialization;

namespace GLMS.MVC.Extensions
{
    public enum AjaxRedirectReason { Redirect, Logon, NoPermission }
    public class AjaxRedirectToRouteResult : RedirectToRouteResult {

        public AjaxRedirectReason Reason { get; set; }

        public AjaxRedirectToRouteResult(RouteValueDictionary routeValues, AjaxRedirectReason reason = AjaxRedirectReason.Redirect) :
            this(null, routeValues, reason)
        {
        }

        public AjaxRedirectToRouteResult(string routeName, RouteValueDictionary routeValues, AjaxRedirectReason reason = AjaxRedirectReason.Redirect)
            : base(routeName, routeValues, permanent: false)
        {
            this.Reason = reason;
        }

        public override void ExecuteResult(ControllerContext context)
        {
            if (context == null) {
                throw new ArgumentNullException("context");
            }
            if (context.IsChildAction) {
                throw new InvalidOperationException(/*MvcResources.RedirectAction_CannotRedirectInChildAction*/);
            }

            string destinationUrl = UrlHelper.GenerateUrl(RouteName, null /* actionName */, null /* controllerName */, RouteValues, RouteTable.Routes, context.RequestContext, false /* includeImplicitMvcValues */);
            if (String.IsNullOrEmpty(destinationUrl)) {
                throw new InvalidOperationException(/*MvcResources.Common_NoRouteMatched*/);
            }

            context.Controller.TempData.Keep();

            if (context.HttpContext.Request.IsAjaxRequest())
            {
                HttpResponseBase response = context.HttpContext.Response;

                response.ContentType = "application/json";
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                response.Write(serializer.Serialize(new { redirectUrl = destinationUrl, reason = Reason }));
            }
            else
            {
                context.HttpContext.Response.Redirect(destinationUrl, endResponse: false);
            }
        }
    }
}
