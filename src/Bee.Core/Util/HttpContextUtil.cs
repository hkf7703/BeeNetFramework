using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Bee.Util
{
    /// <summary>
    /// The Util of the HttpContext.
    /// </summary>
    public static class HttpContextUtil
    {
        /// <summary>
        /// The current HttpContext.
        /// </summary>
        public static HttpContext CurrentHttpContext
        {
            get
            {
                return HttpContext.Current;
            }
        }

        public static string Host
        {
            get
            {
                return CurrentHttpContext.Request.Url.Host;
            }
        }

        public static string EnterUrl
        {
            get
            {
                Uri url = CurrentHttpContext.Request.Url;
                if (url.Port != 80)
                {
                    return "{0}://{1}:{2}".FormatWith(url.Scheme, url.Host, url.Port);
                }
                else
                {
                    return "{0}://{1}".FormatWith(url.Scheme, url.Host);
                }
            }
        }

        /// <summary>
        /// The remote IP.
        /// </summary>
        public static string RemoteIP
        {
            get
            {
                string result = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                if (string.IsNullOrEmpty(result))
                {
                    result = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
                }

                if (result == "::1")
                {
                    result = "127.0.0.1";
                }

                return result;
            }
        }

        public static void ForSwfUpload(HttpContext context)
        {
            string sessionTokenKey = ConfigUtil.GetAppSettingValue<string>("SessionTokenKey");
            if (string.IsNullOrEmpty(sessionTokenKey))
            {
                sessionTokenKey = "bee_sess";
            }

            try
            {
                string session_param_name = "ASPSESSID";
                string session_cookie_name = "ASP.NET_SESSIONID";
                string session_value = context.Request.Form[session_param_name] ?? context.Request.QueryString[session_param_name];
                if (!string.IsNullOrEmpty(session_value))
                {
                    session_value = Bee.Util.SecurityUtil.DecryptS(session_value, sessionTokenKey);
                    if (session_value != null) 
                    { 
                        UpdateCookie(context, session_cookie_name, session_value); 
                    }
                }
            }
            catch (Exception) { }
        }

        private static void UpdateCookie(HttpContext context, string cookie_name, string cookie_value)
        {
            HttpCookie cookie = HttpContext.Current.Request.Cookies.Get(cookie_name);
            if (cookie == null)
            {
                HttpCookie cookie1 = new HttpCookie(cookie_name, cookie_value);
                context.Response.Cookies.Add(cookie1);
            }
            else
            {
                cookie.Value = cookie_value;
                HttpContext.Current.Request.Cookies.Set(cookie);
            }
        }

    }
}
