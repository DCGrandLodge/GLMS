using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.Text;
using HtmlAgilityPack;
using System.Web.Optimization;
using System.Threading;

namespace GLMS.MVC.Extensions
{
    public class ScriptManager
    {
        public static bool UseBundles { get; set; }
        public static ReaderWriterLockSlim bundleLock = new ReaderWriterLockSlim();
        private static readonly string ScriptManagerKey = "__javascript__ScriptManager";
        private HtmlHelper htmlHelper;
        private List<Content> contentSet = new List<Content>();
        private static Dictionary<string, string> bundleMap = new Dictionary<string, string>();

        internal bool HasContent { get { lock (this) { return contentSet.Any(); } } }

        private ScriptManager() { }

        internal static ScriptManager GetScriptManager(HtmlHelper helper)
        {
            // Get ScriptManager from HttpContext.Items
            // This allows for a single ScriptManager to be created and used per HTTP request.
            var scriptmanager = helper.ViewContext.HttpContext.Items[ScriptManagerKey] as ScriptManager;
            if (scriptmanager == null)
            {
                // If ScriptManager hasn't been initialized yet, then initialize it.
                scriptmanager = new ScriptManager();
                scriptmanager.htmlHelper = helper;
                // Store it in HttpContext.Items for subsequent requests during this HTTP request.
                helper.ViewContext.HttpContext.Items[ScriptManagerKey] = scriptmanager;
                helper.ViewContext.HttpContext.Response.Filter = new ScriptManagerFilter(scriptmanager, helper);
            }
            // Return the ScriptManager
            return scriptmanager;
        }

        internal void RegisterClientScriptBundle(Bundle bundle, string id, int insertAt) { RegisterBundle(ContentType.ScriptFile, bundle, "text/javascript", id, insertAt); }
        internal void RegisterCSSBundle(Bundle bundle, string id, int insertAt) { RegisterBundle(ContentType.CSSFile, bundle, "text/css", id, insertAt); }
        internal void RegisterClientScriptBundle(string filename, string id, int insertAt) { RegisterBundle(ContentType.ScriptFile, BundleTable.Bundles.GetBundleFor(filename), "text/javascript", id, insertAt); }
        internal void RegisterClientScriptInclude(string filename, string id, int insertAt) { RegisterContent(ContentType.ScriptFile, filename, "text/javascript", id, insertAt); }
        internal void RegisterClientScript(string javascript, string id) { RegisterContent(ContentType.Script, javascript, "text/javascript", id, -1); }
        internal void RegisterCSSBundle(string filename, string id, int insertAt) { RegisterBundle(ContentType.CSSFile, BundleTable.Bundles.GetBundleFor(filename), "text/css", id, insertAt); }
        internal void RegisterCSSInclude(string filename, string id, int insertAt) { RegisterContent(ContentType.CSSFile, filename, "text/css", id, insertAt); }
        internal void RegisterCSS(string javaCSS, string id) { RegisterContent(ContentType.CSS, javaCSS, "text/css", id, -1); }

        private void RegisterBundle(ContentType contentType, System.Web.Optimization.Bundle bundle, string type, string id, int insertAt)
        {
            if (bundle == null)
            {
                return;
            }
            if (UseBundles)
            {
                addContent(contentType, bundle.Path, type, id, insertAt, bundle.Path);
            }
            else
            {
                var bundleContext = new BundleContext(htmlHelper.ViewContext.HttpContext, BundleTable.Bundles, bundle.Path);
                foreach (var file in bundle.EnumerateFiles(bundleContext))
                {
                    RegisterContent(contentType, file.IncludedVirtualPath, type, id, insertAt);
                }
            }
        }

        private void RegisterContent(ContentType contentType, string content, string type, string id, int insertAt)
        {
            UrlHelper Url = new UrlHelper(htmlHelper.ViewContext.RequestContext);
            string bundlePath = null;
            if (contentType == ContentType.CSSFile || contentType == ContentType.ScriptFile)
            {
                if (content.StartsWith("~"))
                {
                    content = Url.Content(content);
                }
                if (UseBundles)
                {
                    bundleLock.EnterUpgradeableReadLock();
                    try
                    {
                        if (bundleMap.ContainsKey(content))
                        {
                            bundlePath = bundleMap[content];
                            if (bundlePath == String.Empty)
                            {
                                bundlePath = null;
                            }
                        }
                        else
                        {
                            try
                            {
                                foreach (var bundle in BundleTable.Bundles)
                                {
                                    var bundleContext = new BundleContext(htmlHelper.ViewContext.HttpContext, BundleTable.Bundles, bundle.Path);
                                    if (bundle.EnumerateFiles(bundleContext).Any(x => x.VirtualFile.VirtualPath == content))
                                    {
                                        bundlePath = bundle.Path;
                                        break;
                                    }
                                }
                            }
                            catch (ArgumentException)
                            {
                                // Ignore this - just means we couldn't map the path, in which case we assume non-bundled.
                            }
                            bundleLock.EnterWriteLock();
                            try
                            {
                                bundleMap.Add(content, bundlePath ?? String.Empty);
                            }
                            finally
                            {
                                bundleLock.ExitWriteLock();
                            }
                        }
                    }
                    finally
                    {
                        bundleLock.ExitUpgradeableReadLock();
                    }
                }
            }
            addContent(contentType, content, type, id, insertAt, bundlePath);
        }

        private void addContent(ContentType contentType, string content, string type, string id, int insertAt, string bundlePath)
        {
            lock (this)
            {
                Content toAdd = new Content { ContentType = contentType, Value = content, Type = type, ID = id, Written = false, Bundle = bundlePath };
                if (!contentSet.Contains(toAdd))
                {
                    int offset;
                    if (insertAt == -1)
                    {
                        offset = contentSet.FindLastIndex(x => x.ContentType == contentType);
                        if (offset == -1)
                        {
                            contentSet.Add(toAdd);
                        }
                        else
                        {
                            offset = Math.Min(offset + 1, contentSet.Count());
                            contentSet.Insert(offset, toAdd);
                        }
                    }
                    else
                    {
                        offset = contentSet.FindIndex(x => x.ContentType == contentType);
                        if (offset == -1)
                        {
                            offset = 0;
                        }
                        insertAt = Math.Min(offset + insertAt, contentSet.Count());
                        contentSet.Insert(insertAt, toAdd);
                    }
                }
            }
        }

        private IEnumerable<Content> GetScriptsForWriting() { return GetContentForWriting(ContentType.Script); }
        private IEnumerable<Content> GetScriptIncludesForWriting() { return GetContentForWriting(ContentType.ScriptFile); }
        private IEnumerable<Content> GetCSSForWriting() { return GetContentForWriting(ContentType.CSS); }
        private IEnumerable<Content> GetCSSIncludesForWriting() { return GetContentForWriting(ContentType.CSSFile); }

        private IEnumerable<Content> GetContentForWriting(ContentType contentType)
        {
            lock (this)
            {
                IEnumerable<Content> output = contentSet.Where(x => x.ContentType.Equals(contentType) && !x.Written).ToList();
                foreach (Content content in output)
                {
                    content.Written = true;
                }
                return output;
            }
        }
        private enum ContentType { Script, ScriptFile, CSS, CSSFile };
        private class Content
        {
            public ContentType ContentType { get; set; }
            public string Value { get; set; }
            public bool Written { get; set; }
            public string Type { get; set; }
            public string ID { get; set; }
            public string Bundle { get; set; }

            public override int GetHashCode()
            {
                return Value.GetHashCode();
            }
            public override bool Equals(object obj)
            {
                if (obj is Content)
                {
                    return GetHashCode().Equals(obj.GetHashCode());
                }
                return base.Equals(obj);
            }
        }

        private class ScriptManagerFilter : MemoryStream
        {
            private Stream Chain;
            private ScriptManager Manager;
            private HtmlHelper Html;
            private UrlHelper Url;

            public ScriptManagerFilter(ScriptManager Manager, HtmlHelper helper)
            {
                this.Chain = helper.ViewContext.HttpContext.Response.Filter;
                this.Manager = Manager;
                this.Html = helper;
                Url = new UrlHelper(helper.ViewContext.RequestContext);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (Manager.HasContent)
                {
                    HtmlDocument doc = new HtmlDocument();
                    doc.Load(new MemoryStream(buffer, offset, count));
                    HtmlNode head = doc.DocumentNode.SelectSingleNode("/html/head");
                    if (head != null)
                    {
                        // Can't rely on HtmlDocument for writing because it doesn't handle truncation.
                        Chain.Write(buffer, 0, head.StreamPosition);
                        int continuePos = head.NextSibling.StreamPosition;

                        IEnumerable<Content> write = Manager.GetCSSIncludesForWriting();
                        List<string> bundles = new List<string>();
                        foreach (Content include in write)
                        {
                            string path = include.Value;
                            if (UseBundles && include.Bundle != null)
                            {
                                if (bundles.Contains(include.Bundle))
                                {
                                    continue;
                                }
                                else
                                {
                                    path = BundleTable.Bundles.ResolveBundleUrl(include.Bundle, true);
                                }
                                bundles.Add(include.Bundle);
                            }
                            HtmlNode filenode = doc.CreateElement("link");
                            head.AppendChild(filenode);
                            filenode.SetAttributeValue("rel", "stylesheet");
                            filenode.SetAttributeValue("type", include.Type);
                            filenode.SetAttributeValue("href", path);
                            if (include.ID != null)
                            {
                                filenode.SetAttributeValue("id", include.ID);
                            }
                            head.AppendChild(doc.CreateTextNode("\n"));
                        }
                        write = Manager.GetScriptIncludesForWriting();
                        foreach (Content include in write)
                        {
                            string path = include.Value;
                            if (UseBundles && include.Bundle != null)
                            {
                                if (bundles.Contains(include.Bundle))
                                {
                                    continue;
                                }
                                else
                                {
                                    path = BundleTable.Bundles.ResolveBundleUrl(include.Bundle, true);
                                }
                                bundles.Add(include.Bundle);
                            }
                            HtmlNode filenode = doc.CreateElement("script");
                            head.AppendChild(filenode);
                            filenode.SetAttributeValue("type", include.Type);
                            filenode.SetAttributeValue("src", path);
                            if (include.ID != null)
                            {
                                filenode.SetAttributeValue("id", include.ID);
                            }
                            head.AppendChild(doc.CreateTextNode("\n"));
                        } 
                        write = Manager.GetScriptsForWriting();
                        if (write.Any())
                        {
                            HtmlNode scriptnode = doc.CreateElement("script");
                            head.AppendChild(scriptnode);
                            scriptnode.SetAttributeValue("type", "text/javascript");
                            StringBuilder scripts = new StringBuilder();
                            foreach (Content script in write)
                            {
                                scripts.AppendLine(script.Value);
                            }
                            scriptnode.InnerHtml = scripts.ToString();
                            head.AppendChild(doc.CreateTextNode("\n"));
                        }
                        using (StreamWriter writer = new StreamWriter(Chain))
                        {
                            head.WriteTo(writer);
                        }
                        Chain.Write(buffer, continuePos, buffer.Length - continuePos);
                    }
                    else
                    {
                        Chain.Write(buffer, offset, count);
                    }
                }
                else
                {
                    Chain.Write(buffer, offset, count);
                }
            }
        }

        public string RenderCSS(bool forEmail)
        {
            StringBuilder output = new StringBuilder();
            IEnumerable<Content> write = GetCSSIncludesForWriting();
            List<string> bundles = new List<string>();
            foreach (Content include in write)
            {
                string path = include.Value;
                if (UseBundles && include.Bundle != null)
                {
                    if (bundles.Contains(include.Bundle))
                    {
                        continue;
                    }
                    else
                    {
                        path = BundleTable.Bundles.ResolveBundleUrl(include.Bundle, true);
                    }
                    bundles.Add(include.Bundle);
                }
                if (forEmail)
                {
                    output.AppendLine("<style type='text/css'>");
                    if (include.Bundle != null)
                    {
                        var bundle = BundleTable.Bundles.GetBundleFor(include.Bundle);
                        var bundleContext = new BundleContext(htmlHelper.ViewContext.HttpContext, BundleTable.Bundles, bundle.Path);
                        output.Append(bundle.GenerateBundleResponse(bundleContext).Content);
                    }
                    else
                    {
                        output.Append(File.ReadAllText(htmlHelper.ViewContext.HttpContext.Server.MapPath(path)));
                    }
                    output.AppendLine("</style>");
                }
                else
                {
                    output.AppendLine(String.Format("<link rel='stylesheet' type='{0}' href='{1}'{2} />",
                        include.Type, path, include.ID == null ? String.Empty : String.Format(" id='{0}'", include.ID)));
                }
            }
            return output.ToString();
        }

        internal string RenderScripts(bool forEmail)
        {
            IEnumerable<Content> write = GetScriptIncludesForWriting();
            IEnumerable<Content> writeInline = GetScriptsForWriting();
            if (forEmail)
            {
                return null;
            }
            List<string> bundles = new List<string>();
            StringBuilder output = new StringBuilder();
            foreach (Content include in write)
            {
                string path = include.Value;
                if (UseBundles && include.Bundle != null)
                {
                    if (bundles.Contains(include.Bundle))
                    {
                        continue;
                    }
                    else
                    {
                        path = BundleTable.Bundles.ResolveBundleUrl(include.Bundle, true);
                    }
                    bundles.Add(include.Bundle);
                }
                output.AppendLine(String.Format("<script type='{0}' src='{1}'{2}></script>",
                    include.Type, path, include.ID == null ? String.Empty : String.Format(" id='{0}'", include.ID)));
            }
            if (writeInline.Any())
            {
                output.AppendLine("<script type='text/javascript'>");
                foreach (Content script in writeInline)
                {
                    output.AppendLine(script.Value);
                }
                output.AppendLine("</script>");
            }
            return output.ToString();
        }

        internal string RenderAll(bool forEmail)
        {
            return String.Format("{0}\n{1}", RenderCSS(forEmail), RenderScripts(forEmail));
        }
    }

    public static class ScriptManagerExtensions
    {
        public static object RegisterCSSBundle(this HtmlHelper htmlHelper, string bundle, string id = null, int insertAt = -1)
        {
            ScriptManager.GetScriptManager(htmlHelper).RegisterCSSBundle(bundle, id, insertAt);
            return null;
        }
        public static object RegisterCSSInclude(this HtmlHelper htmlHelper, string filename, string id = null, int insertAt = -1)
        {
            ScriptManager.GetScriptManager(htmlHelper).RegisterCSSInclude(filename, id, insertAt);
            return null;
        }
        public static object RegisterClientScriptBundle(this HtmlHelper htmlHelper, string bundle, string id = null, int insertAt = -1)
        {
            ScriptManager.GetScriptManager(htmlHelper).RegisterClientScriptBundle(bundle, id, insertAt);
            return null;
        }
        public static object RegisterClientScriptInclude(this HtmlHelper htmlHelper, string filename, string id = null, int insertAt = -1)
        {
            ScriptManager.GetScriptManager(htmlHelper).RegisterClientScriptInclude(filename, id, insertAt);
            return null;
        }
        public static object RegisterClientScript(this HtmlHelper htmlHelper, string javascript, string id = null)
        {
            ScriptManager.GetScriptManager(htmlHelper).RegisterClientScript(javascript, id);
            return null;
        }
        public static object RegisterBundle(this HtmlHelper htmlHelper, string alias, string id = null, int insertAt = -1)
        {
            foreach (var bundle in BundleTable.Bundles.OfType<Bundle>().Where(x => x.Alias == alias))
            {
                if (bundle is CssBundle)
                {
                    ScriptManager.GetScriptManager(htmlHelper).RegisterCSSBundle(bundle, id, insertAt);
                }
                else if (bundle is JsBundle)
                {
                    ScriptManager.GetScriptManager(htmlHelper).RegisterClientScriptBundle(bundle, id, insertAt);
                }
            }
            return null;
        }

        public static MvcHtmlString RenderScriptManagerCSS(this HtmlHelper htmlHelper, bool forEmail = false)
        {
            return new MvcHtmlString(ScriptManager.GetScriptManager(htmlHelper).RenderCSS(forEmail));
        }
        public static MvcHtmlString RenderScriptManagerScripts(this HtmlHelper htmlHelper, bool forEmail = false)
        {
            return new MvcHtmlString(ScriptManager.GetScriptManager(htmlHelper).RenderScripts(forEmail));
        }
        public static MvcHtmlString RenderScriptManager(this HtmlHelper htmlHelper, bool forEmail = false)
        {
            return new MvcHtmlString(ScriptManager.GetScriptManager(htmlHelper).RenderAll(forEmail));
        }
    }
}