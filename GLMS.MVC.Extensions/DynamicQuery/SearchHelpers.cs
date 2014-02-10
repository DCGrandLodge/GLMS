using System;
using System.Linq.Dynamic;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.Serialization;

namespace GLMS.MVC.Extensions.DynamicQuery
{
    public static class QueryableWhereExtensions
    {
        public static IQueryable<T> Where<T>(this IQueryable<T> source, SearchTerms SearchTerms)
        {
            var compiled = QueryableWhere.GetQueryableWhere(typeof(T), SearchTerms);
            if (compiled != null)
            {
                return source.Where(compiled.Where, compiled.Parameters);
            }
            return source;
        }
    }

    internal sealed class QueryableWhere
    {
        private static List<WhereType> stringWhereTypes = new List<WhereType>() { WhereType.Equals, WhereType.StartsWith, WhereType.Contains, WhereType.EndsWith, WhereType.NotEqual, WhereType.In };
        private static List<WhereType> ordinalWhereTypes = new List<WhereType>() { WhereType.Equals, WhereType.LessThan, WhereType.GreaterThan, WhereType.Between, WhereType.LessThanOrEqual, WhereType.GreaterThanOrEqual, WhereType.NotEqual};
        public string Where { get; set; }
        public object[] Parameters { get; set; }

        private QueryableWhere() { }

        public static QueryableWhere GetQueryableWhere(Type T, SearchTerms SearchTerms)
        {
            if (SearchTerms == null)
            {
                return null;
            }
            PropertyInfo[] properties = T.GetProperties();
            validateColumns(properties,SearchTerms.Keys.AsEnumerable());
            string where = null;
            List<object> searchParams = new List<object>();
            foreach (KeyValuePair<string, SearchTerm> searchTerm in SearchTerms)
            {
                if (!String.IsNullOrEmpty(searchTerm.Value.Item1))
                {
                    PropertyInfo propInfo = properties.Single(prop => prop.Name.Equals(searchTerm.Key));
                    WhereType whereType = WhereType.Equals;
                    bool whereAny = false;
                    Type propertyType = propInfo.PropertyType;
                    if (propertyType.IsGenericType)
                    {
                        Type genType = propertyType.GetGenericTypeDefinition();
                        if (genType == typeof(IEnumerable<>))
                        {
                            whereAny = true;
                            propertyType = propertyType.GetGenericArguments()[0];
                        }
                        if (genType == typeof(Nullable<>))
                        {
                            propertyType = propertyType.GetGenericArguments()[0];
                        }
                    }
                    if (propertyType == typeof(System.String))
                    {
                        if (stringWhereTypes.Contains(searchTerm.Value.Item2))
                        {
                            whereType = searchTerm.Value.Item2;
                        }
                    }
                    else if (ordinalWhereTypes.Contains(searchTerm.Value.Item2))
                    {
                        whereType = searchTerm.Value.Item2;
                    }
                    try
                    {
                        object changedType = whereType == WhereType.Between ? null : Convert.ChangeType(searchTerm.Value.Item1, propertyType);
                        switch (whereType)
                        {
                            case WhereType.Equals:
                            case WhereType.StartsWith:
                            case WhereType.EndsWith:
                            case WhereType.Contains:
                                string subWhere = "{0}{1}{2}" +
                                    (whereAny ? ".Any({3}(@{4}))" : whereType == WhereType.Equals ? " == @{4}" : ".{3}(@{4})");
                                where = String.Format(subWhere, where ?? "", where == null ? "" : " and ", searchTerm.Key, whereType.ToString(), searchParams.Count);
                                searchParams.Add(changedType);
                                break;
                            case WhereType.NotEqual:
                            case WhereType.LessThan:
                            case WhereType.GreaterThan:
                            case WhereType.LessThanOrEqual:
                            case WhereType.GreaterThanOrEqual:
                                where = String.Format("{0}{1}{2} {3} @{4}", where ?? "", where == null ? "" : " and ", searchTerm.Key, getOperator(whereType), searchParams.Count);
                                searchParams.Add(changedType);
                                break;
                            case WhereType.Between:
                                string[] args = searchTerm.Value.Item1.Split(',');
                                where = String.Format("{0}{1}({2} >= @{3} and {2} <= @{4})", where ?? "", where == null ? "" : " and ", searchTerm.Key, searchParams.Count, searchParams.Count + 1);
                                changedType = Convert.ChangeType(args[0], propertyType);
                                searchParams.Add(changedType);
                                changedType = Convert.ChangeType(args[1], propertyType);
                                searchParams.Add(changedType);
                                break;
                            case WhereType.In:
                                string[] inargs = searchTerm.Value.Item1.Split(',');
                                where = String.Format("{0}{1}{2} in @{3}", where ?? "", where == null ? "" : " and ", searchTerm.Key, searchParams.Count);
                                searchParams.Add(inargs);
                                break;
                        }
                    }
                    catch (Exception)
                    {
                        // If we can't convert the value to the appropriate type, just don't use that search term.
                    }
                }
            }
            if ((where != null) && (searchParams.Count > 0))
            {
                return new QueryableWhere() { Where = where, Parameters = searchParams.ToArray() };
            }
            else
            {
                return null;
            }
        }

        private static object getOperator(WhereType whereType)
        {
            switch (whereType)
            {
                case WhereType.NotEqual: return "!=";
                case WhereType.LessThan: return "<";
                case WhereType.GreaterThan: return ">";
                case WhereType.LessThanOrEqual: return "<=";
                case WhereType.GreaterThanOrEqual: return ">=";
                default: return "==";
            }
        }

        private static void validateColumns(PropertyInfo[] properties, IEnumerable<string> columns)
        {
            foreach (string column in columns)
            {
                if (!properties.Any(prop => prop.Name.Equals(column)))
                {
                    throw new ArgumentOutOfRangeException(column, "Column does not exist in underlying dataset");
                }
            }
        }
    }

}
