using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLMS.BLL.Entities
{
    public class MemberDegree
    {
        public Guid MemberID { get; set; }
        public Guid DegreeID { get; set; }
        public DateTime? Date { get; set; }
        public Guid? LodgeID { get; set; }

        public virtual Member Member { get; set; }
        public virtual Degree Degree { get; set; }
        public virtual Lodge Lodge { get; set; }
    }
}
