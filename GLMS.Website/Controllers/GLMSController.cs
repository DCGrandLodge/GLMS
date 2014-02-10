using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GLMS.BLL;

namespace GLMS.Website.Controllers
{
    public abstract class GLMSController : Controller
    {
        protected IGLMSContext context;

        public GLMSController(IGLMSContext context)
        {
            this.context = context;
        }

        protected ActionResult Home()
        {
            return RedirectToAction("Index", "Home");
        }
    }
}