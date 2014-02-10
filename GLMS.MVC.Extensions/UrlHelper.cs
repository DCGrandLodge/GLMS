using System;
using System.IO;
using System.Web;
using System.Web.Mvc;

namespace GLMS.MVC.Extensions
{
    public static class StaticUrlHelper
    {
        private static UrlHelper _urlHelper;

        public static UrlHelper GetFromContext()
        {
            if (_urlHelper == null)
            {
                if (HttpContext.Current == null)
                {
                    throw new HttpException("Current httpcontext is null!");
                }

                if (!(HttpContext.Current.CurrentHandler is System.Web.Mvc.MvcHandler))
                {
                    throw new HttpException("Type casting is failed!");
                }

                _urlHelper = new UrlHelper(((MvcHandler)HttpContext.Current.CurrentHandler).RequestContext);
            }

            return _urlHelper;
        }

        public static string Content(this UrlHelper urlHelper, string contentPath, bool? embed)
        {
            if (embed == true)
            {
                contentPath = urlHelper.RequestContext.HttpContext.Server.MapPath(contentPath);
                return String.Format("data:image/png;base64,{0}", Convert.ToBase64String(File.ReadAllBytes(contentPath), Base64FormattingOptions.None));
            }
            else
            {
                return urlHelper.Content(contentPath);
            }
        }
    }
}