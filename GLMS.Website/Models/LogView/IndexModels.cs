using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GLMS.MVC.Extensions.jqGrid;

namespace GLMS.Website.Models.LogView
{
    public class IndexModel
    {
        public const string GridKey = "Elmah.ErrorLog.LogView";
        public GridModel GridModel { get; set; }
        public string ApplicationName { get; set; }

        public IndexModel()
        {
            GridModel = new GridModel("elmah-grid", GridKey);
        }
    }

    public class ErrorListModel
    {
        public string ID { get; set; }
        public string Sequence { get; set; }
        public string Host { get; set; }
        public int StatusCode { get; set; }
        public string ErrorType { get; set; }
        public string Message { get; set; }
        public string User { get; set; }
        public DateTime Time { get; set; }
    }


}