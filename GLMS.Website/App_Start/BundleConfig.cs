using System.Web;
using System.Web.Optimization;
using GLMS.MVC.Extensions;

namespace GLMS.Website
{
    public class BundleConfig
    {
        // For more information on Bundling, visit http://go.microsoft.com/fwlink/?LinkId=254725
        public static void RegisterBundles(BundleCollection bundles)
        {
            // Set up the bundling classes
#if DEBUG
            JsBundle.Minify = false;
            CssBundle.Minify = false;
            CssBundle.EmbedImages = true;
#else
            JsBundle.Minify = true;
            CssBundle.Minify = true;
            CssBundle.EmbedImages = true;
#endif
            CssImageEmbedder.ImageBase = "~/Content";
            ScriptManager.UseBundles = true;

            bundles.Add(new JsBundle("site", "~/Scripts", "~/bundles/js").Include(
                    "jquery-{version}.js", 
                    "jquery.form.js",
                    "jquery-ui-{version}.js",
                    "date.f-{version}.js",
                    "modernizr-{version}.js",
                    "GLMS.js"));

            bundles.Add(new JsBundle("jqueryval", "~/Scripts", "~/bundles/js").Include(
                    "jquery.validate.js",
                    "jquery.validate.unobtrusive.js"));

            bundles.Add(new JsBundle("jqgrid", "~/Scripts", "~/bundles/jqGrid").Include(
                "i18n/grid.locale-en.js",
                "jquery.jqGrid.js",
                "AscendantDB.PopupEditor.js"
            ));

            bundles.Add(new CssBundle("site", "~/Content", "~/Content").Include(
                    "themes/glms/jquery-ui-1.10.3.glms.css",
                    "site.css"));

            bundles.Add(new CssBundle("jqgrid", "~/Content", "jqGrid").Include(
                "jquery.jqgrid/ui.jqgrid.css"
            ));

        }
    }
}