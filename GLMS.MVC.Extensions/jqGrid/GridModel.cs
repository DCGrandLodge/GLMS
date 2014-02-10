using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Runtime.Serialization;
using Elmah;
using GLMS.MVC.Extensions.DynamicQuery;

namespace GLMS.MVC.Extensions.jqGrid
{
    [Serializable]
    public class GridModel
    {
        public static int DefaultPageSize = 10;
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanCreate { get; set; }
        public string JQGridID { get; set; }
        public int PageSize { get; set; }
        public int Page { get; set; }
        public List<SortField> SortFields { get; set; }
        public SearchTerms SearchTerms { get; set; }

        public GridModel(string JQGridID) : this(JQGridID, null) { }

        public GridModel(string JQGridID, string cachedGridKey)
        {
            this.JQGridID = JQGridID;
            SortFields = new List<SortField>();
            SearchTerms = new SearchTerms();

            JQGridDataRequest gridRequest = null;
            if (!String.IsNullOrEmpty(cachedGridKey))
            {
                if (HttpContext.Current != null && HttpContext.Current.Session != null)
                {
                    try
                    {
                        gridRequest = (JQGridDataRequest)HttpContext.Current.Session[cachedGridKey];
                    }
                    catch (SerializationException ex)
                    {
                        ex.Log();
                    }
                }
            }
            if (gridRequest != null)
            {
                Page = gridRequest.Page;
                PageSize = gridRequest.PageSize;
                foreach (SortField field in gridRequest.SortFields)
                {
                    SortFields.Add(field);
                }
                foreach (KeyValuePair<string, SearchTerm> field in gridRequest.SearchTerms)
                {
                    SearchTerms.Add(field.Key, field.Value);
                }
            }
            else
            {
                Page = 1;
            }
            if (PageSize < DefaultPageSize)
            {
                PageSize = DefaultPageSize;
            }
        }

        public GridModel DefaultSort(string fieldName, SortDirection sortDirection)
        {
            if (!SortFields.Any())
            {
                SortFields.Add(new SortField(fieldName, sortDirection));
            }
            return this;
        }
    }
}
