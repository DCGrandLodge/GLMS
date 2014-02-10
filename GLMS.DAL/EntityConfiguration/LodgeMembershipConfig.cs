using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLMS.BLL.Entities;

namespace GLMS.DAL.EntityConfiguration
{
    public class LodgeMembershipConfig : EntityTypeConfiguration<LodgeMembership>
    {
        public LodgeMembershipConfig()
        {
            ToTable("LodgeMembership");
            HasKey(x => new { x.LodgeID, x.MemberID });

            HasRequired(x => x.Lodge).WithMany(x => x.LodgeMembership).HasForeignKey(x => x.LodgeID);
            HasRequired(x => x.Member).WithMany(x => x.LodgeMembership).HasForeignKey(x => x.MemberID);            
        }
    }
}
