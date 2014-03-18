using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLMS.BLL.Entities;

namespace GLMS.DAL.Migrations.Seed
{
    public static class OfficeData
    {
        public static void Seed(GLMSContext context)
        {
            context.Offices.AddOrUpdate(x => x.Abbr,
                new Office() { OfficeID = Guid.NewGuid(), Abbr = "WM", Sequence = 1, Title = "Worshipful Master" },
                new Office() { OfficeID = Guid.NewGuid(), Abbr = "SW", Sequence = 2, Title = "Senior Warden" },
                new Office() { OfficeID = Guid.NewGuid(), Abbr = "JW", Sequence = 3, Title = "Junior Warden" },
                new Office() { OfficeID = Guid.NewGuid(), Abbr = "Sec", Sequence = 4, Title = "Secretary" },
                new Office() { OfficeID = Guid.NewGuid(), Abbr = "Trs", Sequence = 5, Title = "Treasurer" },
                new Office() { OfficeID = Guid.NewGuid(), Abbr = "SD", Sequence = 6, Title = "Senior Deacon" },
                new Office() { OfficeID = Guid.NewGuid(), Abbr = "JD", Sequence = 7, Title = "Junior Deacon" },
                new Office() { OfficeID = Guid.NewGuid(), Abbr = "SS", Sequence = 8, Title = "Senior Steward" },
                new Office() { OfficeID = Guid.NewGuid(), Abbr = "JS", Sequence = 9, Title = "Junior Steward" },
                new Office() { OfficeID = Guid.NewGuid(), Abbr = "Mar", Sequence = 10, Title = "Marshal" },
                new Office() { OfficeID = Guid.NewGuid(), Abbr = "Chap", Sequence = 11, Title = "Chaplain" },
                new Office() { OfficeID = Guid.NewGuid(), Abbr = "Tyl", Sequence = 12, Title = "Tyler" }
            );
        }
    }
}
