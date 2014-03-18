using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLMS.BLL;
using GLMS.BLL.Entities;

namespace GLMS.DAL.EntityConfiguration
{
    public class MemberConfig : EntityTypeConfiguration<Member>
    {
        public MemberConfig()
        {
            ToTable("Member");
            HasKey(x => x.MemberID);

            Property(x => x.FirstName).HasMaxLength(FieldLengths.Member.FirstName);
            Property(x => x.MiddleName).HasMaxLength(FieldLengths.Member.MiddleName);
            Property(x => x.LastName).HasMaxLength(FieldLengths.Member.LastName);

            Property(x => x.FullName).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Computed);
        }
    }
}
