using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLMS.Mail
{
    public interface IMailService
    {
        void SendMail(List<string> to, string subject, string body);
        void SendMail(string from, List<string> to, string subject, string body);
    }
}
