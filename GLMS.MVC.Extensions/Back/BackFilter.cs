using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace GLMS.MVC.Extensions.Back
{
    public class BackFilter : IResultFilter
    {
        public void OnResultExecuted(ResultExecutedContext filterContext)
        {
            if (filterContext.Result is ViewResult)
            {
                if (filterContext.RequestContext.HttpContext.Response.StatusCode == (int)HttpStatusCode.OK)
                {
                    var path = filterContext.RequestContext.HttpContext.Request.Params["URL"];
                    BackTrail.Push(path);
                }
            }
        }

        public void OnResultExecuting(ResultExecutingContext filterContext)
        {
        }
    }
}
