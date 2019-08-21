using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Web;
using Bee.Util;
using System.Web.SessionState;
using System.Web.Script.Serialization;

namespace Bee.Web
{
    /// <summary>
    /// 使用Cookie实现SessionStateStoreProviderBase
    /// 注意：它只适合保存简单的基元类型数据。
    /// </summary>
    public class CookieSessionStateStore : SessionStateStoreProviderBase
    {
        private static readonly string CookieName = "CookieSessionKey";

        private void SaveToCookie(CookieSessionState state, int? timeout)
        {
            string json = state.ToJson();
            HttpCookie cookie = new HttpCookie(CookieName, json);
            cookie.HttpOnly = true;

            if (timeout.HasValue && timeout > 0)
                cookie.Expires = DateTime.Now.AddMinutes(timeout.Value);

            HttpContext.Current.Response.AppendCookie(cookie);
        }

        private CookieSessionState GetFromCookie()
        {
            HttpCookie cookie = HttpContext.Current.Request.Cookies[CookieName];
            if (cookie == null)
                return null;

            return CookieSessionState.FromJson(cookie.Value);
        }

        public override SessionStateStoreData CreateNewStoreData(HttpContext context, int timeout)
        {
            return CreateLegitStoreData(context, null, null, timeout);
        }

        internal static SessionStateStoreData CreateLegitStoreData(HttpContext context, ISessionStateItemCollection sessionItems, HttpStaticObjectsCollection staticObjects, int timeout)
        {
            if (sessionItems == null)
                sessionItems = new SessionStateItemCollection();
            if (staticObjects == null && context != null)
                staticObjects = SessionStateUtility.GetSessionStaticObjects(context);
            return new SessionStateStoreData(sessionItems, staticObjects, timeout);
        }

        public override void CreateUninitializedItem(HttpContext context, string id, int timeout)
        {
            CookieSessionState state = new CookieSessionState(null, null, timeout);

            SaveToCookie(state, timeout);
        }

        public override void Dispose()
        {
        }

        private SessionStateStoreData DoGet(HttpContext context, string id, bool exclusive, out bool locked, out TimeSpan lockAge, out object lockId, out SessionStateActions actionFlags)
        {
            locked = false;
            lockId = null;
            lockAge = TimeSpan.Zero;
            actionFlags = SessionStateActions.None;

            CookieSessionState state = GetFromCookie();
            if (state == null)
                return null;

            return CreateLegitStoreData(context, state._sessionItems, state._staticObjects, state._timeout);
        }

        public override void EndRequest(HttpContext context)
        {
        }

        public override SessionStateStoreData GetItem(HttpContext context, string id, out bool locked, out TimeSpan lockAge, out object lockId, out SessionStateActions actionFlags)
        {
            return this.DoGet(context, id, false, out locked, out lockAge, out lockId, out actionFlags);
        }

        public override SessionStateStoreData GetItemExclusive(HttpContext context, string id, out bool locked, out TimeSpan lockAge, out object lockId, out SessionStateActions actionFlags)
        {
            return this.DoGet(context, id, true, out locked, out lockAge, out lockId, out actionFlags);
        }

        public override void Initialize(string name, NameValueCollection config)
        {
            if (string.IsNullOrEmpty(name))
                name = "Cookie Session State Provider";
            base.Initialize(name, config);
        }

        public override void InitializeRequest(HttpContext context)
        {
        }


        public override void ReleaseItemExclusive(HttpContext context, string id, object lockId)
        {
        }

        public override void RemoveItem(HttpContext context, string id, object lockId, SessionStateStoreData item)
        {
            HttpCookie cookie = new HttpCookie(CookieName);
            cookie.HttpOnly = true;
            cookie.Expires = DateTime.MinValue;

            HttpContext.Current.Response.AppendCookie(cookie);
        }

        public override void ResetItemTimeout(HttpContext context, string id)
        {
        }

        public override void SetAndReleaseItemExclusive(HttpContext context, string id, SessionStateStoreData item, object lockId, bool newItem)
        {
            ISessionStateItemCollection sessionItems = null;
            HttpStaticObjectsCollection staticObjects = null;

            if (item.Items.Count > 0)
                sessionItems = item.Items;
            if (!item.StaticObjects.NeverAccessed)
                staticObjects = item.StaticObjects;

            CookieSessionState state2 = new CookieSessionState(sessionItems, staticObjects, item.Timeout);
            SaveToCookie(state2, state2._timeout);
        }

        public override bool SetItemExpireCallback(SessionStateItemExpireCallback expireCallback)
        {
            return true;
        }



        public sealed class SessionStateItem
        {
            public Dictionary<string, object> Dict;
            public int Timeout;
        }


        public sealed class CookieSessionState
        {
            internal ISessionStateItemCollection _sessionItems;
            internal HttpStaticObjectsCollection _staticObjects;
            internal int _timeout;

            internal CookieSessionState(ISessionStateItemCollection sessionItems, HttpStaticObjectsCollection staticObjects, int timeout)
            {
                this.Copy(sessionItems, staticObjects, timeout);
            }

            internal void Copy(ISessionStateItemCollection sessionItems, HttpStaticObjectsCollection staticObjects, int timeout)
            {
                this._sessionItems = sessionItems;
                this._staticObjects = staticObjects;
                this._timeout = timeout;
            }

            public string ToJson()
            {
                // 这里忽略_staticObjects这个成员。

                if (_sessionItems == null || _sessionItems.Count == 0)
                    return null;

                Dictionary<string, object> dict = new Dictionary<string, object>(_sessionItems.Count);

                string key;
                NameObjectCollectionBase.KeysCollection keys = _sessionItems.Keys;
                for (int i = 0; i < keys.Count; i++)
                {
                    key = keys[i];
                    dict.Add(key, _sessionItems[key]);
                }

                SessionStateItem item = new SessionStateItem { Dict = dict, Timeout = this._timeout };

                return HttpUtility.UrlEncode((new JavaScriptSerializer()).Serialize(item));

                // 由于使用Dictionary<string, object>类型，造成复杂类型在序列化时就丢失了它们的类型信息，
                // 因此，在下面的反序列化时，就不能还原正原的类型。
                // 也正是因为此原因，CookieSessionStateStore只适合保存简单的基元类型数据。
            }

            public static CookieSessionState FromJson(string json)
            {
                if (string.IsNullOrEmpty(json))
                    return null;

                try
                {
                    SessionStateItem item =
                        (new JavaScriptSerializer()).Deserialize<SessionStateItem>(HttpUtility.UrlDecode(json));


                    SessionStateItemCollection collections = new SessionStateItemCollection();

                    foreach (KeyValuePair<string, object> kvp in item.Dict)
                        collections[kvp.Key] = kvp.Value;

                    return new CookieSessionState(collections, null, item.Timeout);
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}
