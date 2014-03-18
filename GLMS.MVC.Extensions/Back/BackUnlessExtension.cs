using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using GLMS.MVC.Extensions.Back;

namespace GLMS.MVC.Extensions
{
    public static class BackUnlessExtension
    {
        public static MvcHtmlString BackUnless(this HtmlHelper html, string action)
        {
            return DoBackUnless(html, action, null, null, null, null);
        }

        public static MvcHtmlString BackUnless(this HtmlHelper html, string action, string controller)
        {
            return DoBackUnless(html, action, controller, null, null, null);
        }

        public static MvcHtmlString BackUnless(this HtmlHelper html, string action, string controller, string area)
        {
            return DoBackUnless(html, action, controller, area, null, null);
        }

        public static MvcHtmlString BackUnless(this HtmlHelper html, string action, object routeValues)
        {
            return DoBackUnless(html, action, null, null, routeValues, null);
        }

        public static MvcHtmlString BackUnless(this HtmlHelper html, string action, string controller, object routeValues)
        {
            return DoBackUnless(html, action, controller, null, routeValues, null);
        }

        public static MvcHtmlString BackUnless(this HtmlHelper html, string action, string controller, string area, object routeValues)
        {
            return DoBackUnless(html, action, controller, area, routeValues, null);
        }

        public static MvcHtmlString BackUnless(this HtmlHelper html, string action, object routeValues, object htmlAttributes)
        {
            return DoBackUnless(html, action, null, null, routeValues, htmlAttributes);
        }

        public static MvcHtmlString BackUnless(this HtmlHelper html, string action, string controller, object routeValues, object htmlAttributes)
        {
            return DoBackUnless(html, action, controller, null, routeValues, htmlAttributes);
        }

        public static MvcHtmlString BackUnless(this HtmlHelper html, string action, string controller, string area, object routeValues, object htmlAttributes)
        {
            return DoBackUnless(html, action, controller, area, routeValues, htmlAttributes);
        }

        private static MvcHtmlString DoBackUnless(this HtmlHelper html, string action, string controller, string area, object routeValues, object htmlAttributes)
        {
            string unless = StaticUrlHelper.GetFromContext().AreaAction(action, controller, area, routeValues);
            if (BackTrail.Peek() != unless)
            {
                return html.ActionLink("Back", "Back", null, htmlAttributes);
            }
            else
            {
                return null;
            }
        }
    }
}
