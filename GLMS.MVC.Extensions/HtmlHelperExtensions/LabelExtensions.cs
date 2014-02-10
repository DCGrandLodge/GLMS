using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace GLMS.MVC.Extensions
{
    public enum Required { Always, SometimesNow, SometimesNotNow, Never }

    public static class LabelExtensions
    {
        public static MvcHtmlString Dagger(this HtmlHelper html)
        {
            return new MvcHtmlString(GetRequiredFieldIndicator(Required.Always, style: "dagger", text: "‡"));
        }
        public static MvcHtmlString RequiredFieldIndicator(this HtmlHelper html)
        {
            return new MvcHtmlString(GetRequiredFieldIndicator(Required.Always));
        }
        public static MvcHtmlString RequiredFieldIndicator<TModel, TValue>(this HtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression, Required flagRequired)
        {
            return new MvcHtmlString(GetRequiredFieldIndicator(flagRequired, id: html.IdFor(expression) + "_Req"));
        }
        private static string GetRequiredFieldIndicator(Required flagRequired, string style = "required-field-indicator", string text = "*", string id = null)
        {
            if (flagRequired == Required.Never)
            {
                return null;
            }
            string required;
            TagBuilder req;
            if (flagRequired == Required.Always || id == null)
            {
                req = new TagBuilder("span");
            }
            else // Sometimes
            {
                req = new TagBuilder("label");
                req.GenerateId(id);
                if (flagRequired == Required.SometimesNotNow)
                {
                    req.Attributes.Add("style", "display:none;");
                }
            }
            req.SetInnerText(text);
            req.AddCssClass(style);
            required = req.ToString();
            return required;
        }

        public static MvcHtmlString LabelFor<TModel, TValue>(this HtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression, Required flagRequired)
        {
            return LabelFor(html, expression, null, flagRequired);
        }

        public static MvcHtmlString LabelFor<TModel, TValue>(this HtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression, string labelText, Required flagRequired)
        {
            MvcHtmlString labelFor;
            if (labelText != null)
            {
                labelFor = System.Web.Mvc.Html.LabelExtensions.LabelFor(html, expression, labelText);
            }
            else
            {
                labelFor = System.Web.Mvc.Html.LabelExtensions.LabelFor(html, expression);
            }
            string required = GetRequiredFieldIndicator(flagRequired, id: html.IdFor(expression) + "_Req");
            return new MvcHtmlString(String.Format("{0}{1}{2}", labelFor.ToString(), String.IsNullOrEmpty(required) ? "" : "&nbsp;", required));
        }
    }
}
