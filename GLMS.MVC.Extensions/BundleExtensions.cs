using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Hosting;
using System.IO;
using System.Text.RegularExpressions;

namespace GLMS.MVC.Extensions
{
    public class PlainJsBundler : IBundleTransform
    {
        public virtual void Process(BundleContext context, BundleResponse bundle)
        {
            bundle.ContentType = "text/javascript";
        }
    }

    public class PlainCssBundler : IBundleTransform
    {
        public virtual void Process(BundleContext context, BundleResponse bundle)
        {
            bundle.ContentType = "text/css";
        }
    }

    public class CssWithImagesBundler : PlainCssBundler
    {
        public override void Process(BundleContext context, BundleResponse bundle)
        {
            base.Process(context, bundle);
            bundle.Content = CssImageEmbedder.EmbedImages(bundle.Content);
        }
    }

    public class CssWithImagesMinify : CssMinify
    {
        public override void Process(BundleContext context, BundleResponse bundle)
        {
            base.Process(context, bundle);
            bundle.Content = CssImageEmbedder.EmbedImages(bundle.Content);
        }
    }

    public static class CssImageEmbedder
    {
        public static string ImageBase = "";
        public static int MaxEmbedSize = 8192;
        private static readonly Regex url = new Regex(@"url\((([^\)]*))\)", RegexOptions.Singleline);
        private const string format = "url(data:image/{0};base64,{1})";

        public static string EmbedImages(string content)
        {
            foreach (Match match in url.Matches(content))
            {
                string filename = match.Groups[2].Value.Replace("'","").Replace("\"","");
                if (!(filename.StartsWith("data:") || filename.StartsWith("/") || filename.StartsWith("http:") || filename.StartsWith("https:")))
                {
                    var file = new FileInfo(HostingEnvironment.MapPath(System.IO.Path.Combine(ImageBase ?? "", filename)));
                    if (file.Exists && file.Length < MaxEmbedSize)
                    {
                        byte[] buffer = File.ReadAllBytes(file.FullName);
                        string ext = file.Extension.Substring(1);
                        string dataUri = string.Format(format, ext, Convert.ToBase64String(buffer));
                        content = content.Replace(match.Value, dataUri);
                    }
                }
            }
            return content;
        }
    }

    public abstract class Bundle : System.Web.Optimization.Bundle, IEnumerable<string>
    {
        public string Alias { get; set; }
        private string prefix;
        public Bundle(string alias, string virtualPath, params IBundleTransform[] transforms)
            : base(MakePath(virtualPath,alias), transforms)
        {
            this.Alias = alias;
        }

        public Bundle(string alias, string prefix, string virtualPath, params IBundleTransform[] transforms)
            : base(MakePath(virtualPath.StartsWith("~") ? virtualPath : MakePath(prefix, virtualPath), alias), transforms)
        {
            this.prefix = prefix;
            this.Alias = alias;
        }

        private static string MakePath(string prefix, string suffix)
        {
            return String.Format("{0}{1}{2}", prefix, prefix.EndsWith("/") ? "" : "/", suffix);
        }

        public new System.Web.Optimization.Bundle Include(params string[] virtualPaths)
        {
            if (virtualPaths != null)
            {
                for (int i = 0; i < virtualPaths.Length; i++)
                {
                    string virtualPath = virtualPaths[i];
                    if (!(prefix == null || virtualPath.StartsWith("~")))
                    {
                        virtualPaths[i] = String.Format("{0}/{1}", prefix, virtualPath);

                    }
                }
            }
            return base.Include(virtualPaths);
        }

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    public class CssBundle : Bundle
    {
        public static string DefaultPath { get; set; }
        public static bool EmbedImages { get; set; }
        public static bool Minify { get; set; }
        public CssBundle(string alias, string prefix, string virtualPath)
            : base(alias, prefix, virtualPath ?? DefaultPath,
                EmbedImages
                    ? Minify ? (IBundleTransform)new CssWithImagesMinify() : new CssWithImagesBundler()
                    : Minify ? (IBundleTransform)new CssMinify() : new PlainCssBundler())
        { }
        static CssBundle()
        {
            DefaultPath = "~/bundles/css";
        }
    }

    public class JsBundle : Bundle
    {
        public static string DefaultPath { get; set; }
        public static bool Minify { get; set; }
        public JsBundle(string alias, string prefix, string virtualPath)
            : base(alias, prefix, virtualPath ?? DefaultPath, Minify ? (IBundleTransform)new JsMinify() : new PlainJsBundler()) { }
        static JsBundle()
        {
            DefaultPath = "~/bundles/js";
        }
    }
}