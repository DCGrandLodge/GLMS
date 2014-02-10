using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using GLMS.MVC.Extensions.DynamicQuery;

namespace GLMS.MVC.Extensions.jqGrid
{
    [Serializable]
    public class SortField
    {
        public string FieldName { get; set; }
        public SortDirection Direction { get; set; }
        public SortField(string FieldName, SortDirection Direction)
        {
            this.FieldName = FieldName;
            this.Direction = Direction;
        }
    }
    [Serializable]
    public abstract class JQGridDataRequest
    {
        public string JQGridID { get; set; }
        public bool IsSearch { get; set; }
        public long nd { get; set; }
        public int PageSize { get; set; }
        public int Page { get; set; }
        public List<SortField> SortFields { get; set; }
        public SearchTerms SearchTerms { get; set; }
        public string this[string index]
        {
            get
            {
                SearchTerm term;
                if (SearchTerms.TryGetValue(index, out term))
                {
                    return term.Item1;
                }
                return null;
            }
        }

        public JQGridDataRequest()
        {
            IsSearch = false;
            PageSize = -1;
            Page = 1;
            SearchTerms = new SearchTerms();
            SortFields = new List<SortField>();
        }
    }

    // This class has to be generic because the model binder has to be generic so it can locate the search
    // terms based on the underlying model type.
    [Serializable]
    public class JQGridDataRequest<T> : JQGridDataRequest
    {

        private static Type modelType = typeof(T);
        private static PropertyInfo[] properties = modelType.GetProperties();

        public Type ModelType { get { return modelType; } }

        public JQGridDataResult<T> BuildResult(IQueryable<T> data)
        {
            return new JQGridDataResult<T>(this, data);
        }

        public void ForcePairedSort(string FieldA, string FieldB, SortDirection? bDirection = null, bool symmetric = true)
        {
            var sortA = SortFields.FirstOrDefault(f => f.FieldName.Equals(FieldA));
            var sortB = SortFields.FirstOrDefault(f => f.FieldName.Equals(FieldB));
            if (sortA != null && sortB == null)
            {
                SortFields.Add(new SortField(FieldB, bDirection ?? sortA.Direction));
            }
            else if (symmetric && sortA == null && sortB != null)
            {
                SortFields.Add(new SortField(FieldA, bDirection ?? sortB.Direction));
            }
        }

        public void validateColumns()
        {
            validateColumns(SearchTerms.Keys.AsEnumerable());
            validateColumns(SortFields.Select(f => f.FieldName));
        }
        
        private void validateColumns(IEnumerable<string> columns)
        {
            foreach (string column in columns)
            {
                if (!properties.Any(prop => prop.Name.Equals(column)))
                {
                    throw new ArgumentOutOfRangeException(column, "Column does not exist in underlying dataset");
                }
            }
        }

        public string GetQueryableOrderBy()
        {
            validateColumns(SortFields.Select(f => f.FieldName));
            string order = null;
            foreach (SortField sortField in SortFields)
            {
                order = String.Format("{0}{1}{2} {3}", order ?? "", order == null ? "" : ",", sortField.FieldName, sortField.Direction.ToString());
            }
            if (order == null)
            {
                order = String.Format("{0} Asc", properties[0].Name);
            }
            return order;
        }
    }
}
