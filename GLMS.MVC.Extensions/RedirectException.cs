using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Routing;
using System.Web;

namespace GLMS.MVC.Extensions
{
    public enum RedirectTarget { LogOn, Home, NoPermission, Custom, Exception };

    public class RedirectException : ApplicationException
    {
        public static string LogonAction = "LogOn";
        public RedirectTarget Target { get; set; }
        public string FailArea { get; set; }
        public string FailController { get; set; }
        public string FailAction { get; set; }
        public object FailParameters { get; set; }
        public AjaxRedirectReason FailReason { get; set; }

        public RedirectException() : base() { }
        public RedirectException(string message) : base(message) { }
        public RedirectException(string message, Exception innerException) : base(message, innerException) { }

        public AjaxRedirectToRouteResult GetRedirect(RequestContext context)
        {
            string action;
            string controller;
            string area = "";
            AjaxRedirectReason reason = AjaxRedirectReason.Redirect;
            bool saveRedirect = false;
            switch (Target)
            {
                case RedirectTarget.LogOn:
                    action = LogonAction;
                    controller = "Account";
                    saveRedirect = true;
                    reason = AjaxRedirectReason.Logon;
                    break;
                case RedirectTarget.NoPermission:
                    action = "NoPermission";
                    controller = "Error";
                    reason = AjaxRedirectReason.NoPermission;
                    break;
                case RedirectTarget.Home:
                    action = "Index";
                    controller = "Home";
                    break;
                case RedirectTarget.Custom:
                    if (this.FailAction == null)
                    {
                        throw new ArgumentNullException("Target action of RedirectException cannot be null");
                    }
                    action = this.FailAction ?? (string)context.RouteData.Values["action"];
                    controller = this.FailController ?? (string)context.RouteData.Values["controller"];
                    area = this.FailArea ?? (string)context.RouteData.Values["area"];
                    reason = this.FailReason;
                    break;
                default: // Security.RedirectTarget.Exception:
                    throw new UnauthorizedAccessException("You are not authorized to view this page.");
            }
            if (saveRedirect)
            {
                if (!(context.HttpContext.Request.Headers["X-Requested-With"] != null
                    && context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest"))
                {
                    if (context.RouteData.DataTokens.ContainsKey("area"))
                    {
                        context.RouteData.Values.Add("area", context.RouteData.DataTokens["area"]);
                    }
                    HttpSessionStateBase session = context.HttpContext.Session;
                    session["LoginRedirect.RouteValues"] = context.RouteData.Values.ToDictionary(x => x.Key, x => x.Value);
                    session["LoginRedirect.QueryString"] = context.HttpContext.Request.QueryString;
                }
            }
            RouteValueDictionary rvd = new RouteValueDictionary(FailParameters);
            rvd["area"] = area;
            rvd["controller"] = controller;
            rvd["action"] = action;
            return new AjaxRedirectToRouteResult(rvd, reason);
        }
    }

}
