using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace GLMS.MVC.Extensions.DynamicQuery
{
    public class SearchTermsBinder<T> : IModelBinder
    {
        private static PropertyInfo[] properties = typeof(T).GetProperties();

        public static SearchTermsBinder<T> RegisterBinder()
        {
            var binder = new SearchTermsBinder<T>();
            ModelBinders.Binders[typeof(SearchTerms<T>)] = binder;
            return binder;
        }

        private Dictionary<string, WhereType> searchTypes = new Dictionary<string, WhereType>();
        internal SearchTermsBinder<T> DefaultSearchType(string fieldName, WhereType whereType)
        {
            searchTypes.Add(fieldName, whereType);
            return this;
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
            
            SearchTerms<T> searchTerms = new SearchTerms<T>();

            foreach (PropertyInfo property in properties)
            {
                ValueProviderResult value = bindingContext.ValueProvider.GetValue(property.Name);
                if ((value != null) && !String.IsNullOrEmpty(value.AttemptedValue))
                {
                    try
                    {
                        WhereType whereType;
                        if (!searchTypes.TryGetValue(property.Name, out whereType))
                        {
                            whereType = WhereType.Contains;
                        }
                        ValueProviderResult opResult = bindingContext.ValueProvider.GetValue(property.Name + ".Op");
                        if (opResult != null)
                        {
                            switch (opResult.AttemptedValue)
                            {
                                case "in":
                                    whereType = WhereType.In;
                                    break;
                                case "<=":
                                    whereType = WhereType.LessThanOrEqual;
                                    break;
                                case ">=":
                                    whereType = WhereType.GreaterThanOrEqual;
                                    break;
                                case "!=":
                                    whereType = WhereType.NotEqual;
                                    break;
                                case ">":
                                    whereType = WhereType.GreaterThan;
                                    break;
                                case "<":
                                    whereType = WhereType.LessThan;
                                    break;
                                case "between":
                                    whereType = WhereType.Between;
                                    break;
                                case "^":
                                    whereType = WhereType.StartsWith;
                                    break;
                                case "$":
                                    whereType = WhereType.EndsWith;
                                    break;
                                case "=":
                                    whereType = WhereType.Equals;
                                    break;
                            }
                        }
                        searchTerms.Add(property.Name, new SearchTerm(value.AttemptedValue, whereType));
                    }
                    catch (Exception)
                    {
                        // DO NOTHING - we don't even really need to log this.
                    }
                }
            }
            return searchTerms;
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
