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
    public class AjaxRedirectResult : RedirectResult {

        private AjaxRedirectReason reason;

        public AjaxRedirectResult(string url, AjaxRedirectReason reason = AjaxRedirectReason.Redirect) :
            base(url)
        {
            this.reason = reason;
        }

        public override void ExecuteResult(ControllerContext context)
        {
            if (context == null) {
                throw new ArgumentNullException("context");
            }
            if (context.IsChildAction) {
                throw new InvalidOperationException(/*MvcResources.RedirectAction_CannotRedirectInChildAction*/);
            }

            if (String.IsNullOrEmpty(Url)) {
                throw new InvalidOperationException(/*MvcResources.Common_NoRouteMatched*/);
            }

            context.Controller.TempData.Keep();

            if (context.HttpContext.Request.IsAjaxRequest())
            {
                HttpResponseBase response = context.HttpContext.Response;

                response.ContentType = "application/json";
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                response.Write(serializer.Serialize(new { redirectUrl = Url, reason = reason }));
            }
            else
            {
                context.HttpContext.Response.Redirect(Url, endResponse: false);
            }
        }


    }

}
