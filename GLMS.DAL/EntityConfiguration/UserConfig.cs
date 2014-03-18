using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLMS.BLL;
using GLMS.BLL.Entities;

namespace GLMS.DAL.EntityConfiguration
{
    public class UserConfig : EntityTypeConfiguration<User>
    {
        public UserConfig()
        {
            ToTable("Users");
            HasKey(x => x.UserID);

            Property(x => x.Username).IsRequired().HasMaxLength(FieldLengths.User.Username);
            Property(x => x.FirstName).IsRequired().HasMaxLength(FieldLengths.User.FirstName);
            Property(x => x.LastName).IsRequired().HasMaxLength(FieldLengths.User.LastName);

            HasOptional(x => x.Member).WithOptionalDependent(x => x.User);
        }
    }
}
