using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLMS.BLL.Entities;

namespace GLMS.DAL.EntityConfiguration
{
    public class LodgeConfig : EntityTypeConfiguration<Lodge>
    {
        public LodgeConfig()
        {
            ToTable("Lodge");
            HasKey(x => x.LodgeID);
            Property(x => x.Name).IsRequired().HasMaxLength(120);
            Property(x => x.PhoneNumber).HasMaxLength(20);
            Property(x => x.MeetingDates).HasMaxLength(64);

            Property(x => x.UnderDispensation).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Computed);
            Property(x => x.Chartered).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Computed);
            Property(x => x.Dark).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Computed);
            Property(x => x.Merged).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Computed);
            Property(x => x.Status).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Computed);

            HasOptional(x => x.MergedWithLodge).WithMany(x => x.MergedLodges).HasForeignKey(x => x.MergedWithLodgeID).WillCascadeOnDelete(false);
        }
    }
}
