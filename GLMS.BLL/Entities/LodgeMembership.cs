using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLMS.BLL.Entities
{
    public class LodgeMembership
    {
        public Guid LodgeID { get; set; }
        public Guid MemberID { get; set; }
        public MemberStatus Status { get; set; }

        // Petition
        public PetitionType? PetitionType { get; set; }
        public DateTime? PetitionDate { get; set; }
        public DateTime? ElectedDate { get; set; }
        public DateTime? RejectedDate { get; set; }

        // Membership
        public DateTime? AffiliatedDate { get; set; }
        public DateTime? HonoraryDate { get; set; }
        public DateTime? DemitDate { get; set; }
        public DateTime? WithdrawDate { get; set; }
        public DateTime? NPDDate { get; set; }
        public DateTime? ExpelledDate { get; set; }
        public DateTime? ReinstatedDate { get; set; }

        public virtual Lodge Lodge { get; set; }
        public virtual Member Member { get; set; }
    }
}
