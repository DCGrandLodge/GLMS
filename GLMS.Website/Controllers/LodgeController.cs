using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GLMS.BLL;
using GLMS.MVC.Extensions.jqAutoComplete;
using GLMS.MVC.Extensions.jqGrid;
using GLMS.Website.Models;

namespace GLMS.Website.Controllers
{
    public class LodgeController : GLMSController
    {
        static LodgeController()
        {
            JQGridDataRequestBinder<LodgeIndexModel.Data>.RegisterBinder();
        }

        public LodgeController(IGLMSContext context) : base(context) { }

        #region Index
        public ActionResult Index()
        {
            LodgeIndexModel model = new LodgeIndexModel();
            ViewBag.Tabs = Tabs.GetTabModel(MainTab.Lodges);
            return View(model);
        }

        public JsonResult IndexData(JQGridDataRequest<LodgeIndexModel.Data> request)
        {
            Session[LodgeIndexModel.GridKey] = request;
            return new JQGridDataResult<LodgeIndexModel.Data>(request, GetLodges());
        }

        public JsonResult AutoCompleteIndex(JQAutoCompleteRequest<LodgeIndexModel.Data> request)
        {
            request.InjectSearchTerms(LodgeIndexModel.GridKey);
            return new JQAutoCompleteResult<LodgeIndexModel.Data>(request, GetLodges());
        }

        private IQueryable<LodgeIndexModel.Data> GetLodges()
        {
            return context.Lodges.Select(x => new LodgeIndexModel.Data()
            {
                LodgeID = x.LodgeID,
                Number = x.Number,
                Name = x.Name,
                Address = x.Address.Street + " " + x.Address.City,
                Status = x.Status
            });
        }
        #endregion

    }
}
