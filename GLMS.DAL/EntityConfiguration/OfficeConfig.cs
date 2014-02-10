﻿using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLMS.BLL.Entities;

namespace GLMS.DAL.EntityConfiguration
{
    public class OfficeConfig : EntityTypeConfiguration<Office>
    {
        public OfficeConfig()
        {
            ToTable("Office");
            HasKey(x => x.OfficeID);
            Property(x => x.Title).HasMaxLength(120);
            Property(x => x.Abbr).HasMaxLength(16);
        }
    }
}
