using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLMS.BLL.Entities;

namespace GLMS.DAL.EntityConfiguration
{
    public class LodgeOfficerConfig : EntityTypeConfiguration<LodgeOfficer>
    {
        public LodgeOfficerConfig()
        {
            ToTable("LodgeOfficers");
            HasKey(x => new { x.LodgeID, x.MemberID });

            HasRequired(x => x.Lodge).WithMany(x => x.Officers).HasForeignKey(x => x.LodgeID);
            HasRequired(x => x.Member).WithMany().HasForeignKey(x => x.MemberID);
            HasRequired(x => x.LodgeOffice).WithMany().HasForeignKey(x => x.LodgeOfficeID);
        }
    }
}
