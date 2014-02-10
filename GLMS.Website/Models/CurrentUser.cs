using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using GLMS.BLL.Entities;

namespace GLMS.Website
{
    [Serializable]
    public class CurrentUser
    {
        public const string SessionKey = "__CurrentUser__";
        public static bool IsAuthenticated { get { return HttpContext.Current.Request.IsAuthenticated && User != null; } }
        public static CurrentUser User { get { return HttpContext.Current.Session[SessionKey] as CurrentUser; } }

        public CurrentUser(User user)
        {
            _userID = user.UserID;
            _name = user.FirstName;
            _username = user.Username;
            _accessLevel = user.AccessLevel;
        }

        private Guid _userID;
        private string _name;
        private string _username;
        private AccessLevel _accessLevel;

        public Guid userID { get { return _userID; } }
        public string name { get { return _name; } }
        public string email { get { return _username; } }

        public static Guid UserID { get { return User._userID; } }
        public static string Email { get { return User._username; } }
        public static string Name { get { return User._name; } }
        public static AccessLevel AccessLevel { get { return User._accessLevel; } }

        public static bool HasPermission(AccessLevel accessLevel) { if (IsAuthenticated) return User.hasPermission(accessLevel); else return false; }

        public bool hasPermission(AccessLevel accessLevel)
        {
            return _accessLevel >= accessLevel;
        }

        public static void SignIn(User user)
        {
            if (user != null)
            {
                FormsAuthentication.SetAuthCookie(String.Format("{0} {1}", user.FirstName, user.LastName), false);
                HttpContext.Current.Session[CurrentUser.SessionKey] = new CurrentUser(user);
            }
        }

        public static void SignOut()
        {
            /* Don't care if we fail to log off, as long as we do all steps. */
            try { HttpContext.Current.Session.Remove(CurrentUser.SessionKey); }
            catch { }
            try { FormsAuthentication.SignOut(); }
            catch { }
        }

    }
}