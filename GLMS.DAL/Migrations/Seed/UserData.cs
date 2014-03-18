using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLMS.BLL.Entities;

namespace GLMS.DAL.Migrations.Seed
{
    public static class UserData
    {
        public static void Seed(GLMSContext context)
        {
            var user = context.Users.Where(x => x.UserID == Guid.Empty).FirstOrDefault();
            if (user == null)
            {
                user = new User()
                {
                    UserID = Guid.Empty,
                    Username = "sysdba",
                    FirstName = "System",
                    LastName = "Administrator",
                    AccessLevel = AccessLevel.System,
                    Active = true,
                    DateCreated = DateTime.Now,
                    Email = "Mark.Shapiro@seguetech.com",
                    Password = new Password()
                    {
                        PlainTextPassword = "Password1",
                        ForceChange = true,
                    }
                };
                context.Add(user);
                context.SaveChanges();
            }
        }
    }
}
