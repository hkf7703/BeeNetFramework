using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.IO;
using System.Web.Compilation;
using Bee.Core;
using System.Web.UI;
using Bee.Util;

namespace Bee.Web
{
    public abstract class ActionResult
    {
        protected string controllerName;
        protected string actionName;

        protected BeeDataAdapter dataAdapter;
        protected ControllerBase instance;

        internal virtual void Init(ControllerBase instance, BeeDataAdapter dataAdapter)
        {
            this.instance = instance;
            this.dataAdapter = dataAdapter;
        }

        public abstract void Ouput(HttpContext context);

        public string ControllerName
        {
            get { return controllerName; }
            set { controllerName = value; }
        }

        public string ActionName
        {
            get { return actionName; }
            set { actionName = value; }
        }
    }

    public sealed class JsonResult : ActionResult
    {
        private BeeMvcResult mvcResult = new BeeMvcResult();

        public JsonResult(object model)
        {
            mvcResult.data = model;
            mvcResult.code = 200;
        }

        public override void Ouput(HttpContext context)
        {
            context.Response.Write(Newtonsoft.Json.JsonConvert.SerializeObject(mvcResult));
        }
    }

    public sealed class ContentResult : ActionResult
    {
        private object content = null;
        public ContentResult(object obj)
        {
            content = obj;
        }

        public override void Ouput(HttpContext context)
        {
            if (content != null)
            {
                context.Response.Write(content);
            }
        }
    }

    public sealed class StreamResult : ActionResult
    {
        private string fileName;
        private Stream stream;

        public StreamResult(string fileName, Stream stream)
        {
            this.fileName = fileName;
            this.stream = stream;
        }

        public override void Ouput(HttpContext context)
        {
            stream.Position = 0;
            context.Response.ClearContent();
            context.Response.ContentType = MimeTypes.GetMimeType(fileName);
            context.Response.AddHeader("Content-Disposition", string.Format("attachment;filename={0}",
                System.Web.HttpUtility.UrlEncode(this.fileName, Encoding.UTF8)));

            context.Response.BinaryWrite(GetStreamBuffer(stream));

            stream.Close();
            stream.Dispose();
            stream = null;
        }

        private byte[] GetStreamBuffer(Stream stream)
        {
            byte[] dst = new byte[stream.Length];
            stream.Read(dst, 0, (int)stream.Length);
            return dst;
        }


    }

    /// <summary>
    /// 表示一个重定向的结果
    /// </summary>
    public sealed class RedirectResult : ActionResult
    {
        public RedirectResult(string url)
        {
            this.Url = url;
        }

        public RedirectResult(string controllerName, string actionName)
        {
            this.controllerName = controllerName;
            this.actionName = actionName;
            if (string.Compare(ControllerManager.DefaultControllerName, ControllerName, true) == 0)
            {
                this.Url = string.Format("~/{0}.bee", ActionName);
            }
            else
            {
                this.Url = string.Format("~/{0}/{1}.bee", ControllerName, ActionName);
            }
        }

        public override void Ouput(HttpContext context)
        {
            context.Response.Redirect(Url, false);
        }

        public string Url
        {
            get;
            set;
        }

    }

    /// <summary>
    /// 表示一个页面结果（页面将由框架执行）
    /// </summary>
    public sealed class PageResult : ActionResult
    {

        private object model;

        public PageResult(string controllerName, string viewName)
            : this(controllerName, viewName, null)
        {
        }

        public PageResult(string controllerName, string viewName, object model)
        {
            this.controllerName = controllerName;
            this.actionName = viewName;
            this.model = model;
        }

        public override void Ouput(HttpContext context)
        {
            BeePageView pageBase = null;

            dataAdapter.Add(Constants.BeeAutoModelInfo, instance.AutoModelInfo());

            string areaName = dataAdapter.TryGetValue<string>(Constants.BeeAreaName, string.Empty);

            string virtualPath = string.Empty;
            if (actionName.StartsWith("BeeAuto"))
            {
                virtualPath = string.Format("/InnerViews/{0}.aspx", actionName);
            }
            else
            {
                if (string.IsNullOrEmpty(areaName))
                {
                    virtualPath = string.Format("/Views/{0}/{1}.aspx", controllerName, actionName);
                }
                else
                {
                    virtualPath = string.Format("/Views/{2}/{0}/{1}.aspx", controllerName, actionName, areaName);
                }
            }

            if (model != null)
            {
                dataAdapter[Constants.BeeModelName] = model;
            }

            if (context.Request.ApplicationPath != "/")
            {
                virtualPath = context.Request.ApplicationPath + virtualPath;
            }

            object o = BuildManager.CreateInstanceFromVirtualPath(virtualPath, typeof(object));
            if (o == null)
            {
                throw new MvcException(string.Format("Cannot find the page:{0}", virtualPath));
            }
            else
            {
                pageBase = o as BeePageView;
                Page page = o as Page;

                if (pageBase != null)
                {
                    pageBase.InitData(dataAdapter);

                    // 效率低于下面的方法
                    //context.Server.Execute(page, context.Response.Output, false);

                    pageBase.ProcessRequest(context);
                }
                else if (page != null)
                {
                    //context.Server.Execute(page, context.Response.Output, false);

                    page.ProcessRequest(context);
                }
                else
                {
                    throw new MvcException(string.Format("Cannot find the page:{0}", virtualPath));
                }
            }
        }

        public object Model
        {
            get
            {
                return this.model;
            }
        }

    }
}
