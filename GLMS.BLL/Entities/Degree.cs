using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLMS.BLL.Entities
{
    public class Degree
    {
        public Guid DegreeID { get; set; }
        public int Number { get; set; }
        public string Name { get; set; }
        public string Abbr { get; set; }
    }
}
