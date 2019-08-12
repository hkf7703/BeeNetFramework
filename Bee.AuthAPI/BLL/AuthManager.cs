using Bee.AuthAPI.Models;
using Bee.Caching;
using Bee.Core;
using Bee.Data;
using Bee.Util;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace Bee.AuthAPI.BLL
{
    public class AuthManager
    {

        public static readonly string DBConnectKey = "dbconn";

        public static readonly string AuthConnString = ConfigUtil.GetAppSettingValue<string>(DBConnectKey);

        public static readonly string SessionUserId = "Session_UserId";

        public static readonly string AllShownPermission = "Cache_AllShownPermission";
        public static readonly string UserAllPermission = "Cache_UserAllPermission";

        public static readonly int CacheMinute = 5;

        private static AuthManager instance = new AuthManager();

        private static readonly Regex ExAttributeRegex = new Regex("(?<name>[^?&]*)=(?<value>[^&]+)");

        private AuthManager()
        {

        }

        public static AuthManager Instance
        {
            get
            {
                return instance;
            }
        }

        public bool LoginFlag
        {
            get
            {
                AuthUser user = null;
                if (HttpContextUtil.CurrentHttpContext.Session != null)
                {
                    user =
                        HttpContext.Current.Session[SessionUserId] as AuthUser;
                }

                return user != null;
            }
        }

        public AuthUser InnerGetCurrentUser()
        {
            AuthUser user = null;
            if (HttpContextUtil.CurrentHttpContext.Session != null)
            {
                user =
                    HttpContext.Current.Session[SessionUserId] as AuthUser;
            }

            return user;
        }

        public AuthUser CurrentUser
        {
            get
            {
                AuthUser user = null;
                if (HttpContextUtil.CurrentHttpContext.Session != null)
                {
                    user =
                        HttpContext.Current.Session[SessionUserId] as AuthUser;
                    if (user == null)
                    {
                        throw new CoreException("Session过期或丢失， 请重新登入");
                    }
                }

                return user;
            }
        }

        public AuthUser AuthLogin(string userName, string password, bool logFlag)
        {
            using (DbSession dbSession = new DbSession(AuthConnString))
            {
                AuthUser user =
                    dbSession.Query<AuthUser>(SqlCriteria.New.Equal("UserName", userName)
                    .Equal("status", 0)).FirstOrDefault();

                if (user != null)
                {
                    // 无论系统设置如何， 加密还是未加密， 均可登入。
                    string md5Password = password;
                    md5Password = SecurityUtil.MD5EncryptS(password);

                    if (user.Password == password || user.Password == md5Password)
                    {
                        HttpContextUtil.CurrentHttpContext.Session[SessionUserId] = user;
                        OnlineUserManager.Instance.TickUserId(user.Id);

                        //if (logFlag)
                        //{
                        //    LoginLog loginLog = new LoginLog();
                        //    loginLog.UserId = user.Id;
                        //    loginLog.IP = HttpContextUtil.RemoteIP;

                        //    dbSession.Insert(loginLog);
                        //}
                    }
                    else
                    {
                        user = null;
                    }
                }

                return user;
            }
        }
        
        public void AuthLogout()
        {
            AuthUser authUser = AuthManager.Instance.InnerGetCurrentUser();
            if (authUser != null)
            {
                OnlineUserManager.Instance.RemoveUserId(authUser.Id);
                Bee.Caching.CacheManager.Instance.RemoveCache<int>(UserAllPermission, authUser.Id);
            }
            HttpContextUtil.CurrentHttpContext.Session.Clear();
        }

        public List<AuthPermission> GetUserAllPermission(int userId)
        {
            return CacheManager.Instance.GetEntity<List<AuthPermission>, int>(UserAllPermission, userId, TimeSpan.FromMinutes(CacheMinute), pUserId =>
            {
                using (DbSession dbSession = new DbSession(AuthConnString))
                {
                    string sql = @"select d.*
                            from authuser a left join authuserrole b on a.id = b.userid
                            left join authaccess c on b.roleid = c.roleid 
                            left join authpermission d on c.permissionid = d.id
                            left join authrole e on b.roleid = e.id
                            where a.id = @userId and d.delflag = 0 and e.delflag = 0
                            order by d.dispindex";

                    BeeDataAdapter dataAdapter = new BeeDataAdapter();
                    dataAdapter.Add("userId", pUserId);
                    return dbSession.ExecuteCommand<AuthPermission>(sql, dataAdapter);
                }
            });
        }
        public List<AuthPermission> GetAllShownPermission()
        {
            return CacheManager.Instance.GetEntity<List<AuthPermission>, string>(AllShownPermission, string.Empty, TimeSpan.FromMinutes(CacheMinute), key =>
            {
                using (DbSession dbSession = new DbSession(AuthConnString))
                {
                    return dbSession.ExecuteCommand<AuthPermission>(
                        @"select d.*
                          from authpermission d where showflag=1 and delflag=0
                          order by dispindex asc", null);
                }
            });
        }

        

    }
}