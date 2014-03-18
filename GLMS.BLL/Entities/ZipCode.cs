using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLMS.BLL.Entities
{
    public class ZipCode
    {
        public string Zip { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string StateAbbr { get; set; }
    }
}
