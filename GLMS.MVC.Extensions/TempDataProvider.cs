using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace GLMS.MVC.Extensions
{
    public class TempDataProvider : ITempDataProvider
    {
        public const string VOLATILE = "__VOLATILE";

        private ITempDataProvider tempDataProvider;

        public TempDataProvider(System.Web.Mvc.ITempDataProvider tempDataProvider)
        {
            this.tempDataProvider = tempDataProvider;
        }

        IDictionary<string, object> ITempDataProvider.LoadTempData(ControllerContext controllerContext)
        {
            return tempDataProvider.LoadTempData(controllerContext);
        }

        void ITempDataProvider.SaveTempData(ControllerContext controllerContext, IDictionary<string, object> values)
        {
            if (values.ContainsKey(TempDataProvider.VOLATILE))
            {
                HashSet<string> vtemp = (HashSet<string>)values[TempDataProvider.VOLATILE];
                foreach (string key in vtemp.ToList())
                {
                    if (!values.ContainsKey(key))
                    {
                        vtemp.Remove(key);
                    }
                }
            }
            tempDataProvider.SaveTempData(controllerContext, values);
        }
    }
}