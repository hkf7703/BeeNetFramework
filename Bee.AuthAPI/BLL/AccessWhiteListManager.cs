using Bee.Caching;
using Bee.Data;
using Bee.Util;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bee.AuthAPI.BLL
{
    public class AccessWhiteListManager
    {
        private static AccessWhiteListManager instance = new AccessWhiteListManager();

        private static readonly string AuthConnString = AuthManager.AuthConnString;

        private AccessWhiteListManager()
        {
        }

        public static AccessWhiteListManager Instance
        {
            get
            {
                return instance;
            }
        }

        public bool Check(string controllerName, string actionName)
        {
            DataTable table = CacheManager.Instance.GetEntity<DataTable>("AccessWhiteList", TimeSpan.FromHours(2), () =>
            {
                using (DbSession dbSession = new DbSession(AuthConnString))
                {
                    return dbSession.Query("beeaccesswl", null);
                }
            });


            bool result = false;
            DataRow[] rows = table.Select("controllername='{0}' and actionname='{1}'".FormatWith(controllerName, actionName));

            result = rows.Length > 0;
            if (!result)
            {
                rows = table.Select("controllername='{0}' and actionname='*'".FormatWith(controllerName));

                result = rows.Length > 0;
            }


            return rows.Length > 0;
        }
    }
}
