using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLMS.BLL.Entities;

namespace GLMS.DAL.EntityConfiguration
{
    public class MemberConfig : EntityTypeConfiguration<Member>
    {
        public MemberConfig()
        {
            ToTable("Member");
            HasKey(x => x.MemberID);

            Property(x => x.FirstName).HasMaxLength(120);
            Property(x => x.MiddleName).HasMaxLength(120);
            Property(x => x.LastName).HasMaxLength(120);
        }
    }
}
