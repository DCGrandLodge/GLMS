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
    public class ZipCodeConfig : EntityTypeConfiguration<ZipCode>
    {
        public ZipCodeConfig()
        {
            ToTable("ZipCode");
            HasKey(x => x.Zip);
            Property(x => x.Zip).IsRequired().HasMaxLength(FieldLengths.ZipCode.Zip);
            Property(x => x.City).IsRequired().HasMaxLength(FieldLengths.ZipCode.City);
            Property(x => x.State).IsRequired().HasMaxLength(FieldLengths.ZipCode.State);
            Property(x => x.StateAbbr).IsRequired().HasMaxLength(FieldLengths.ZipCode.StateAbbr);
        }
    }
}
