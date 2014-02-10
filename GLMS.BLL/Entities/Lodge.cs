using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLMS.BLL.Entities
{
    public class Lodge
    {
        public Guid LodgeID { get; set; }

        public int Number { get; set; }
        public string Name { get; set; }
        public Address Address { get; set; } // Address complex type
        public string PhoneNumber { get; set; }
        public string MeetingDates { get; set; }
        public bool DuesPaying { get; set; } // TODO - Question for Kevin - what are the various Dues fields for?

        public string Status { get { return Dark ? "Dark" : Merged ? "Merged" : Chartered ? "Active" : UnderDispensation ? "UD" : null; } private set { } }
        public DateTime? DispensationDate { get; set; }
        public bool UnderDispensation { get { return DispensationDate.HasValue && !Dark && !Merged && !CharterDate.HasValue; } private set { } }
        public DateTime? CharterDate { get; set; }
        public bool Chartered { get { return CharterDate.HasValue && !Dark && !Merged; } private set { } }
        public DateTime? DarkDate { get; set; }
        public bool Dark { get { return !DarkDate.HasValue; } private set { } }
        public Guid? MergedWithLodgeID { get; set; }
        public bool Merged { get { return !MergedWithLodgeID.HasValue; } private set { } }

        public virtual ICollection<LodgeMembership> LodgeMembership { get; set; }
        public virtual ICollection<LodgeOfficer> Officers { get; set; }
        public virtual ICollection<Lodge> MergedLodges { get; set; }
        public virtual Lodge MergedWithLodge { get; set; }

    }
}
