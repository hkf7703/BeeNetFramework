using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.SessionState;
using System.Web.Caching;
using System.Collections.Specialized;

namespace Bee.Web
{
    /// <summary>
    /// 为了解决并发时， session会自我锁定， 导致同一个session同时只有一个请求在工作。
    /// 轻量化session
    /// </summary>
    public sealed class ProcCacheSessionStateStore : SessionStateStoreProviderBase
    {
        private CacheItemRemovedCallback _callback;
        private SessionStateItemExpireCallback _expireCallback;
        internal static readonly int CACHEKEYPREFIXLENGTH = "ProcCacheSession_".Length;

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

        private string CreateSessionStateCacheKey(string id)
        {
            return ("ProcCacheSession_" + id);
        }

        public override void CreateUninitializedItem(HttpContext context, string id, int timeout)
        {
            string key = this.CreateSessionStateCacheKey(id);
            CheckIdLength(id, true);
            InProcSessionState state = new InProcSessionState(null, null, timeout);
            HttpRuntime.Cache.Add(key, state, null, Cache.NoAbsoluteExpiration, new TimeSpan(0, timeout, 0), CacheItemPriority.NotRemovable, this._callback);
        }

        internal static bool CheckIdLength(string id, bool throwOnFail)
        {
            if (id.Length <= 80)
                return true;

            if (throwOnFail)
                throw new HttpException(string.Format("SessionID too long: {0}", id));

            return false;
        }

        public override void Dispose()
        {
        }

        private SessionStateStoreData DoGet(HttpContext context, string id, bool exclusive, out bool locked, out TimeSpan lockAge, out object lockId, out SessionStateActions actionFlags)
        {
            string key = this.CreateSessionStateCacheKey(id);
            locked = false;
            lockId = null;
            lockAge = TimeSpan.Zero;
            actionFlags = SessionStateActions.None;

            CheckIdLength(id, true);
            InProcSessionState state = (InProcSessionState)HttpRuntime.Cache.Get(key);
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
                name = "InProc Cache Session State Provider";
            base.Initialize(name, config);
            this._callback = new CacheItemRemovedCallback(this.OnCacheItemRemoved);
        }

        public override void InitializeRequest(HttpContext context)
        {
        }

        public void OnCacheItemRemoved(string key, object value, CacheItemRemovedReason reason)
        {
            InProcSessionState state = (InProcSessionState)value;
            if (this._expireCallback != null)
            {
                string id = key.Substring(CACHEKEYPREFIXLENGTH);
                this._expireCallback(id, CreateLegitStoreData(null, state._sessionItems, state._staticObjects, state._timeout));
            }
        }

        public override void ReleaseItemExclusive(HttpContext context, string id, object lockId)
        {
        }

        public override void RemoveItem(HttpContext context, string id, object lockId, SessionStateStoreData item)
        {
            string key = this.CreateSessionStateCacheKey(id);
            CheckIdLength(id, true);
            InProcSessionState state = (InProcSessionState)HttpRuntime.Cache.Get(key);
            if (state != null)
                HttpRuntime.Cache.Remove(key);
        }

        public override void ResetItemTimeout(HttpContext context, string id)
        {
        }

        public override void SetAndReleaseItemExclusive(HttpContext context, string id, SessionStateStoreData item, object lockId, bool newItem)
        {
            string key = this.CreateSessionStateCacheKey(id);

            ISessionStateItemCollection sessionItems = null;
            HttpStaticObjectsCollection staticObjects = null;
            CheckIdLength(id, true);
            if (item.Items.Count > 0)
                sessionItems = item.Items;
            if (!item.StaticObjects.NeverAccessed)
                staticObjects = item.StaticObjects;

            InProcSessionState state2 = new InProcSessionState(sessionItems, staticObjects, item.Timeout);
            HttpRuntime.Cache.Insert(key, state2, null, Cache.NoAbsoluteExpiration, new TimeSpan(0, state2._timeout, 0), CacheItemPriority.NotRemovable, this._callback);
        }

        public override bool SetItemExpireCallback(SessionStateItemExpireCallback expireCallback)
        {
            this._expireCallback = expireCallback;
            return true;
        }




        internal sealed class InProcSessionState
        {
            internal ISessionStateItemCollection _sessionItems;
            internal HttpStaticObjectsCollection _staticObjects;
            internal int _timeout;

            internal InProcSessionState(ISessionStateItemCollection sessionItems, HttpStaticObjectsCollection staticObjects, int timeout)
            {
                this.Copy(sessionItems, staticObjects, timeout);
            }

            internal void Copy(ISessionStateItemCollection sessionItems, HttpStaticObjectsCollection staticObjects, int timeout)
            {
                this._sessionItems = sessionItems;
                this._staticObjects = staticObjects;
                this._timeout = timeout;
            }
        }

    }
}
