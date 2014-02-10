using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLMS.BLL.Entities;

namespace GLMS.DAL.EntityConfiguration
{
    public class AddressConfig : ComplexTypeConfiguration<Address>
    {
        public AddressConfig()
        {
            Property(x => x.Street).HasMaxLength(128);
            Property(x => x.City).HasMaxLength(128);
            Property(x => x.State).HasMaxLength(2);
            Property(x => x.Zip).HasMaxLength(10);
            Property(x => x.Country).HasMaxLength(128);
        }
    }
}
