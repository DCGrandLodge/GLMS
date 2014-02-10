using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLMS.BLL.Entities;

namespace GLMS.DAL.EntityConfiguration
{
    public class DegreeConfig : EntityTypeConfiguration<Degree>
    {
        public DegreeConfig()
        {
            ToTable("Degree");
            HasKey(x => x.DegreeID);
            Property(x => x.Name).IsRequired().HasMaxLength(120);
            Property(x => x.Abbv).IsRequired().HasMaxLength(16);
        }
    }
}
