using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Routing;
using System.Text.RegularExpressions;
using Bee.Logging;
using Bee.Core;

namespace Bee.Web
{
    public static class BeeRouteHelper
    {
        private static Type mvcDispatchType = typeof(MvcRouteDispatcher);

        public static void DebugRoutes()
        {
            RouteCollection routes = RouteTable.Routes;
            using (routes.GetReadLock())
            {
                bool flag = false;
                foreach (RouteBase base2 in routes)
                {
                    Route route = base2 as Route;
                    if (route != null)
                    {
                        route.RouteHandler = new DebugRouteHandler();
                    }
                    if (route == DebugRoute.Singleton)
                    {
                        flag = true;
                    }
                }
                if (!flag)
                {
                    routes.Add(DebugRoute.Singleton);
                }
            }
        }

        public static void RegisterMvcDispatcher(Type type)
        {
            mvcDispatchType = type;
        }

        internal static MvcRouteDispatcher CreateMvcDispatcher()
        {
            if (mvcDispatchType == typeof(MvcRouteDispatcher))
            {
                return new MvcRouteDispatcher();
            }
            else
            {
                IEntityProxy entityProxy = EntityProxyManager.Instance.GetEntityProxyFromType(mvcDispatchType);
                return entityProxy.CreateInstance() as MvcRouteDispatcher;
            }
        }
    }



    public class DebugRouteHandler : IRouteHandler
    {
        // Methods
        public IHttpHandler GetHttpHandler(RequestContext requestContext)
        {
            DebugHttpHandler handler = new DebugHttpHandler();
            handler.RequestContext = requestContext;

            return handler;
        }
    }

    public class BeeRouteHandler : IRouteHandler
    {
        // Methods
        public IHttpHandler GetHttpHandler(RequestContext requestContext)
        {
            MvcRouteDispatcher handler = BeeRouteHelper.CreateMvcDispatcher();
            handler.RequestContext = requestContext;

            return handler;
        }
    }

    public class DebugRoute : Route
    {
        // Fields
        private static DebugRoute singleton = new DebugRoute();

        // Methods
        private DebugRoute()
            : base("{*catchall}", new DebugRouteHandler())
        {
        }

        // Properties
        public static DebugRoute Singleton
        {
            get
            {
                return singleton;
            }
        }
    }




    public class BeeRoute : Route
    {
        private Regex domainRegex;
        private Regex pathRegex;

        public string Domain { get; set; }

        public BeeRoute(string url, RouteValueDictionary defaults)
            : this("{host}", url, defaults)
        {

        }

        public BeeRoute(string url, object defaults)
            : this("{host}", url, defaults)
        {

        }

        public BeeRoute(string domain, string url, RouteValueDictionary defaults)
            : base(url, defaults, new BeeRouteHandler())
        {
            Domain = domain;
        }

        public BeeRoute(string domain, string url, RouteValueDictionary defaults, IRouteHandler routeHandler)
            : base(url, defaults, routeHandler)
        {
            Domain = domain;
        }

        public BeeRoute(string domain, string url, object defaults)
            : base(url, new RouteValueDictionary(defaults), new BeeRouteHandler())
        {
            Domain = domain;
        }

        public BeeRoute(string domain, string url, object defaults, IRouteHandler routeHandler)
            : base(url, new RouteValueDictionary(defaults), routeHandler)
        {
            Domain = domain;
        }

        public override RouteData GetRouteData(System.Web.HttpContextBase httpContext)
        {
            // 构造 regex
            if (domainRegex == null)
            {
                domainRegex = CreateRegex(Domain, true);
            }
            if (pathRegex == null)
            {
                pathRegex = CreateRegex(Url);
            }

            // 请求信息
            string requestDomain = httpContext.Request.Headers["host"];
            if (!string.IsNullOrEmpty(requestDomain))
            {
                if (requestDomain.IndexOf(":") > 0)
                {
                    requestDomain = requestDomain.Substring(0, requestDomain.IndexOf(":"));
                }
            }
            else
            {
                requestDomain = httpContext.Request.Url.Host;
            }
            string requestPath = httpContext.Request.AppRelativeCurrentExecutionFilePath.Substring(2) + httpContext.Request.PathInfo;

            // 匹配域名和路由
            Match domainMatch = domainRegex.Match(requestDomain);
            Match pathMatch = pathRegex.Match(requestPath);

            //httpContext.Response.Write(string.Format("requestDomain:{0}", requestDomain));
            //httpContext.Response.Write(string.Format("domainRegex:{0}", domainRegex.ToString()));

            //httpContext.Response.Write(string.Format("requestPath:{0}", requestPath));
            //httpContext.Response.Write(string.Format("pathRegex:{0}", pathRegex.ToString()));

            // 路由数据
            RouteData data = null;
            if (domainMatch.Success && pathMatch.Success)
            {
                data = new RouteData(this, RouteHandler);

                // 添加默认选项
                if (Defaults != null)
                {
                    foreach (KeyValuePair<string, object> item in Defaults)
                    {
                        data.Values[item.Key] = item.Value;
                    }
                }

                // 匹配域名路由
                for (int i = 1; i < domainMatch.Groups.Count; i++)
                {
                    Group group = domainMatch.Groups[i];
                    if (group.Success)
                    {
                        string key = domainRegex.GroupNameFromNumber(i);

                        if (!string.IsNullOrEmpty(key) && !char.IsNumber(key, 0))
                        {
                            if (!string.IsNullOrEmpty(group.Value))
                            {
                                data.Values[key] = group.Value;
                            }
                        }
                    }
                }

                // 匹配域名路径
                for (int i = 1; i < pathMatch.Groups.Count; i++)
                {
                    Group group = pathMatch.Groups[i];
                    if (group.Success)
                    {
                        string key = pathRegex.GroupNameFromNumber(i);

                        if (!string.IsNullOrEmpty(key) && !char.IsNumber(key, 0))
                        {
                            if (!string.IsNullOrEmpty(group.Value))
                            {
                                data.Values[key] = group.Value;
                            }
                        }
                    }
                }
            }

            return data;
        }

        public override VirtualPathData GetVirtualPath(RequestContext requestContext, RouteValueDictionary values)
        {
            return base.GetVirtualPath(requestContext, RemoveDomainTokens(values));
        }

        public DomainData GetDomainData(RequestContext requestContext, RouteValueDictionary values)
        {
            // 获得主机名
            string hostname = Domain;
            foreach (KeyValuePair<string, object> pair in values)
            {
                hostname = hostname.Replace("{" + pair.Key + "}", pair.Value.ToString());
            }

            // Return 域名数据
            return new DomainData
            {
                Protocol = "http",
                HostName = hostname,
                Fragment = ""
            };
        }

        private Regex CreateRegex(string source)
        {
            return CreateRegex(source, false);
        }

        private Regex CreateRegex(string source, bool domainFlag)
        {
            // 替换
            source = source.Replace("/", @"\/?");
            source = source.Replace(".", @"\.?");
            source = source.Replace("-", @"\-?");
            source = source.Replace("{", @"(?<");
            //source = source.Replace("}", @">([a-zA-Z0-9_]*))");

            if (domainFlag)
            {
                source = source.Replace("}", @">([\u0100-\uffffa-zA-Z0-9_.]*))");
            }
            else
            {
                source = source.Replace("}", @">([\u0100-\uffffa-zA-Z0-9_]*))");
            }


            return new Regex("^" + source + "$");
        }

        private RouteValueDictionary RemoveDomainTokens(RouteValueDictionary values)
        {
            Regex tokenRegex = new Regex(@"({[a-zA-Z0-9_]*})*-?\.?\/?({[a-zA-Z0-9_]*})*-?\.?\/?({[a-zA-Z0-9_]*})*-?\.?\/?({[a-zA-Z0-9_]*})*-?\.?\/?({[a-zA-Z0-9_]*})*-?\.?\/?({[a-zA-Z0-9_]*})*-?\.?\/?({[a-zA-Z0-9_]*})*-?\.?\/?({[a-zA-Z0-9_]*})*-?\.?\/?({[a-zA-Z0-9_]*})*-?\.?\/?({[a-zA-Z0-9_]*})*-?\.?\/?({[a-zA-Z0-9_]*})*-?\.?\/?({[a-zA-Z0-9_]*})*-?\.?\/?");
            Match tokenMatch = tokenRegex.Match(Domain);
            for (int i = 0; i < tokenMatch.Groups.Count; i++)
            {
                Group group = tokenMatch.Groups[i];
                if (group.Success)
                {
                    string key = group.Value.Replace("{", "").Replace("}", "");
                    if (values.ContainsKey(key))
                        values.Remove(key);
                }
            }

            return values;
        }
    }

    public class DomainData
    {
        public string Protocol { get; set; }
        public string HostName { get; set; }
        public string Fragment { get; set; }
    }

    public class DebugHttpHandler : IHttpHandler
    {

        // Methods
        private static string FormatRouteValueDictionary(RouteValueDictionary values)
        {
            if (values == null)
            {
                return "(null)";
            }
            string str = string.Empty;
            foreach (string str2 in values.Keys)
            {
                str = str + string.Format("{0} = {1}, ", str2, values[str2]);
            }
            if (str.EndsWith(", "))
            {
                str = str.Substring(0, str.Length - 2);
            }
            return str;
        }

        public void ProcessRequest(HttpContext context)
        {
            Logger.Debug("DebugHttpHandler.ProcessRequest");

            string format = "<html>\r\n<head>\r\n    <title>Route Tester</title>\r\n    <style>\r\n        body, td, th {{font-family: verdana; font-size: small;}}\r\n        .message {{font-size: .9em;}}\r\n        caption {{font-weight: bold;}}\r\n        tr.header {{background-color: #ffc;}}\r\n        label {{font-weight: bold; font-size: 1.1em;}}\r\n        .false {{color: #c00;}}\r\n        .true {{color: #0c0;}}\r\n    </style>\r\n</head>\r\n<body>\r\n<h1>Route Tester</h1>\r\n<div id=\"main\">\r\n    <p class=\"message\">\r\n        Type in a url in the address bar to see which defined routes match it. \r\n        A {{*catchall}} route is added to the list of routes automatically in \r\n        case none of your routes match.\r\n    </p>\r\n    <p><label>Route</label>: {1}</p>\r\n    <div style=\"float: left;\">\r\n        <table border=\"1\" cellpadding=\"3\" cellspacing=\"0\" width=\"300\">\r\n            <caption>Route Data</caption>\r\n            <tr class=\"header\"><th>Key</th><th>Value</th></tr>\r\n            {0}\r\n        </table>\r\n    </div>\r\n    <div style=\"float: left; margin-left: 10px;\">\r\n        <table border=\"1\" cellpadding=\"3\" cellspacing=\"0\" width=\"300\">\r\n            <caption>Data Tokens</caption>\r\n            <tr class=\"header\"><th>Key</th><th>Value</th></tr>\r\n            {4}\r\n        </table>\r\n    </div>\r\n    <hr style=\"clear: both;\" />\r\n    <table border=\"1\" cellpadding=\"3\" cellspacing=\"0\">\r\n        <caption>All Routes</caption>\r\n        <tr class=\"header\">\r\n            <th>Matches Current Request</th>\r\n            <th>Url</th>\r\n            <th>Defaults</th>\r\n            <th>Constraints</th>\r\n            <th>DataTokens</th>\r\n        </tr>\r\n        {2}\r\n    </table>\r\n    <hr />\r\n    <strong>AppRelativeCurrentExecutionFilePath</strong>: {3}\r\n</div>\r\n</body>\r\n</html>";
            string str2 = string.Empty;
            RouteData routeData = this.RequestContext.RouteData;
            RouteValueDictionary values = routeData.Values;
            RouteBase base2 = routeData.Route;
            string str3 = string.Empty;
            using (RouteTable.Routes.GetReadLock())
            {
                foreach (RouteBase base3 in RouteTable.Routes)
                {
                    bool flag = base3.GetRouteData(this.RequestContext.HttpContext) != null;
                    string str4 = string.Format("<span class=\"{0}\">{0}</span>", flag);
                    string url = "n/a";
                    string str6 = "n/a";
                    string str7 = "n/a";
                    string str8 = "n/a";
                    Route route = base3 as Route;
                    if (route != null)
                    {
                        url = route.Url;
                        str6 = FormatRouteValueDictionary(route.Defaults);
                        str7 = FormatRouteValueDictionary(route.Constraints);
                        str8 = FormatRouteValueDictionary(route.DataTokens);
                    }
                    str3 = str3 + string.Format("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{3}</td></tr>", new object[] { str4, url, str6, str7, str8 });
                }
            }
            string str9 = "n/a";
            string str10 = "";
            if (base2 is DebugRoute)
            {
                str9 = "<strong class=\"false\">NO MATCH!</strong>";
            }
            else
            {
                foreach (string str11 in values.Keys)
                {
                    str2 = str2 + string.Format("\t<tr><td>{0}</td><td>{1}&nbsp;</td></tr>", str11, values[str11]);
                }
                foreach (string str11 in routeData.DataTokens.Keys)
                {
                    str10 = str10 + string.Format("\t<tr><td>{0}</td><td>{1}&nbsp;</td></tr>", str11, routeData.DataTokens[str11]);
                }
                Route route2 = base2 as Route;
                if (route2 != null)
                {
                    str9 = route2.Url;
                }
            }
            context.Response.Write(string.Format(format, new object[] { str2, str9, str3, context.Request.AppRelativeCurrentExecutionFilePath, str10 }));

           
        }

        // Properties
        public bool IsReusable
        {
            get
            {
                return true;
            }
        }

        public RequestContext RequestContext
        {

            get;
            set;
        }
    }

    public static class RouteCollectionExtensions
    {
        public static void IgnoreRoute(this RouteCollection routes, string url)
        {
            routes.IgnoreRoute(url, null);
        }

        public static void IgnoreRoute(this RouteCollection routes, string url, object constraints)
        {
            if (routes == null)
            {
                throw new ArgumentNullException("routes");
            }
            if (url == null)
            {
                throw new ArgumentNullException("url");
            }
            IgnoreRouteInternal internal3 = new IgnoreRouteInternal(url);
            internal3.Constraints = new RouteValueDictionary(constraints);
            IgnoreRouteInternal item = internal3;
            routes.Add(item);
        }

        public static Route MapRoute(this RouteCollection routes, string name, string url)
        {
            return routes.MapRoute(name, url, null, null);
        }

        public static Route MapRoute(this RouteCollection routes, string name, string url, object defaults)
        {
            return routes.MapRoute(name, url, defaults, null);
        }

        public static Route MapRoute(this RouteCollection routes, string name, string url, string[] namespaces)
        {
            return routes.MapRoute(name, url, null, null, namespaces);
        }

        public static Route MapRoute(this RouteCollection routes, string name, string url, object defaults, object constraints)
        {
            return routes.MapRoute(name, url, defaults, constraints, null);
        }

        public static Route MapRoute(this RouteCollection routes, string name, string url, object defaults, string[] namespaces)
        {
            return routes.MapRoute(name, url, defaults, null, namespaces);
        }

        public static Route MapRoute(this RouteCollection routes, string name, string url, object defaults, object constraints, string[] namespaces)
        {
            if (routes == null)
            {
                throw new ArgumentNullException("routes");
            }
            if (url == null)
            {
                throw new ArgumentNullException("url");
            }
            Route route2 = new Route(url, new BeeRouteHandler());
            route2.Defaults = new RouteValueDictionary(defaults);
            route2.Constraints =new RouteValueDictionary(constraints);
            route2.DataTokens = new RouteValueDictionary();
            Route route = route2;
            if ((namespaces != null) && (namespaces.Length > 0))
            {
                route.DataTokens["Namespaces"] = namespaces;
            }
            routes.Add(name, route);
            return route;
        }

        // Nested Types
        private sealed class IgnoreRouteInternal : Route
        {
            // Methods
            public IgnoreRouteInternal(string url)
                : base(url, new StopRoutingHandler())
            {
            }

            public override VirtualPathData GetVirtualPath(RequestContext requestContext, RouteValueDictionary routeValues)
            {
                return null;
            }
        }
    }

}
