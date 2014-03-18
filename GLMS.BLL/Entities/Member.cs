using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLMS.BLL.Entities
{
    public enum MemberStatus { Applicant, Active, Deceased, Demitted, Withdrew, NPD, Expelled, NonMember, Suspended }
    public enum PetitionType { Degrees, Affiliation }
    public class Member
    {
        public Guid MemberID { get; set; }

        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public DateTime DOB { get; set; }
        public DateTime? DOD { get; set; }
        public bool Clergy { get; set; }
        public MemberStatus Status { get; set; }

        public virtual ICollection<MemberDegree> DegreeDates { get; set; }
        public virtual ICollection<LodgeMembership> LodgeMembership { get; set; }
        public virtual User User { get; set; }

        public string FullName
        {
            get
            {
                /*
                return String.Format("{0}, {1}{2}{3}", 
                    LastName, 
                    String.Join(" ", FirstName, MiddleName, Suffix), 
                    String.IsNullOrWhiteSpace(Prefix) ? "" : ",", 
                    Prefix);
                 */
                return String.Format("{0}, {1}", LastName, String.Join(" ", FirstName, MiddleName));
            }
            private set { }
        }
    }
}
