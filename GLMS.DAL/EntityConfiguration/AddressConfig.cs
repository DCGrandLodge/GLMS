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
    public class AddressConfig : ComplexTypeConfiguration<Address>
    {
        public AddressConfig()
        {
            Property(x => x.Street).HasMaxLength(FieldLengths.Address.Street);
            Property(x => x.City).HasMaxLength(FieldLengths.Address.City);
            Property(x => x.State).HasMaxLength(FieldLengths.Address.State);
            Property(x => x.Zip).HasMaxLength(FieldLengths.Address.Zip);
            Property(x => x.Country).HasMaxLength(FieldLengths.Address.Country);
        }
    }
}
