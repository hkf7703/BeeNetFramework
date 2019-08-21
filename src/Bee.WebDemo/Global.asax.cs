using Bee.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;

namespace Bee.WebDemo
{
    public class Global : System.Web.HttpApplication
    {

        void Application_Start(object sender, EventArgs e)
        {
            RouteCollection routes = RouteTable.Routes;

            //RouteTable.Routes.Add("General", new Route("{controller}/{action}", new StopRoutingHandler()));

            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // Ignore the assets directory which contains images, js, css & html
            routes.IgnoreRoute("images/{*pathInfo}");
            routes.IgnoreRoute("js/{*pathInfo}");
            routes.IgnoreRoute("style/{*pathInfo}");
            //Exclude favicon (google toolbar request gif file as fav icon which is weird)
            routes.IgnoreRoute("{*favicon}", new
            {
                favicon = @"(.*/)?favicon.([iI][cC][oO]|[gG][iI][fF])(/.*)?"
            });

            // 在应用程序启动时运行的代码
            RouteTable.Routes.Add("General", new BeeRoute("{host}", "{controller}/{action}",
                new
                {
                    host = "test",
                    controller = "Home",
                    action = "Index"
                }));


            // 调试路由用 去掉注释试试
            // BeeRouteHelper.DebugRoutes();

            BeeRouteHelper.RegisterMvcDispatcher(typeof(MyMvcDispatcher));



        }

        void Application_End(object sender, EventArgs e)
        {
            //  在应用程序关闭时运行的代码
        }

        void Application_Error(object sender, EventArgs e)
        {
            // 在出现未处理的错误时运行的代码

        }

        void Session_Start(object sender, EventArgs e)
        {
            // 在新会话启动时运行的代码

        }

        void Session_End(object sender, EventArgs e)
        {
            // 在会话结束时运行的代码。 
            // 注意: 只有在 Web.config 文件中的 sessionstate 模式设置为
            // InProc 时，才会引发 Session_End 事件。如果会话模式设置为 StateServer 
            // 或 SQLServer，则不会引发该事件。

        }

    }

    public class MyMvcDispatcher : MvcRouteDispatcher
    {
        protected override void ActionExecuting(ActionExecutingArgs actionExcutingArgs)
        {
            string controllerName = actionExcutingArgs.ControllerName.ToLower();
            string actionName = actionExcutingArgs.ActionName.ToLower();

            base.ActionExecuting(actionExcutingArgs);
        }
    }
}