using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace GLMS.MVC.Extensions
{
    public class PopupEditorResult
    {
        public bool result { get; set; }
        public string message { get; set; }

        public PopupEditorResult(bool result, string message = null)
        {
            this.result = result;
            this.message = message;
        }

        public PopupEditorResult(ModelStateDictionary ModelState)
        {
            if (ModelState.IsValid)
            {
                result = true;
            }
            else
            {
                result = false;
                message = ModelState.SelectMany(x => x.Value.Errors).Select(x => x.ErrorMessage).FirstOrDefault();
            }
        }
    }

    public class JsonDataResult : JsonResult
    {
        private static List<JavaScriptConverter> Converters = new List<JavaScriptConverter>();
        public static void RegisterConverter(JavaScriptConverter converter)
        {
            Converters.Add(converter);
        }

        public JsonDataResult()
        {
        }
        public JsonDataResult(object data)
        {
            this.Data = data;
        }
        public JsonDataResult(JsonRequestBehavior jsonRequestBehavior)
        {
            this.JsonRequestBehavior = jsonRequestBehavior;
        }
        public JsonDataResult(object data, JsonRequestBehavior jsonRequestBehavior)
        {
            this.Data = data;
            this.JsonRequestBehavior = jsonRequestBehavior;
        }
        public override void ExecuteResult(ControllerContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
            if (JsonRequestBehavior == JsonRequestBehavior.DenyGet &&
                String.Equals(context.HttpContext.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("This request has been blocked because sensitive information could be disclosed to third party web sites when this is used in a GET request. To allow GET requests, set JsonRequestBehavior to AllowGet.");
            }

            HttpResponseBase response = context.HttpContext.Response;

            if (!String.IsNullOrEmpty(ContentType))
            {
                response.ContentType = ContentType;
            }
            else
            {
                response.ContentType = "application/json";
            }
            if (ContentEncoding != null)
            {
                response.ContentEncoding = ContentEncoding;
            }
            if (Data != null)
            {
                if (Data.GetType().GetCustomAttributes(typeof(DataContractAttribute), true).Any())
                {
                    // Use the DataContractJsonSerializer instead of the JavaScriptSerializer 
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(Data.GetType());
                    serializer.WriteObject(response.OutputStream, Data);
                }
                else
                {
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    serializer.RegisterConverters(Converters);
                    response.Write(serializer.Serialize(Data));
                }
            }
        }

    }
}
