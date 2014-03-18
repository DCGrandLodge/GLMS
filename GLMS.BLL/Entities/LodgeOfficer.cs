using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLMS.BLL.Entities
{
    public class LodgeOfficer
    {
        public Guid LodgeOfficerID { get; set; }
        public Guid LodgeID { get; set; }
        public Guid MemberID { get; set; }
        public Guid LodgeOfficeID { get; set; }

        public bool Proxy { get; set; }
        public bool Honorary { get; set; }
        public bool PastOfficer { get; set; }
        public bool Emeritus { get; set; }

        public bool Appointed { get; set; }
        public DateTime DateElected { get; set; }
        public DateTime? DateInstalled { get; set; }

        public virtual Lodge Lodge { get; set; }
        public virtual Member Member { get; set; }
        public virtual Office LodgeOffice { get; set; }
    }
}
