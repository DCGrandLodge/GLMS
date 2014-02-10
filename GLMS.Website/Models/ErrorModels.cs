using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GLMS.Website.Models
{
    public class Http404Model
    {
        public string RequestedUrl { get; set; }
    }

    public class GLMSException : Exception
    {
    }
}