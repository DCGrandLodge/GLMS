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
    public class LodgeConfig : EntityTypeConfiguration<Lodge>
    {
        public LodgeConfig()
        {
            ToTable("Lodge");
            HasKey(x => x.LodgeID);
            Property(x => x.Name).IsRequired().HasMaxLength(FieldLengths.Lodge.Name);
            Property(x => x.PhoneNumber).HasMaxLength(FieldLengths.Lodge.Phone);
            Property(x => x.MeetingDates).HasMaxLength(FieldLengths.Lodge.MeetingDate);

            Property(x => x.UnderDispensation).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Computed);
            Property(x => x.Chartered).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Computed);
            Property(x => x.Dark).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Computed);
            Property(x => x.Merged).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Computed);
            Property(x => x.Status).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Computed);
            Property(x => x.StatusDate).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Computed);

            HasOptional(x => x.MergedWithLodge).WithMany(x => x.MergedLodges).HasForeignKey(x => x.MergedWithLodgeID).WillCascadeOnDelete(false);
        }
    }
}
