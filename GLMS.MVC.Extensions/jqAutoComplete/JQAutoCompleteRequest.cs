using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Web;
using GLMS.MVC.Extensions.DynamicQuery;
using GLMS.MVC.Extensions.jqGrid;

namespace GLMS.MVC.Extensions.jqAutoComplete
{
    [Serializable]
    public class JQAutoCompleteRequest
    {
        public string Term { get; set; }

        public JQAutoCompleteRequest<T> ForField<T>(string fieldName) where T : class
        {
            JQAutoCompleteRequest<T> result = new JQAutoCompleteRequest<T>()
            {
                FieldName = fieldName,
            };
            result.SearchTerms.Add(fieldName, new SearchTerm(Term,WhereType.Contains));
            return result;
        }
    }

    // This class has to be generic because the model binder has to be generic so it can locate the search
    // terms based on the underlying model type.
    [Serializable]
    public class JQAutoCompleteRequest<T>
    {
        private static Type modelType = typeof(T);

        public string FieldName { get; set; }
        public int Limit { get; set; }
        public SearchTerms SearchTerms { get; private set; }

        public JQAutoCompleteRequest()
        {
            SearchTerms = new SearchTerms();
        }

        public void InjectSearchTerms(string gridSessionKey)
        {
            if (HttpContext.Current != null && HttpContext.Current.Session != null && !String.IsNullOrWhiteSpace(gridSessionKey))
            {
                JQGridDataRequest grid = (JQGridDataRequest)HttpContext.Current.Session[gridSessionKey];
                if (grid != null)
                {
                    InjectSearchTerms(grid.SearchTerms);
                }
            }
        }

        public void InjectSearchTerms(SearchTerms SearchTerms)
        {
            foreach (string term in SearchTerms.Keys)
            {
                if (!this.SearchTerms.ContainsKey(term))
                {
                    this.SearchTerms.Add(term, SearchTerms[term]);
                }
            }
        }
    }
}
