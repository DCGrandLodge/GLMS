using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Diagnostics;

namespace GLMS.MVC.Extensions
{
    //[DebuggerNonUserCode]
    public class GLMSControllerFactory : DefaultControllerFactory
    {
        public static Type ErrorController { get; set; }

        protected override IController GetControllerInstance(System.Web.Routing.RequestContext requestContext, Type controllerType)
        {
            IController icontroller = null;
            try
            {
                icontroller = base.GetControllerInstance(requestContext, controllerType);
            }
            catch (HttpException)
            {
                if (ErrorController == null)
                {
                    throw;
                }
                requestContext.RouteData.Values["controller"] = "Error";
                requestContext.RouteData.Values["action"] = "NotFound";
                icontroller = base.GetControllerInstance(requestContext, GLMSControllerFactory.ErrorController);
            }
            Controller controller = icontroller as Controller;
            if (controller != null)
            {
                controller.ActionInvoker = new GLMSActionInvoker();
                controller.TempDataProvider = new TempDataProvider(controller.TempDataProvider);
            }
            return icontroller;
        }
    }

    public class GLMSActionInvoker : ControllerActionInvoker
    {
        [DebuggerNonUserCode]
        public override bool InvokeAction(ControllerContext controllerContext, string actionName)
        {
            // Don't dump volatile TempData values on Ajax requests
            bool isAjax = (controllerContext.HttpContext.Request.Headers["X-Requested-With"] != null
                && controllerContext.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest");
            TempDataDictionary TempData = controllerContext.Controller.TempData;
            if (TempData.ContainsKey(TempDataProvider.VOLATILE) && !isAjax)
            {
                HashSet<string> vtemp = (HashSet<string>)TempData[TempDataProvider.VOLATILE];
                object fetch;
                foreach (string key in vtemp)
                {
                    fetch = TempData[key];
                }
                TempData.Keep(TempDataProvider.VOLATILE);
            }
            try
            {
                return base.InvokeAction(controllerContext, actionName);
            }
            catch (RedirectException)
            {
                if (TempData.ContainsKey(TempDataProvider.VOLATILE) && !isAjax)
                {
                    HashSet<string> vtemp = (HashSet<string>)TempData[TempDataProvider.VOLATILE];
                    foreach (string key in vtemp)
                    {
                        TempData.Keep(key);
                    }
                    TempData.Keep(TempDataProvider.VOLATILE);
                }
                throw;
            }
        }

        [DebuggerNonUserCode]
        protected override ActionResult InvokeActionMethod(ControllerContext controllerContext, ActionDescriptor actionDescriptor, IDictionary<string, object> parameters)
        {
            bool isAjax = (controllerContext.HttpContext.Request.Headers["X-Requested-With"] != null
                && controllerContext.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest");
            try
            {
                ActionResult result = base.InvokeActionMethod(controllerContext, actionDescriptor, parameters);
                if (controllerContext.RouteData.DataTokens.ContainsKey("area"))
                {
                    controllerContext.RouteData.Values.Add("area", controllerContext.RouteData.DataTokens["area"]);
                }
                if (!(controllerContext.HttpContext.Request.IsAuthenticated) &&
                    result is ViewResult && (!isAjax) &&
                    actionDescriptor.ControllerDescriptor.ControllerName != "Account" &&
                    actionDescriptor.ControllerDescriptor.ControllerName != "Registration")
                {
                    HttpSessionStateBase session = controllerContext.HttpContext.Session;
                    session["LoginRedirect.RouteValues"] = controllerContext.RouteData.Values.ToDictionary(x => x.Key, x => x.Value);
                    session["LoginRedirect.QueryString"] = controllerContext.HttpContext.Request.QueryString;
                }
                return result;
            }
            catch (RedirectException ex)
            {
                return ex.GetRedirect(controllerContext.RequestContext);
            }
        }
    }
}