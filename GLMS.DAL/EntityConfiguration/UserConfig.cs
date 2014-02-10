using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLMS.BLL.Entities;

namespace GLMS.DAL.EntityConfiguration
{
    public class UserConfig : EntityTypeConfiguration<User>
    {
        public UserConfig()
        {
            ToTable("Users");
            HasKey(x => x.UserID);

            Property(x => x.Username).HasMaxLength(64).IsRequired();
            Property(x => x.FirstName).HasMaxLength(120).IsRequired();
            Property(x => x.LastName).HasMaxLength(120).IsRequired();

            HasOptional(x => x.Member).WithOptionalDependent(x => x.User);
        }
    }
}
