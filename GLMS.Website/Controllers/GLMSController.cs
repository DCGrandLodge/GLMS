using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GLMS.BLL;
using GLMS.MVC.Extensions;
using GLMS.MVC.Extensions.Back;

namespace GLMS.Website.Controllers
{
    public abstract class GLMSController : Controller
    {
        protected IGLMSContext context;

        public GLMSController(IGLMSContext context)
        {
            this.context = context;
        }

        public ActionResult Back()
        {
            var back = BackTrail.Back();
            if (back == null)
            {
                return Home();
            }
            else
            {
                return Redirect(back);
            }
        }

        // Helper actions not publically accessible
        [NonAction]
        protected ActionResult Home()
        {
            return RedirectToAction("Index", "Home");
        }

        [NonAction]
        protected ActionResult ToIndex(object routeValues = null)
        {
            return RedirectToAction("Index", routeValues);
        }

    }
}