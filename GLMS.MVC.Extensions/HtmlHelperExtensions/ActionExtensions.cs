using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.Mvc.Ajax;
using System.Web.Routing;

namespace GLMS.MVC.Extensions
{
    public static class ActionExtensions
    {
        #region AjaxHelper
        public static MvcHtmlString ActionLink(this AjaxHelper ajaxHelper, string linkText, string actionName, string controllerName, string areaName, AjaxOptions ajaxOptions)
        {
            RouteValueDictionary dict = new RouteValueDictionary();
            dict.Add("Area", areaName);
            return ajaxHelper.ActionLink(linkText, actionName, controllerName, dict, ajaxOptions);
        }
        public static MvcHtmlString ActionLink(this AjaxHelper ajaxHelper, string linkText, string actionName, string controllerName, string areaName, object routeValues, AjaxOptions ajaxOptions)
        {
            RouteValueDictionary dict = new RouteValueDictionary(routeValues);
            if (dict.ContainsKey("Area"))
            {
                dict.Remove("Area");
            }
            dict.Add("Area", areaName);
            return ajaxHelper.ActionLink(linkText, actionName, controllerName, dict, ajaxOptions);
        }
        public static MvcHtmlString ActionLink(this AjaxHelper ajaxHelper, string linkText, string actionName, string controllerName, string areaName, RouteValueDictionary routeValues, AjaxOptions ajaxOptions)
        {
            if (routeValues == null) { routeValues = new RouteValueDictionary(); }
            if (routeValues.ContainsKey("Area"))
            {
                routeValues.Remove("Area");
            }
            routeValues.Add("Area", areaName);
            return ajaxHelper.ActionLink(linkText, actionName, controllerName, routeValues, ajaxOptions);
        }
        public static MvcHtmlString ActionLink(this AjaxHelper ajaxHelper, string linkText, string actionName, string controllerName, string areaName, object routeValues, AjaxOptions ajaxOptions, object htmlAttributes)
        {
            RouteValueDictionary dict = new RouteValueDictionary(routeValues);
            if (dict.ContainsKey("Area"))
            {
                dict.Remove("Area");
            }
            dict.Add("Area", areaName);
            return ajaxHelper.ActionLink(linkText, actionName, controllerName, dict, ajaxOptions, htmlAttributes);
        }
        public static MvcHtmlString ActionLink(this AjaxHelper ajaxHelper, string linkText, string actionName, string controllerName, string areaName, RouteValueDictionary routeValues, AjaxOptions ajaxOptions, IDictionary<string, object> htmlAttributes)
        {
            if (routeValues == null) { routeValues = new RouteValueDictionary(); }
            if (routeValues.ContainsKey("Area"))
            {
                routeValues.Remove("Area");
            }
            routeValues.Add("Area", areaName);
            return ajaxHelper.ActionLink(linkText, actionName, controllerName, routeValues, ajaxOptions, htmlAttributes);
        }
        public static MvcHtmlString ActionLink(this AjaxHelper ajaxHelper, string linkText, string actionName, string controllerName, string areaName, string protocol, string hostName, string fragment, object routeValues, AjaxOptions ajaxOptions, object htmlAttributes)
        {
            RouteValueDictionary dict = new RouteValueDictionary(routeValues);
            if (dict.ContainsKey("Area"))
            {
                dict.Remove("Area");
            }
            dict.Add("Area", areaName);
            return ajaxHelper.ActionLink(linkText, actionName, controllerName, protocol, hostName, fragment, dict, ajaxOptions, htmlAttributes);
        }
        public static MvcHtmlString ActionLink(this AjaxHelper ajaxHelper, string linkText, string actionName, string controllerName, string areaName, string protocol, string hostName, string fragment, RouteValueDictionary routeValues, AjaxOptions ajaxOptions, IDictionary<string, object> htmlAttributes)
        {
            if (routeValues == null) { routeValues = new RouteValueDictionary(); }
            if (routeValues.ContainsKey("Area"))
            {
                routeValues.Remove("Area");
            }
            routeValues.Add("Area", areaName);
            return ajaxHelper.ActionLink(linkText, actionName, controllerName, protocol, hostName, fragment, routeValues, ajaxOptions, htmlAttributes);
        }

        #endregion

        #region UrlHelper
        public static string AreaAction(this UrlHelper urlHelper, string actionName, string controllerName, string areaName)
        {
            RouteValueDictionary dict = new RouteValueDictionary();
            dict.Add("Area", areaName);
            return urlHelper.Action(actionName, controllerName, dict);
        }
        public static string AreaAction(this UrlHelper urlHelper, string actionName, string controllerName, string areaName, object routeValues)
        {
            RouteValueDictionary dict = new RouteValueDictionary(routeValues);
            if (dict.ContainsKey("Area"))
            {
                dict.Remove("Area");
            }
            dict.Add("Area", areaName);
            return urlHelper.Action(actionName, controllerName, dict);
        }
        public static string AreaAction(this UrlHelper urlHelper, string actionName, string controllerName, string areaName, RouteValueDictionary routeValues)
        {
            if (routeValues == null) { routeValues = new RouteValueDictionary(); }
            if (routeValues.ContainsKey("Area"))
            {
                routeValues.Remove("Area");
            }
            routeValues.Add("Area", areaName);
            return urlHelper.Action(actionName, controllerName, routeValues);
        }
        public static string AreaAction(this UrlHelper urlHelper, string actionName, string controllerName, string areaName, object routeValues, string protocol)
        {
            RouteValueDictionary dict = new RouteValueDictionary(routeValues);
            if (dict.ContainsKey("Area"))
            {
                dict.Remove("Area");
            }
            dict.Add("Area", areaName);
            return urlHelper.Action(actionName, controllerName, dict, protocol);
        }
        public static string AreaAction(this UrlHelper urlHelper, string actionName, string controllerName, string areaName, RouteValueDictionary routeValues, string protocol, string hostName)
        {
            if (routeValues == null) { routeValues = new RouteValueDictionary(); }
            if (routeValues.ContainsKey("Area"))
            {
                routeValues.Remove("Area");
            }
            routeValues.Add("Area", areaName);
            return urlHelper.Action(actionName, controllerName, routeValues, protocol, hostName);
        }
        #endregion

        #region HtmlHelper
        public static MvcHtmlString ActionLink(this HtmlHelper htmlHelper, string linkText, string actionName, 
            string controllerName, string areaName)
        {
            RouteValueDictionary dict = new RouteValueDictionary();
            dict.Add("Area", areaName);
            return htmlHelper.ActionLink(linkText, actionName, controllerName, dict, new RouteValueDictionary());
        }

        public static MvcHtmlString ActionLink(this HtmlHelper htmlHelper, string linkText, string actionName,
            string controllerName, string areaName, object routeValues)
        {
            RouteValueDictionary dict = new RouteValueDictionary(routeValues);
            if (dict.ContainsKey("Area"))
            {
                dict.Remove("Area");
            }
            dict.Add("Area", areaName);
            return htmlHelper.ActionLink(linkText, actionName, controllerName, dict, null);
        }

        public static MvcHtmlString ActionLink(this HtmlHelper htmlHelper, string linkText, string actionName,
            string controllerName, string areaName, RouteValueDictionary routeValues)
        {
            if (routeValues == null) { routeValues = new RouteValueDictionary(); }
            if (routeValues.ContainsKey("Area"))
            {
                routeValues.Remove("Area");
            }
            routeValues.Add("Area", areaName);
            return htmlHelper.ActionLink(linkText, actionName, controllerName, routeValues, null);
        }

        public static MvcHtmlString ActionLink(this HtmlHelper htmlHelper, string linkText, string actionName, 
            string controllerName, string areaName, object routeValues, object htmlAttributes)
        {
            RouteValueDictionary dict = new RouteValueDictionary(routeValues);
            if (dict.ContainsKey("Area"))
            {
                dict.Remove("Area");
            }
            dict.Add("Area", areaName);
            return htmlHelper.ActionLink(linkText, actionName, controllerName, dict, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static MvcHtmlString ActionLink(this HtmlHelper htmlHelper, string linkText, string actionName, 
            string controllerName, string areaName, RouteValueDictionary routeValues, IDictionary<string, object> htmlAttributes)
        {
            if (routeValues == null) { routeValues = new RouteValueDictionary(); }
            if (routeValues.ContainsKey("Area"))
            {
                routeValues.Remove("Area");
            }
            routeValues.Add("Area", areaName);
            return htmlHelper.ActionLink(linkText, actionName, controllerName, routeValues, htmlAttributes);
        }

        public static MvcHtmlString ActionLink(this HtmlHelper htmlHelper, string linkText, string actionName, 
            string controllerName, string areaName, string protocol, string hostName, string fragment, 
            object routeValues, object htmlAttributes)
        {
            RouteValueDictionary dict = new RouteValueDictionary(routeValues);
            if (dict.ContainsKey("Area"))
            {
                dict.Remove("Area");
            }
            dict.Add("Area", areaName);
            return htmlHelper.ActionLink(linkText, actionName, controllerName, protocol, hostName, fragment, dict, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static MvcHtmlString ActionLink(this HtmlHelper htmlHelper, string linkText, string actionName, 
            string controllerName, string areaName, string protocol, string hostName, string fragment, 
            RouteValueDictionary routeValues, IDictionary<string, object> htmlAttributes)
        {
            if (routeValues == null) { routeValues = new RouteValueDictionary(); }
            if (routeValues.ContainsKey("Area"))
            {
                routeValues.Remove("Area");
            }
            routeValues.Add("Area", areaName);
            return MvcHtmlString.Create(HtmlHelper.GenerateLink(htmlHelper.ViewContext.RequestContext, htmlHelper.RouteCollection, linkText, null, actionName, controllerName, protocol, hostName, fragment, routeValues, htmlAttributes));
        }
        #endregion
    }
}