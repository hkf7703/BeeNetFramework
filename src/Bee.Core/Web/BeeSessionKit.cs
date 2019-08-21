using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.SessionState;

namespace Bee.Web
{
    public class BeeSessionKit
    {
        private HttpSessionState innerSession = HttpContext.Current.Session;

        public virtual object this[string name]
        {
            get
            {
                return innerSession[name];
            }
            set
            {
                innerSession[name] = value;
            }
        }

        public static BeeDataAdapter Current
        {
            get
            {
                string sessionId = HttpContext.Current.Request.Cookies["ASP.NET_SessionId"].Value;
                string cacheName = String.Format("Session_Cache_{0}", sessionId);

                BeeDataAdapter result = Caching.CacheManager.Instance.GetEntity<BeeDataAdapter>(cacheName);
                if (result == null)
                {
                    result = new BeeDataAdapter();
                    Caching.CacheManager.Instance.AddEntity<BeeDataAdapter>(cacheName, result, TimeSpan.FromHours(2));
                }
                else
                {
                    Caching.CacheManager.Instance.RemoveCache(cacheName);
                    Caching.CacheManager.Instance.AddEntity<BeeDataAdapter>(cacheName, result, TimeSpan.FromHours(2));
                }

                return result;
            }
        }
    }
}
