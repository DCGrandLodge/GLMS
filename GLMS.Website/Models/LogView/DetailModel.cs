using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;
using System.Collections.Specialized;

namespace GLMS.Website.Models.LogView
{
    public class DetailModel
    {
        public static readonly Regex Regex = new Regex(@"
                ^
                \s*
                \w+ \s+ 
                (?<type> .+ ) \.
                (?<method> .+? ) 
                (?<params> \( (?<params> .*? ) \) )
                ( \s+ 
                \w+ \s+ 
                  (?<file> [a-z] \: .+? ) 
                  \: \w+ \s+ 
                  (?<line> [0-9]+ ) \p{P}? )?
                \s*
                $",
            RegexOptions.IgnoreCase
            | RegexOptions.Multiline
            | RegexOptions.ExplicitCapture
            | RegexOptions.CultureInvariant
            | RegexOptions.IgnorePatternWhitespace
            | RegexOptions.Compiled);

        public string ID { get; set; }
        public string Sequence { get; set; }
        public string Host { get; set; }
        public int StatusCode { get; set; }
        public string ErrorType { get; set; }
        public string Message { get; set; }
        public string User { get; set; }
        public DateTime Time { get; set; }
        public string Detail { get; set; }
        public bool Resolved { get; set; }

        public NameValueCollection ServerVariables { get; set; }
        public NameValueCollection QueryString { get; set; }
        public NameValueCollection Form { get; set; }

        public NameValueCollection EntityValidationErrors { get; set; }
    }
}