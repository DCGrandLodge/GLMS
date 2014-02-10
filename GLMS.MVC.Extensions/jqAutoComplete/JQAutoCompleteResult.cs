using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Reflection;
using System.Linq.Dynamic;
using GLMS.MVC.Extensions.DynamicQuery;

namespace GLMS.MVC.Extensions.jqAutoComplete
{
    public class JQAutoCompleteResult<T> : JsonResult
    {
        private PropertyInfo[] properties = typeof(T).GetProperties();

        public JQAutoCompleteResult(JQAutoCompleteRequest<T> Request, IQueryable<T> Data, bool DataFiltered = false)
        {
            JsonRequestBehavior = System.Web.Mvc.JsonRequestBehavior.AllowGet;
            this.Data = buildResponse(Request, Data, DataFiltered);
        }

        public JQAutoCompleteResult(IQueryable<string> Response)
        {
            JsonRequestBehavior = System.Web.Mvc.JsonRequestBehavior.AllowGet;
            this.Data = Response.Distinct();
        }

        public JQAutoCompleteResult(IQueryable<T> Data, string FieldName)
        {
            JsonRequestBehavior = System.Web.Mvc.JsonRequestBehavior.AllowGet;
            this.Data = buildResponse(new JQAutoCompleteRequest<T>() { FieldName = FieldName }, Data, false);
        }

        private IQueryable<string> buildResponse(JQAutoCompleteRequest<T> Request, IQueryable<T> QueryableData, bool DataFiltered)
        {
            IEnumerable<string> response = new List<String>();
            if (!DataFiltered)
            {
                QueryableData = FilterData(Request, QueryableData);
            }
            var result = QueryableData.Select(Request.FieldName);
            bool isStringList = result.ElementType == typeof(IEnumerable<string>);
            if (isStringList)
            {
                var expr = System.Linq.Dynamic.DynamicExpression.ParseLambda<T, IEnumerable<string>>(Request.FieldName, new object[0]);
                string key = Request.SearchTerms[Request.FieldName].Item1;
                result = QueryableData.SelectMany(expr).Where(x => x.Contains(key));
            }
            return ((IQueryable<string>)result).Distinct();
        }

        private IQueryable<T> FilterData(JQAutoCompleteRequest<T> Request, IQueryable<T> QueryableData)
        {
            if (Request == null)
            {
                return QueryableData;
            }
            else
            {
                QueryableData = QueryableData.Where(Request.SearchTerms);
                return QueryableData;
            }
        }

    }
}
