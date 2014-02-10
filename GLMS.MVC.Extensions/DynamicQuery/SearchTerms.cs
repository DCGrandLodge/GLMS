using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace GLMS.MVC.Extensions.DynamicQuery
{
    [Serializable]
    public enum SortDirection { Asc, Desc };
    [Serializable]
    public enum WhereType { Equals, StartsWith, Contains, EndsWith, LessThan, GreaterThan, Between, LessThanOrEqual, GreaterThanOrEqual, NotEqual, In }
    [Serializable]
    public class SearchTerm
    {
        public string Item1 { get; set; }
        public WhereType Item2 { get; set; }
        public SearchTerm()
        {

        }
        public SearchTerm(string Term, WhereType Operator)
        {
            // TODO: Complete member initialization
            this.Item1 = Term;
            this.Item2 = Operator;
        }
    }
    [Serializable]
    public class SearchTerms : Dictionary<string, SearchTerm>
    {
        public SearchTerm this[string index, string Default = null]
        {
            get
            {
                SearchTerm term = null;
                if (!TryGetValue(index, out term) && Default != null)
                {
                    term = new SearchTerm(Default, WhereType.Equals);
                }
                return term;
            }
        }
        public SearchTerms() : base() { }
        protected SearchTerms(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
    [Serializable]
    public class SearchTerms<T> : SearchTerms
    {
        public SearchTerms() : base() { }
        protected SearchTerms(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

}
