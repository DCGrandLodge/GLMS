using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GLMS.BLL;
using GLMS.BLL.Entities;
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
                PhoneNumber = x.PhoneNumber,
                Address = (x.Address.Street ?? "") + " " + (x.Address.City ?? "") + " " + (x.Address.State ?? ""),
                Status = x.Status,
                StatusDate = x.StatusDate
            });
        }
        #endregion

        #region View
        public ActionResult View(Guid id)
        {
            var model = GetViewModel(id);
            if (model == null)
            {
                TempData["__ErrorMessage"] = "Unable to find lodge details";
                return ToIndex();
            }

            return View(model);
        }

        private LodgeViewModel GetViewModel(Guid id)
        {
            var model = context.Lodges
                // Restrict to just the lodge we're viewing
                .Where(x => x.LodgeID == id)
                // Give LINQtoEntities a hint that we will be pulling from these two related tables
                .Include(x => x.MergedWithLodge)
                .Include(x => x.MergedLodges)
                // select the raw (unformatted) data
                .Select(x => new
                {
                    x.LodgeID,
                    x.Name,
                    x.Number,
                    x.PhoneNumber,
                    x.Address.Street,
                    x.Address.City,
                    x.Address.State,
                    x.Address.Zip,
                    x.Address.Country,
                    x.Status,
                    x.StatusDate,
                    x.CharterDate,
                    x.DarkDate,
                    x.DispensationDate,
                    x.MeetingDates,
                    x.MergedDate,
                    MergedLodges = x.MergedLodges.Select(y => new LodgeViewModel { LodgeID = y.LodgeID, Name = y.Name, Number = y.Number, StatusDate = y.MergedDate }),
                    x.MergedWithLodgeID,
                    MergedWithName = x.MergedWithLodge.Name,
                    MergedWithNumber = (int?)x.MergedWithLodge.Number,
                    MergedWithDate = x.MergedDate
                })
                // Materialize
                .ToList()
                // Convert to LodgeViewModel, along with formatting (since we can't call String.Format on the SQL side of the expression)
                .Select(x => new LodgeViewModel()
                {
                    LodgeID = x.LodgeID,
                    Name = x.Name,
                    Number = x.Number,
                    Status = x.Status,
                    StatusDate = x.StatusDate,
                    PhoneNumber = x.PhoneNumber,
                    Address1 = x.Street,
                    Address2 = x.City + " " + x.State + " " + x.Zip,
                    Address3 = x.Country,
                    CharterDate = x.CharterDate,
                    DarkDate = x.DarkDate,
                    DispensationDate = x.DispensationDate,
                    MeetingDates = x.MeetingDates,
                    MergedInto = x.MergedWithLodgeID.HasValue ? new LodgeViewModel() { LodgeID = x.MergedWithLodgeID.Value, Name = x.MergedWithName, Number = x.MergedWithNumber.Value, StatusDate = x.MergedDate } : null,
                    MergedLodges = x.MergedLodges
                })
                // There can only be one record since we're filtering on a Key field, and FirstOrDefault is faster than SingleOrDefault
                .FirstOrDefault();
            if (model != null)
            {
                model.Officers = GetOfficerViewModel(id, false);
                model.HonoraryOfficers = GetHonoraryOfficerViewModel(id);
            }
            return model;
        }

        private IEnumerable<LodgeOfficerModel> GetHonoraryOfficerViewModel(Guid id)
        {
            return context.Lodges
                .Where(x => x.LodgeID == id)
                .SelectMany(x => x.Officers)
                .Where(x => x.Emeritus || x.PastOfficer)
                .Select(x => new { x.LodgeOffice, x.Member, x.Emeritus, x.PastOfficer, x.Honorary })
                .Select(x => new
                {
                    x.LodgeOffice.Title,
                    x.LodgeOffice.Sequence,
                    x.Member.FullName,
                    x.Emeritus,
                    x.PastOfficer,
                    x.Honorary,
                })
                .OrderBy(x => x.Sequence)  //  Display in order of Officer rank (WM, SW, etc.)
                .ThenByDescending(x => x.Emeritus)    // Display Emeritus officers first (served in the past, retains title)
                .ThenByDescending(x => x.PastOfficer) // Then Past officers
                .ThenByDescending(x => x.Honorary)    // Then Honorary officers
                .ToList()
                .Select(x => new LodgeOfficerModel()
                {
                    // Format is (Honorary) (Past) (Title) (Emeritus)
                    Title = String.Format("{0}{1}{2}{3}", x.Honorary ? "Honorary " : "", x.PastOfficer ? "Past " : "", x.Title == "Worshipful Master" ? "Master" : x.Title, x.Emeritus ? " Emeritus" : ""),
                    Name = x.FullName
                });
        }

        #endregion

        #region Create
        public ActionResult Create()
        {
            return View(new LodgeEditModel());
        }

        [HttpPost]
        public ActionResult Create(LodgeEditModel model)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Edit
        public ActionResult Edit(Guid id)
        {
            var model = GetEditModel(id);
            if (model == null)
            {
                TempData["__ErrorMessage"] = "Unable to find lodge details";
                return ToIndex();
            }
            return View(model);
        }

        [HttpPost]
        public ActionResult Edit(LodgeEditModel model)
        {
            var lodge = context.Lodges
                .Where(x => x.LodgeID == model.LodgeID)
                .FirstOrDefault();
            if (lodge == null)
            {
                TempData["__ErrorMessage"] = "Unable to find lodge details";
                return ToIndex();
            }
            if (ModelState.IsValid)
            {
                lodge.Name = model.Name;
                lodge.Number = model.Number;
                lodge.PhoneNumber = model.PhoneNumber;
                lodge.MeetingDates = model.MeetingDates;
                lodge.Address.Street = model.Address.Street;
                lodge.Address.City = model.Address.City;
                lodge.Address.State = model.Address.State;
                lodge.Address.Zip = model.Address.Zip;
                lodge.Address.Country = model.Address.Country;
                if (context.TrySaveChanges(ModelState))
                {
                    ModelState.Clear();
                    TempData["__Success"] = "Saved";
                }
            }
            model = GetEditModel(lodge.LodgeID);
            return View(model);
        }

        public JsonResult OfficerAutocomplete(Guid id, JQAutoCompleteRequest request)
        {
            return Json(context.Lodges
                .Where(x => x.LodgeID == id)
                .SelectMany(x => x.LodgeMembership.Select(y => new { y.Status, y.Member }))
                .Where(x => x.Status == MemberStatus.Active && x.Member.Status == MemberStatus.Active &&
                    x.Member.FullName.Contains(request.Term.ToLower()))
                .Select(x => new { label = x.Member.FullName, id = x.Member.MemberID }), JsonRequestBehavior.AllowGet);
        }

        private LodgeEditModel GetEditModel(Guid id)
        {
            var model = context.Lodges
                .Where(x => x.LodgeID == id)
                .Select(x => new LodgeEditModel()
                {
                    LodgeID = x.LodgeID,
                    Name = x.Name,
                    Number = x.Number,
                    Address = new AddressEditModel()
                    {
                        Street = x.Address.Street,
                        City = x.Address.City,
                        State = x.Address.State,
                        Zip = x.Address.Zip,
                        Country = x.Address.Country,
                    },
                    PhoneNumber = x.PhoneNumber,
                    MeetingDates = x.MeetingDates,
                })
                .FirstOrDefault();
            if (model != null)
            {
                model.Officers = GetOfficerViewModel(id, true).ToList();
            }
            return model;
        }

        #endregion

        #region Common
        private IEnumerable<LodgeOfficerModel> GetOfficerViewModel(Guid id, bool forEdit)
        {
            // In this case we want all the Lodge offices, along with Proxies for WM, SW, and JW on separate rows.
            var withProxy = new string[] { "WM", "SW", "JW" };
            // So we construct a union of ALL offices (proxy = false) plus proxies for the pillar offices (proxy = true)
            var offices = context.Offices.Where(x => !x.GrandOffice)
                .Select(x => new { x.OfficeID, x.Title, x.Sequence, Proxy = false })
                .Union(context.Offices.Where(x => !x.GrandOffice && withProxy.Contains(x.Abbr))
                    .Select(x => new { x.OfficeID, x.Title, x.Sequence, Proxy = true }));

            return offices
                // GroupJoin, followed by .SelectMany with .DefaultIsEmpty, translates to a LeftJoin 
                // we want the full list of Offices, along with the Officer names, keeping in mind that a Lodge might not have someone in a particular office
                .GroupJoin(context.Lodges
                        .Where(x => x.LodgeID == id)
                        .SelectMany(x => x.Officers)
                        .Where(x => !(x.Emeritus || x.PastOfficer || x.Honorary)),  // Lodge officers - but only actual, current officers
                    x => new { x.OfficeID, x.Proxy },
                    x => new { OfficeID = x.LodgeOfficeID, x.Proxy },
                    (x, y) => new
                    {
                        x.OfficeID,
                        x.Title,
                        x.Sequence,
                        x.Proxy,
                        Data = y
                    })
                .SelectMany(x => x.Data.DefaultIfEmpty(), (x, y) => new
                {
                    // Office fields
                    x.OfficeID,
                    x.Title,
                    x.Sequence,
                    x.Proxy,
                    // Officer fields
                    MemberID = (Guid?)y.MemberID,
                    y.Member.FullName,
                    // The officer may or may not be a member of the Lodge, e.g. for Tyler
                    IsMember = y == null ? true : y.Member.LodgeMembership.Any(z => z.LodgeID == id)
                })
                .OrderBy(x => x.Sequence)   // Sort first on Office sequence
                .ThenBy(x => x.Proxy)       // Then on Proxy descending (false first)
                // Materialize the list
                .ToList()
                // And format it into a LodgeOfficerModel for display
                .Select(x => new LodgeOfficerModel()
                {
                    OfficeID = x.OfficeID,
                    Title = String.Format("{0}{1}", x.Title, x.Proxy ? " (Proxy)" : ""),
                    MemberID = x.MemberID,
                    Name = x.MemberID.HasValue
                        ? String.Format("{0}{3}", x.FullName, x.IsMember || forEdit ? "" : " (non-member)")
                        : forEdit ? "" : "(vacant)",
                    Proxy = x.Proxy
                });
        }

        #endregion
    }
}
