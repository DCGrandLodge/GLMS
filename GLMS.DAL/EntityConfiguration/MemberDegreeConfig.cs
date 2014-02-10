using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLMS.BLL.Entities;

namespace GLMS.DAL.EntityConfiguration
{
    public class MemberDegreeConfig : EntityTypeConfiguration<MemberDegree>
    {
        public MemberDegreeConfig()
        {
            ToTable("MemberDegree");
            HasKey(x => new { x.MemberID, x.DegreeID });
            HasRequired(x => x.Member).WithMany(x => x.DegreeDates).HasForeignKey(x => x.MemberID);
            HasRequired(x => x.Degree).WithMany().HasForeignKey(x => x.DegreeID);
            HasOptional(x => x.Lodge).WithMany().HasForeignKey(x => x.LodgeID);
        }
    }
}
