using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Collections;
using System.Net;
using System.Xml;
using System.IO;
using ApplicationException = Elmah.ApplicationException;
using Elmah;
using GLMS.Website.Models.LogView;
using GLMS.MVC.Extensions;
using GLMS.BLL.Entities;
using GLMS.MVC.Extensions.jqGrid;
using GLMS.MVC.Extensions.jqAutoComplete;
using GLMS.MVC.Extensions.DynamicQuery;

namespace GLMS.Website.Controllers
{
    public class LogViewController : Controller
    {
       
        private ErrorLog _log;
        protected virtual ErrorLog ErrorLog
        {
            get
            {
                if (_log == null)
                {
                    _log = ErrorLog.GetDefault(HttpContext.ApplicationInstance.Context);
                }
                return _log;
            }
        }

        static LogViewController()
        {
            JQGridDataRequestBinder<ErrorListModel>.RegisterBinder();
            JQAutoCompleteRequestBinder<ErrorListModel>.RegisterBinder();
        }

        public LogViewController()
        {

        }

        protected override void OnAuthorization(AuthorizationContext filterContext)
        {
            base.OnAuthorization(filterContext);
            if (!CurrentUser.IsAuthenticated)
            {
                throw new RedirectException() { Target = RedirectTarget.LogOn };
            }
            if (!CurrentUser.HasPermission(AccessLevel.System))
            {
                throw new RedirectException() { Target = RedirectTarget.NoPermission };
            }
        }

        public ActionResult Index(bool clear = false)
        {
            if (clear) { Session.Remove(IndexModel.GridKey); }
            var model = new IndexModel()
            {
                ApplicationName = String.IsNullOrWhiteSpace(Server.MachineName) ? ErrorLog.ApplicationName :
                    String.Format("{0} on {1}", ErrorLog.ApplicationName, Server.MachineName)
            };
            // TODO - Set tabs
            return View("Index", model);
        }

        public ActionResult Resolve(Guid id, bool resolve)
        {
            this.ErrorLog.ResolveError(id, resolve);
            return RedirectToAction("Index");
        }

        public JsonResult GetErrors(JQGridDataRequest<ErrorListModel> request, bool includeResolved)
        {
            Session[IndexModel.GridKey] = request;
            // Elmah ErrorLog class does not allow filtering or sorting.
            request.IsSearch = false;
            request.SearchTerms.Clear();
            request.SortFields.Clear();
            request.SortFields.Add(new SortField("Sequence", SortDirection.Desc));

            var errorEntryList = new ArrayList(request.PageSize);
            var totalCount = this.ErrorLog.GetErrors(request.Page - 1, request.PageSize, errorEntryList, includeResolved);
            var data = errorEntryList.Cast<ErrorLogEntry>()
                .Select(x => new ErrorListModel()
                {
                    ID = x.Id,
                    Sequence = x.Error.Sequence,
                    StatusCode = x.Error.StatusCode,
                    Host = x.Error.HostName,
                    ErrorType = Server.HtmlEncode(x.Error.Type),
                    Message = Server.HtmlEncode(x.Error.Message),
                    User = x.Error.User,
                    Time = x.Error.Time
                })
                .AsQueryable();
            var json = request.BuildResult(data);
            var response = json.Data as JQGridDataResponse<ErrorListModel>;
            // Elmah ErrorLog class only gives us one page, jqgrid expects full info
            response.RecordCount = totalCount;
            response.PageCount = (int)Math.Ceiling((double)totalCount / request.PageSize);
            response.Page = request.Page;
            return json;
        }

        public JsonResult ClearLog(DateTime? olderThan)
        {
            ErrorLog.DeleteErrors(olderThan);
            return Json(true, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Detail(string id)
        {
            int iid;
            if (int.TryParse(id, out iid))
            {
                id = ErrorLog.GetErrorIDBySequence(iid);
            }
            var logEntry = ErrorLog.GetError(id);
            var model = new DetailModel()
            {
                ID = logEntry.Id,
                Resolved = logEntry.Error.Resolved,
                Sequence = logEntry.Error.Sequence,
                StatusCode = logEntry.Error.StatusCode,
                Host = logEntry.Error.HostName,
                ErrorType = Server.HtmlEncode(logEntry.Error.Type),
                Message = Server.HtmlEncode(logEntry.Error.Message),
                User = logEntry.Error.User,
                Time = logEntry.Error.Time,
                Detail = logEntry.Error.Detail,
                ServerVariables = logEntry.Error.ServerVariables,
                EntityValidationErrors = logEntry.Error.EntityValidationErrors,
                QueryString = logEntry.Error.QueryString,
                Form = logEntry.Error.Form
            };
            return View("Detail", model);
        }

        public ActionResult Json(string id)
        {
            if (id.Length == 0)
            {
                throw new ApplicationException("Missing error identifier specification.");
            }
            ErrorLogEntry entry = ErrorLog.GetDefault(HttpContext.ApplicationInstance.Context).GetError(id);
            if (entry == null)
            {
                throw new HttpException((int)HttpStatusCode.NotFound, string.Format("Error with ID '{0}' not found.", id));
            }
            MemoryStream stream = new MemoryStream();
            TextWriter writer = new StreamWriter(stream);
            ErrorJson.Encode(entry, writer);
            writer.Flush();
            stream.Position = 0;
            return new FileStreamResult(stream, "application/json");
        }

        public ActionResult Xml(string id)
        {
            if (id.Length == 0)
            {
                throw new ApplicationException("Missing error identifier specification.");
            }
            ErrorLogEntry entry = ErrorLog.GetDefault(HttpContext.ApplicationInstance.Context).GetError(id);
            if (entry == null)
            {
                throw new HttpException((int)HttpStatusCode.NotFound, string.Format("Error with ID '{0}' not found.", id));
            }

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.NewLineOnAttributes = true;
            settings.CheckCharacters = false;
            MemoryStream stream = new MemoryStream();
            XmlWriter writer = XmlWriter.Create(stream, settings);
            writer.WriteStartDocument();
            writer.WriteStartElement("error");
            ErrorXml.Encode(entry.Error, writer);
            writer.WriteEndElement(/* error */);
            writer.WriteEndDocument();
            writer.Flush();
            stream.Position = 0;
            return new FileStreamResult(stream, "application/xml");
        }

        // Default ELMAH 

        public ActionResult Rss()
        {
            return new ElmahResult("rss");
        }

        public ActionResult DigestRss()
        {
            return new ElmahResult("digestrss");
        }

        public ActionResult Download()
        {
            return new ElmahResult("download");
        }

    }
}
