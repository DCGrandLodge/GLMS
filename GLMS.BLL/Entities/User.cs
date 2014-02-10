using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLMS.BLL.Entities
{
    public enum AccessLevel { None, Self, Lodge, System }

    public class User
    {
        public Guid UserID { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public bool Active { get; set; }
        public Password Password { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? LastLogin { get; set; }
        public Guid? MemberID { get; set; }
        public AccessLevel AccessLevel { get; set; }

        public virtual Member Member { get; set; }

        public User()
        {
            Password = new Password();
        }
    }
}
