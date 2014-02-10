using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Reflection;
using System.Web.Script.Serialization;
using GLMS.MVC.Extensions.DynamicQuery;
using GLMS.MVC.Extensions.jqAutoComplete;

namespace GLMS.MVC.Extensions.jqGrid
{
    /// <summary>
    /// This type is the ModelBinder for jquery.jqgrid
    /// Add this type to MVC modelbinders to bind jquery.jqgrid
    /// requests to <see cref="JQDataTable"/>
    /// </summary>
    public class JQGridDataRequestBinder<T> : IModelBinder
    {
        private static PropertyInfo[] properties = typeof(T).GetProperties();

        public static JQGridDataRequestBinder<T> RegisterBinder(bool withAutocomplete = true)
        {
            var binder = new JQGridDataRequestBinder<T>();
            ModelBinders.Binders[typeof(JQGridDataRequest<T>)] = binder;
            SearchTermsBinder<T>.RegisterBinder();
            if (withAutocomplete)
            {
                JQAutoCompleteRequestBinder<T>.RegisterBinder();
            }
            return binder;
        }

        #region Fluent Constructors
        private Dictionary<string, string> sortPairs = new Dictionary<string, string>();
        public JQGridDataRequestBinder<T> WithPairedSort(string ifPrimary, string thenSecondary, bool isSymmetric = true)
        {
            sortPairs.Add(ifPrimary, thenSecondary);
            if (isSymmetric)
            {
                sortPairs.Add(thenSecondary, ifPrimary);
            }
            return this;
        }

        internal string sessionKey { get; private set; }
        public JQGridDataRequestBinder<T> StoreInSession(string sessionKey)
        {
            this.sessionKey = sessionKey;
            return this;
        }

        public JQGridDataRequestBinder<T> DefaultSearchType(string fieldName, WhereType whereType)
        {
            var searchBinder = ModelBinders.Binders.GetBinder(typeof(SearchTerms<T>)) as SearchTermsBinder<T>;
            searchBinder.DefaultSearchType(fieldName, whereType);
            return this;
        }

        public JQGridDataRequestBinder<T> InjectAutocomplete()
        {
            var binder = ModelBinders.Binders.GetBinder(typeof(JQAutoCompleteRequest<T>)) as JQAutoCompleteRequestBinder<T>;
            if (binder == null)
            {
                binder = JQAutoCompleteRequestBinder<T>.RegisterBinder();
            }
            binder.InjectFromSession(this);
            return this;
        }
        #endregion

        public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            if (controllerContext == null)
            {
                throw new ArgumentNullException("controllerContext");
            }

            if (bindingContext == null)
            {
                throw new ArgumentNullException("bindingContext");
            }

            JQGridDataRequest<T> gridDataRequest = new JQGridDataRequest<T>();

            gridDataRequest.JQGridID = GetBindingValue(bindingContext, "jqGridID");
            if (string.IsNullOrEmpty(gridDataRequest.JQGridID))
            {
                throw new ArgumentException("jqGridID must always be provided");
            }

            gridDataRequest.IsSearch = bool.Parse(GetBindingValue(bindingContext, "_search"));
            gridDataRequest.nd = long.Parse(GetBindingValue(bindingContext, "nd"));
            gridDataRequest.PageSize = int.Parse(GetBindingValue(bindingContext, "rows"));
            gridDataRequest.Page = int.Parse(GetBindingValue(bindingContext, "page"));
            string sortFieldVal = GetBindingValue(bindingContext, "sidx");
            if (!String.IsNullOrEmpty(sortFieldVal))
            {
                string[] sortFields = sortFieldVal.Split(',');
                foreach (var field in sortFields)
                {
                    string sortField = field.Trim();
                    SortDirection sortDirection = SortDirection.Asc;
                    if ("desc".Equals(GetBindingValue(bindingContext, "sord"), StringComparison.OrdinalIgnoreCase))
                    {
                        sortDirection = SortDirection.Desc;
                    }
                    if (sortField.EndsWith(" asc"))
                    {
                        sortDirection = SortDirection.Asc;
                        sortField = sortField.Substring(0, sortField.Length - 4);
                    }
                    else if (sortField.EndsWith(" desc"))
                    {
                        sortDirection = SortDirection.Desc;
                        sortField = sortField.Substring(0, sortField.Length - 5);
                    }
                    gridDataRequest.SortFields.Add(new SortField(sortField, sortDirection));
                }
            }
            foreach (var kvp in sortPairs)
            {
                gridDataRequest.ForcePairedSort(kvp.Key, kvp.Value, symmetric: false);
            }
            var searchBinder = ModelBinders.Binders.GetBinder(typeof(SearchTerms<T>));
            gridDataRequest.SearchTerms = searchBinder.BindModel(controllerContext, bindingContext) as SearchTerms;
            if (sessionKey != null)
            {
                controllerContext.HttpContext.Session[sessionKey] = gridDataRequest;
            }
            return gridDataRequest;
        }

        private static string GetBindingValue(ModelBindingContext bindingContext, string key)
        {
            var bindingValue = bindingContext.ValueProvider.GetValue(key);
            if (bindingValue != null)
            {
                return bindingValue.AttemptedValue;
            }
            return null;
        }
    }
}
