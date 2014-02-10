using System.Web;
using System.Web.Mvc;
using GLMS.Website.Filters;

namespace GLMS.Website
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new GLMSAuthorizeAttribute());
        }
    }
}