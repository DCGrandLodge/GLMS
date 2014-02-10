using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace GLMS.Mail
{
    public class MailDeliveryService : IMailService
    {
        public void SendMail(List<string> to, string subject, string body)
        {
            // TODO - SMTPFrom
            //SendMail(Configuration.Instance.SMTPFrom, to, subject, body);
        }

        public void SendMail(string from, List<string> to, string subject, string body)
        {
            Task.Factory.StartNew(() =>
            {
                // TODO - SMTPServer, SMTPPort
                /*
                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient(Configuration.Instance.SMTPServer, Configuration.Instance.SMTPPort);
                mail.From = new MailAddress(from);
                foreach (string item in to)
                {
                    mail.To.Add(item);
                }
                mail.Subject = subject;
                mail.Body = body;
                mail.IsBodyHtml = true;

                //SmtpServer.UseDefaultCredentials = true;
                SmtpServer.Send(mail);
                 */
            });
        }
    }
}
