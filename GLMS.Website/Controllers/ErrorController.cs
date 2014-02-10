using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using GLMS.Website.Models;

namespace GLMS.Website.Controllers
{
    [AllowAnonymous]
    public class ErrorController : Controller
    {
        public ActionResult Index()
        {
            return NotFound();
        }

        public ActionResult NotFound()
        {
            Response.StatusCode = (int)HttpStatusCode.NotFound;
            return View("404", new Http404Model() { RequestedUrl = Request.RawUrl });
        }

        public ActionResult NoPermission()
        {
            Response.StatusCode = (int)HttpStatusCode.Forbidden;
            return View("403");
        }

    }
}