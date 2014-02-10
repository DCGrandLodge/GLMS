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
using System.ComponentModel.DataAnnotations;
using System.Linq.Dynamic;
using System.Collections;
using GLMS.MVC.Extensions.DynamicQuery;

namespace GLMS.MVC.Extensions.jqGrid
{
    public class JQGridDataResult<T> : JsonDataResult
    {
        private PropertyInfo[] properties = typeof(T).GetProperties();

        public JQGridDataResult(JQGridDataRequest<T> Request, IQueryable<T> Data, bool DataFiltered = false, params string[] Columns)
        {
            JsonRequestBehavior = System.Web.Mvc.JsonRequestBehavior.AllowGet;
            this.Data = buildResponse(Request, Data, DataFiltered, Columns);
        }

        public JQGridDataResult(JQGridDataResponse<T> response)
        {
            JsonRequestBehavior = System.Web.Mvc.JsonRequestBehavior.AllowGet;
            this.Data = response;
        }

        public JQGridDataResult(IQueryable<T> data, params string[] Columns)
        {
            JsonRequestBehavior = System.Web.Mvc.JsonRequestBehavior.AllowGet;
            this.Data = buildResponse(null, data, true, Columns);
        }

        private JQGridDataResponse<T> buildResponse(JQGridDataRequest<T> Request, IQueryable<T> QueryableData, bool DataFiltered, string[] Columns)
        {
            JQGridDataResponse<T> response = buildResponseHeader(Request, ref QueryableData, DataFiltered);
            foreach (T record in QueryableData)
            {
                response.Rows.Add(new JQGridDataRow(record, properties, Columns));
            }
            return response;
        }

        private JQGridDataResponse<T> buildResponseHeader(JQGridDataRequest<T> Request, ref IQueryable<T> QueryableData, bool DataFiltered)
        {
            JQGridDataResponse<T> response = new JQGridDataResponse<T>();
            if (!DataFiltered)
            {
                QueryableData = FilterData(Request, QueryableData);
            }
            response.RecordCount = QueryableData.Count();
            if (Request == null || (Request.PageSize == -1))
            {
                response.PageSize = response.RecordCount;
                response.Page = 1;
            }
            else
            {
                response.PageSize = Request.PageSize;
                response.Page = Request.Page;
                if (Request.Page > response.PageCount)
                {
                    response.Page = response.PageCount;
                }
                if (response.RecordCount > 0)
                {
                    QueryableData = QueryableData.Skip((response.Page - 1) * response.PageSize).Take(response.PageSize);
                }
            }
            return response;
        }

        private IQueryable<T> FilterData(JQGridDataRequest<T> Request, IQueryable<T> QueryableData)
        {
            if (Request == null)
            {
                return QueryableData.OrderBy(String.Format("{0} Asc", properties[0].Name));
            }
            else
            {
                if (Request.SortFields != null)
                {
                    QueryableData = QueryableData.OrderBy(Request.GetQueryableOrderBy());
                }
                QueryableData = QueryableData.Where(Request.SearchTerms);
                return QueryableData;
            }
        }
    }
}
