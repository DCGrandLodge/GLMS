using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace GLMS.MVC.Extensions
{
    public static class ControllerExtension
    {
        public static RedirectResult AjaxRedirect(this Controller controller, string Url)
        {
            return new AjaxRedirectResult(Url);
        }

        public static RedirectToRouteResult AjaxRedirectToAction(this Controller controller, string actionName)
        {
            return AjaxRedirectToAction(controller, actionName, (RouteValueDictionary)null);
        }

        public static RedirectToRouteResult AjaxRedirectToAction(this Controller controller, string actionName, object routeValues)
        {
            return AjaxRedirectToAction(controller, actionName, new RouteValueDictionary(routeValues));
        }

        public static RedirectToRouteResult AjaxRedirectToAction(this Controller controller, string actionName, RouteValueDictionary routeValues)
        {
            return AjaxRedirectToAction(controller, actionName, null /* controllerName */, routeValues);
        }

        public static RedirectToRouteResult AjaxRedirectToAction(this Controller controller, string actionName, string controllerName)
        {
            return AjaxRedirectToAction(controller, actionName, controllerName, (RouteValueDictionary)null);
        }

        public static RedirectToRouteResult AjaxRedirectToAction(this Controller controller, string actionName, string controllerName, object routeValues)
        {
            return AjaxRedirectToAction(controller, actionName, controllerName, new RouteValueDictionary(routeValues));
        }

        public static RedirectToRouteResult AjaxRedirectToAction(this Controller controller, string actionName, string controllerName, RouteValueDictionary routeValues)
        {
            RouteValueDictionary mergedRouteValues;

            if (controller.RouteData == null)
            {
                mergedRouteValues = MergeRouteValues(actionName, controllerName, null, routeValues, includeImplicitMvcValues: true);
            }
            else
            {
                mergedRouteValues = MergeRouteValues(actionName, controllerName, controller.RouteData.Values, routeValues, includeImplicitMvcValues: true);
            }

            return new AjaxRedirectToRouteResult(mergedRouteValues);
        }

        public static RouteValueDictionary GetRouteValues(RouteValueDictionary routeValues)
        {
            return (routeValues != null) ? new RouteValueDictionary(routeValues) : new RouteValueDictionary();
        }

        public static RouteValueDictionary MergeRouteValues(string actionName, string controllerName, RouteValueDictionary implicitRouteValues, RouteValueDictionary routeValues, bool includeImplicitMvcValues)
        {
            // Create a new dictionary containing implicit and auto-generated values
            RouteValueDictionary mergedRouteValues = new RouteValueDictionary();

            if (includeImplicitMvcValues)
            {
                // We only include MVC-specific values like 'controller' and 'action' if we are generating an action link.
                // If we are generating a route link [as to MapRoute("Foo", "any/url", new { controller = ... })], including
                // the current controller name will cause the route match to fail if the current controller is not the same
                // as the destination controller.

                object implicitValue;
                if (implicitRouteValues != null && implicitRouteValues.TryGetValue("action", out implicitValue))
                {
                    mergedRouteValues["action"] = implicitValue;
                }

                if (implicitRouteValues != null && implicitRouteValues.TryGetValue("controller", out implicitValue))
                {
                    mergedRouteValues["controller"] = implicitValue;
                }
            }

            // Merge values from the user's dictionary/object
            if (routeValues != null)
            {
                foreach (KeyValuePair<string, object> routeElement in GetRouteValues(routeValues))
                {
                    mergedRouteValues[routeElement.Key] = routeElement.Value;
                }
            }

            // Merge explicit parameters when not null
            if (actionName != null)
            {
                mergedRouteValues["action"] = actionName;
            }

            if (controllerName != null)
            {
                mergedRouteValues["controller"] = controllerName;
            }

            return mergedRouteValues;
        }
    }
}