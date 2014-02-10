using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLMS.BLL.Entities
{
    public class Office
    {
        public Guid OfficeID { get; set; }
        public string Title { get; set; }
        public string Abbr { get; set; }
    }
}
