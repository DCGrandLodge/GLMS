using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLMS.BLL.Entities;

namespace GLMS.DAL.Migrations.Seed
{
    public class DegreeData
    {
        public static void Seed(GLMSContext context)
        {
            context.Degrees.AddOrUpdate(x => x.Number,
                new Degree() { DegreeID = Guid.NewGuid(), Number = 1, Name = "Entered Apprentice", Abbv = "EA" },
                new Degree() { DegreeID = Guid.NewGuid(), Number = 2, Name = "Fellowcraft", Abbv = "FC" },
                new Degree() { DegreeID = Guid.NewGuid(), Number = 3, Name = "Master Mason", Abbv = "MM" },
                new Degree() { DegreeID = Guid.NewGuid(), Number = 4, Name = "Past Master", Abbv = "PM" }
            );
        }
    }
}
