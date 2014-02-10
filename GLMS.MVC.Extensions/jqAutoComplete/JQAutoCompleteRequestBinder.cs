using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Reflection;
using GLMS.MVC.Extensions.DynamicQuery;
using GLMS.MVC.Extensions.jqGrid;

namespace GLMS.MVC.Extensions.jqAutoComplete
{
    public class JQAutoCompleteRequestBinder : IModelBinder
    {
        public static void RegisterBinder()
        {
            ModelBinders.Binders[typeof(JQAutoCompleteRequest)] = new JQAutoCompleteRequestBinder();
        }

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

            JQAutoCompleteRequest autoCompleteRequest = new JQAutoCompleteRequest();
            autoCompleteRequest.Term = GetBindingValue(bindingContext, "term");
            return autoCompleteRequest;
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

    public class JQAutoCompleteRequestBinder<T> : IModelBinder
    {
        private static PropertyInfo[] properties = typeof(T).GetProperties();

        public static JQAutoCompleteRequestBinder<T> RegisterBinder()
        {
            var binder = new JQAutoCompleteRequestBinder<T>();
            ModelBinders.Binders[typeof(JQAutoCompleteRequest<T>)] = binder;
            return binder;
        }

        private JQGridDataRequestBinder<T> gridBinder;
        internal void InjectFromSession(JQGridDataRequestBinder<T> jQGridDataRequestBinder)
        {
            this.gridBinder = jQGridDataRequestBinder;
        }

        private string sessionKey;
        internal void InjectFromSession(string sessionKey)
        {
            this.sessionKey = sessionKey;
        }

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

            JQAutoCompleteRequest<T> autoCompleteRequest = new JQAutoCompleteRequest<T>();
            
            string id = GetBindingValue(bindingContext, "id");
            string term = GetBindingValue(bindingContext, "term");
            autoCompleteRequest.FieldName = id;
            if (!(String.IsNullOrEmpty(id) || String.IsNullOrEmpty(term)))
            {
                autoCompleteRequest.SearchTerms.Add(id, new SearchTerm(term,WhereType.Contains));
            }
            if (sessionKey != null)
            {
                autoCompleteRequest.InjectSearchTerms(sessionKey);
            }
            if (gridBinder != null)
            {
                autoCompleteRequest.InjectSearchTerms(gridBinder.sessionKey);
            }
            return autoCompleteRequest;
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
