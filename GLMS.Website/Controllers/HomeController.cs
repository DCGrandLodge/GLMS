using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GLMS.BLL;
using GLMS.DAL.Migrations.Seed;
using GLMS.Migration;
using GLMS.Website.Models;

namespace GLMS.Website.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Tabs = Tabs.GetTabModel(MainTab.Dashboard);
            return View();
        }

        public string Migrate()
        {
            try{
            LegacyMigration.MigrateAll(IoC.GetInstance<IGLMSContext>());
            }catch(Exception ex) {
                return ex.Message;
            }
            return "Done";
        }
    }
}
