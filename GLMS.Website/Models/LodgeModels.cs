using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GLMS.MVC.Extensions.jqGrid;

namespace GLMS.Website.Models
{
    public class LodgeIndexModel : GridModel
    {
        public class Data
        {
            public Guid LodgeID { get; set; }
            public int Number { get; set; }
            public string Name { get; set; }
            public string Address { get; set; }
            public string Status { get; set; }
        }

        public static string GridKey = "LodgeIndex";
        public LodgeIndexModel() : base("lodge-index", GridKey)
        {

        }
    }
}