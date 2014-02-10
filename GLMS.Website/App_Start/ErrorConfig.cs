using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using GLMS.BLL.Entities;

namespace GLMS
{
    public class ErrorConfig
    {
        public static void RegisterErrorHandler()
        {
            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                var log = Elmah.ErrorLog.GetDefault();
                foreach (var ex in args.Exception.Flatten().InnerExceptions)
                {
                    log.Log(new Elmah.Error(ex));
                }
                args.SetObserved();
            };
        }
    }
}