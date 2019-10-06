using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Bee.Util;
using System.Text.RegularExpressions;
using Bee.Core;
using Bee.Logging;
using System.Collections.Specialized;
using System.Web.SessionState;
using System.Diagnostics;
using System.Web.Routing;
using System.IO;

namespace Bee.Web
{
    public class ActionExecutingArgs
    {
        public ActionExecutingArgs(string controllerName, string actionName, BeeDataAdapter dataAdapter)
        {
            ControllerName = controllerName;
            ActionName = actionName;
            Data = dataAdapter;
            Result = ActionExecutingResult.OK;
        }

        public string ControllerName { get; set; }
        public string ActionName { get; set; }
        public BeeDataAdapter Data { get; set; }
        public ActionExecutingResult Result { get; set; }
        public string Message { get; set; }
        public int Code { get; set; }
    }

    public class MvcDispatcher : IHttpHandler, IRequiresSessionState , IHttpAsyncHandler
    {
        internal static readonly string AjaxUrlPattern
            = @"/(?<name>(\w[\./\w]*)?\w+)[/\.](?<method>\w+)\.[a-zA-Z]+";

        internal static readonly string DefaultActionPattern = @"(?<method>\w+)\.[a-zA-Z]+";

        private static readonly bool LogRequestFlag = ConfigUtil.GetAppSettingValue<bool>("LogRequestFlag", false);

        private AsyncTaskDelegate del;
        protected delegate void AsyncTaskDelegate(HttpContext context);

        //private HttpContext httpContext;

        static MvcDispatcher()
        {
            Bee.Web.FxVirtualPathProvider.RegisterToHostingEnvironment(false);
        }

        public bool IsReusable
        {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context)
        {
            System.Runtime.Remoting.Messaging.CallContext.HostContext = context;
            Stopwatch stopwatch = new Stopwatch();
            try
            {
                stopwatch.Start();

                BeeDataAdapter routeData = GetRouteData(context);

                string controllerName = routeData[Constants.BeeControllerName] as string;
                string actionName = routeData[Constants.BeeActionName] as string;


                HttpContext httpContext = context;
                //this.httpContext = context;
                BeeDataAdapter dataAdapter = new BeeDataAdapter(routeData);

                NameValueCollection formParams = httpContext.Request.Form;
                foreach (string key in formParams.Keys)
                {
                    if (!string.IsNullOrEmpty(key))
                    {
                        dataAdapter.Add(key.ToLower(), StringUtil.HtmlEncode(formParams[key]));
                    }
                }

                formParams = httpContext.Request.QueryString;
                foreach (string key in formParams.Keys)
                {
                    if (!string.IsNullOrEmpty(key))
                    {
                        dataAdapter.Add(key.ToLower(), StringUtil.HtmlEncode(formParams[key]));
                    }
                }
                // 解析inputstream
                string json = new StreamReader(httpContext.Request.InputStream).ReadToEnd();
                if (!string.IsNullOrEmpty(json) && json.StartsWith("{"))
                {
                    var jObject = Newtonsoft.Json.Linq.JObject.Parse(json);
                    foreach(var item in jObject)
                    {
                        dataAdapter.Add(item.Key, item.Value);
                    }
                }

                if (LogRequestFlag)
                {
                    BeeDataAdapter cookieData = new BeeDataAdapter();
                    foreach (string key in context.Request.Cookies.AllKeys)
                    {
                        cookieData.Add(key, context.Request.Cookies[key].Value);
                    }

                    Logger.Debug(@"
cookie:{0}
Request:{1}".FormatWith(cookieData.ToString(), dataAdapter.ToString()));
                }

                ActionExecutingArgs args = new ActionExecutingArgs(controllerName, actionName, dataAdapter);
                ActionExecuting(args); // 提供拦截通道
                if (args.Result != ActionExecutingResult.OK)
                {
                    BeeMvcResult mvcResult = new BeeMvcResult();
                    mvcResult.code = 400;
                    if(args.Code > 0)
                    {
                        mvcResult.code = args.Code;
                    }
                    mvcResult.msg = args.Message;

                    WriteMvcResult(httpContext, mvcResult);
                    return;
                }

                InnerExecuteAction(context, controllerName, actionName, dataAdapter);

                stopwatch.Stop();
                if (stopwatch.ElapsedMilliseconds > 5000)
                {
                    Logger.Debug(string.Format("{0}耗时较长， 耗时：{1}ms", context.Request.Url.ToString(), stopwatch.ElapsedMilliseconds));
                }

            }
            catch (Exception e)
            {
                string error = ResourceUtil.ReadToEndFromCache(typeof(MvcDispatcher).Assembly, "Bee.Web.Error.htm", false);

                context.Response.Write(string.Format(error, e.Message, GetFullException(e)));

                Logger.Error(e.Message, e);

                Logger.Log(LogLevel.Core, e.Message, e);
            }
        }

        private static string GetFullException(Exception e)
        {
            StringBuilder builder = new StringBuilder();

            builder.Append(e.ToString());

            Exception innerException = e.InnerException;
            while (innerException != null)
            {
                System.Reflection.ReflectionTypeLoadException reflectionTypeLoadException
                    = innerException as System.Reflection.ReflectionTypeLoadException;
                if (reflectionTypeLoadException != null && reflectionTypeLoadException.LoaderExceptions.Length > 0)
                {
                    innerException = reflectionTypeLoadException.LoaderExceptions[0];
                }
                
                builder.AppendLine();
                builder.AppendLine();
                builder.AppendLine();
                builder.AppendFormat("InnerException:{0}", innerException.ToString());

                innerException = innerException.InnerException;
                
            }

            return builder.ToString();
        }


        protected virtual BeeDataAdapter GetRouteData(HttpContext context)
        {
            BeeDataAdapter result = new BeeDataAdapter();

            string controllerName = ControllerManager.DefaultControllerName;
            string actionName = string.Empty;

            string path = context.Request.AppRelativeCurrentExecutionFilePath;

            Match match = Regex.Match(path, AjaxUrlPattern);

            if (!match.Success)
            {
                match = Regex.Match(path, DefaultActionPattern);
                if (!match.Success)
                {
                    throw new MvcException("The url is incorrect!");
                }
                else
                {
                    actionName = match.Groups["method"].Value;
                }
            }
            else
            {
                controllerName = match.Groups["name"].Value.Replace("/", ".");
                actionName = match.Groups["method"].Value;
            }

            if (string.IsNullOrEmpty(controllerName))
            {
                controllerName = "AuthMain";
            }


            result[Constants.BeeControllerName] = controllerName;
            result[Constants.BeeActionName] = actionName;

            return result;
        }

        /// <summary>
        /// Before execute the action.
        /// </summary>
        protected virtual void ActionExecuting(ActionExecutingArgs actionExcutingArgs)
        {

        }

        protected virtual void ActionError(string controllerName, string actionName, BeeDataAdapter dataAdapter, Exception innerException)
        { 
            BeeMvcResult mvcResult = new BeeMvcResult();
            mvcResult.code = 400;
            mvcResult.msg = innerException.Message;

            var httpException = innerException as HttpException;
            if(httpException != null)
            {
                mvcResult.code = httpException.GetHttpCode();
            }

            var coreException = innerException as CoreException;
            if(coreException != null)
            {
                mvcResult.code = 405;
            }

            Logger.Error("Invoke {0}.{1} error.\r\n{2}".FormatWith(controllerName, actionName, dataAdapter), innerException);

            WriteMvcResult(HttpContextUtil.CurrentHttpContext, mvcResult);
        }


        private void InnerExecuteAction(HttpContext httpContext, string controllerName, string actionName, BeeDataAdapter dataAdapter)
        {
            try
            {
                CoreExecuteAction(httpContext, controllerName, actionName, dataAdapter);
            }
            catch (Exception e)
            {
                ActionError(controllerName, actionName, dataAdapter, e);
            }
        }


        internal static void ExecuteAction(string controllerName, string actionName, BeeDataAdapter dataAdapter)
        {
            ExecuteAction(HttpContextUtil.CurrentHttpContext, controllerName, actionName, dataAdapter);
        }


        internal static void ExecuteAction(HttpContext httpContext, string controllerName, string actionName, BeeDataAdapter dataAdapter)
        {
            try
            {
                CoreExecuteAction(httpContext, controllerName, actionName, dataAdapter);
            }
            catch (Exception e)
            {
                BeeMvcResult mvcResult = new BeeMvcResult();
                mvcResult.code = 400;
                mvcResult.msg = e.Message;

                WriteMvcResult(httpContext, mvcResult);
            }
        }


        private static void WriteMvcResult(HttpContext httpContext, BeeMvcResult mvcResult)
        {
            httpContext.Response.AppendHeader("Content-Type", "application/json; charset=utf-8");

            httpContext.Response.Write(SerializeUtil.ToJson(mvcResult));
        }

        private static void CoreExecuteAction(HttpContext httpContext, string controllerName, string actionName, BeeDataAdapter dataAdapter)
        {
            if (dataAdapter == null)
            {
                dataAdapter = new BeeDataAdapter();
            }

            //HttpContext httpContext = HttpContextUtil.CurrentHttpContext;

            // 加入Area特性
            string areaName = dataAdapter.TryGetValue<string>(Constants.BeeAreaName, string.Empty);
            if (!string.IsNullOrEmpty(areaName))
            {
                controllerName = "{0}|{1}".FormatWith(areaName, controllerName);
            }

            ControllerInfo controllerInfo = ControllerManager.Instance.GetControllerInfo(controllerName);
            if (controllerInfo == null)
            {
                throw new MvcException(string.Format("Cannot find {0} controller.", controllerName));
            }

            ControllerBase instance = controllerInfo.CreateInstance() as ControllerBase;
            //ReflectionUtil.CreateInstance(controllerInfo.Type) as ControllerBase;
            instance.Init(httpContext, controllerInfo, dataAdapter, actionName);

            GeneralUtil.CatchAll(delegate
            {
                instance.OnBeforeAction(actionName, dataAdapter);
            });

            object result = null;
            try
            {
                // 假如不匹配任何方法， 则会抛出CoreException
                result = controllerInfo.Invoke(instance, actionName, dataAdapter);
            }
            catch(Exception e)
            {
                Logger.Error("Invoke {0}.{1} error.\r\n{2}"
                    .FormatWith(controllerName, actionName, dataAdapter), e);

                throw;
            }

            GeneralUtil.CatchAll(delegate
            {
                instance.OnAfterAction(actionName, dataAdapter);
            });
            

            // 加入ControllerName及ActionName信息
            dataAdapter.Add(Constants.BeeControllerName, controllerName);
            dataAdapter.Add(Constants.BeeActionName, actionName);
            if (result != null)
            {
                ActionResult actionResult = result as ActionResult;
                if (actionResult != null)
                {
                    if (string.IsNullOrEmpty(actionResult.ControllerName))
                    {
                        actionResult.ControllerName = controllerName;
                    }
                    if (string.IsNullOrEmpty(actionResult.ActionName))
                    {
                        actionResult.ActionName = actionName;
                    }

                    actionResult.Init(instance, dataAdapter);

                    try
                    {
                        actionResult.Ouput(httpContext);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Rend {0}.{1} error.\r\n{2}"
                                .FormatWith(controllerName, actionName, dataAdapter), ex);
                        throw new MvcException(ex.Message, ex);
                    }
                }
                else
                {
                    BeeMvcResult mvcResult = new BeeMvcResult();
                    mvcResult.data = result;
                    mvcResult.code = 200;

                    WriteMvcResult(httpContext, mvcResult);
                }
            }
            else
            {
                httpContext.Response.AppendHeader("Content-Type", "application/json; charset=utf-8");
                BeeMvcResult mvcResult = new BeeMvcResult();
                mvcResult.code = 200;

                WriteMvcResult(httpContext, mvcResult);
            }
        }

        #region IHttpAsyncHandler 成员

        public IAsyncResult BeginProcessRequest(HttpContext context, AsyncCallback cb, object extraData)
        {
            this.del = new AsyncTaskDelegate(ProcessRequest);

            return del.BeginInvoke(context, cb, extraData);
        }

        public void EndProcessRequest(IAsyncResult result)
        {
            this.del.EndInvoke(result);
        }

        #endregion
    }

    public class TestMvcDispatcher : MvcDispatcher, IHttpAsyncHandler
    {

    }

    public class MvcRouteDispatcher : MvcDispatcher
    {

        protected override BeeDataAdapter GetRouteData(HttpContext context)
        {
            BeeDataAdapter result = new BeeDataAdapter();

            RouteValueDictionary routeData = RequestContext.RouteData.Values;
            foreach (string item in routeData.Keys)
            {
                if (string.Compare("controller", item, true) == 0)
                {
                    result.Add(Constants.BeeControllerName, routeData[item]);
                }
                else if (string.Compare("action", item, true) == 0)
                {
                    result.Add(Constants.BeeActionName, routeData[item]);
                }
                else if (string.Compare("area", item, true) == 0)
                {
                    result.Add(Constants.BeeAreaName, routeData[item]);
                }
                else
                {
                    result.Add(item, routeData[item]);
                }
            }

            return result;
        }

        public RequestContext RequestContext
        {
            get;
            set;
        }

    }
}
