using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Hosting;
using System.Web.Caching;
using System.Collections;
using System.IO;
using Bee.Util;
using Bee.Logging;
using System.Reflection;

namespace Bee.Web
{
    public class BeeVirtualFile : VirtualFile
    {
        private static List<Assembly> ResourceAssemblyList = new List<Assembly>();

        protected string url;

        static BeeVirtualFile()
        {
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                string s = a.FullName.Split(',')[0];
                if (s.StartsWith("Bee."))
                {
                    ResourceAssemblyList.Add(a);
                }
            }
        }

        // Methods
        public BeeVirtualFile(string virtualPath, string url)
            : base(virtualPath)
        {
            this.url = url;
        }

        protected internal static bool FileExists(string url)
        {
            Stream fileStream;
            bool exists = FileExists(url, out fileStream);
            if (fileStream != null)
            {
                fileStream.Dispose();
            }
            return exists;
        }

        protected internal static bool FileExists(string url, out Stream fileStream)
        {
            fileStream = null;
            string appRoot = "~/";
            if (!appRoot.EndsWith("/"))
            {
                appRoot = appRoot + "/";
            }
            if (!url.StartsWith(appRoot, true, null))
            {
                return false;
            }
            url = url.Remove(0, appRoot.Length);
            if (url.EndsWith(".aspx"))
            {
                url = "Bee/" + url;
            }
            else
            {
                url = "Bee/Resources/" + url;
            }
            url = url.Replace('/', '.');

            foreach (Assembly assembly in ResourceAssemblyList)
            {
                fileStream = ResourceUtil.GetStream(assembly, url, false);
                
                if (fileStream != null)
                {
                    break;
                }
            }
            return (fileStream != null);
        }

        public override Stream Open()
        {
            Stream fileStream;
            FileExists(this.url, out fileStream);
            return fileStream;
        }
    }

    public class FxVirtualPathProvider : VirtualPathProvider
    {
        protected static FxVirtualPathProvider RegisteredInstance;

        protected FxVirtualPathProvider()
        {
        }

        public override bool FileExists(string virtualPath)
        {
            return (this.IsFxVirtualPath(virtualPath) || base.FileExists(virtualPath));
        }

        public override CacheDependency GetCacheDependency(string virtualPath, IEnumerable virtualPathDependencies, DateTime utcStart)
        {
            return null;
        }

        public override VirtualFile GetFile(string virtualPath)
        {
            string url;
            if (this.IsFxVirtualPath(virtualPath, out url) && !File.Exists(HostingEnvironment.MapPath(virtualPath)))
            {
                return new BeeVirtualFile(virtualPath, url);
            }
            return base.GetFile(virtualPath);
        }

        protected override void Initialize()
        {
            base.Initialize();
            RegisteredInstance = this;
        }

        protected virtual bool IsFxVirtualPath(string virtualPath)
        {
            string url;
            return this.IsFxVirtualPath(virtualPath, out url);
        }

        protected virtual bool IsFxVirtualPath(string virtualPath, out string url)
        {
            url = StringUtil.MakeUrlRelative(virtualPath, HostingEnvironment.ApplicationVirtualPath);
            return BeeVirtualFile.FileExists(url);
        }

        public static void RegisterToHostingEnvironment(bool registerEvenExists)
        {
            if ((RegisteredInstance == null) || registerEvenExists)
            {
                HostingEnvironment.RegisterVirtualPathProvider(new FxVirtualPathProvider());
            }
        }

    }
}
