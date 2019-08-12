using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bee.Core;
using Bee.Caching;

namespace Bee.Data
{


    [AttributeUsage(AttributeTargets.Class)]
    public class OrmTableAttribute : Attribute
    {
        public string TableName;
        public bool CachedFlag;
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class OrmColumnAttribute : Attribute
    {
        public bool PrimaryKeyFlag;
        public bool AllowNullFlag;
        public string ForeignTableName;
        public string ForeignColumnName;
        public string DbColumnName;
    }

    public static class OrmUtil
    {
        /// <summary>
        /// 读取类的标示列
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <returns>主键列名</returns>
        public static string GetIdentityColumnName<T>()
            where T : class
        {
            string result = Constants.DefaultIdentityColumnName;

            EntityProxy<T> entityProxy = EntityProxyManager.Instance.GetEntityProxy<T>();
            foreach (PropertySchema schema in entityProxy.GetPropertyList())
            {
                OrmColumnAttribute ormColumnAttribute = schema.GetCustomerAttribute<OrmColumnAttribute>();
                if (ormColumnAttribute != null && ormColumnAttribute.PrimaryKeyFlag)
                {
                    result = schema.Name;
                }
            }

            return result;
        }

        public static string GetTableName<T>() where T : class
        {
            Type typeInfo = typeof(T);

            return GetTableName(typeInfo);
        }

        public static string GetTableName(Type typeInfo)
        {
            string typeName = typeInfo.FullName;
            return CacheManager.Instance.GetEntity<string, string>(Constants.BeeDataTableNameTypeCategory, typeName, TimeSpan.FromHours(4), (para) =>
            {
                IEntityProxy entityProxy = EntityProxyManager.Instance.GetEntityProxyFromType(typeInfo);

                OrmTableAttribute ormTableAttribute =
                    entityProxy.GetCustomerAttribute<OrmTableAttribute>();

                string tableName = typeInfo.Name;
                if (ormTableAttribute != null)
                {
                    if (!string.IsNullOrEmpty(ormTableAttribute.TableName))
                    {
                        tableName = ormTableAttribute.TableName;
                    }
                }

                return tableName;
            });
        }

        public static bool CheckCacheFlag<T>()
            where T : class
        {
            bool result = false;

            EntityProxy<T> entityProxy = EntityProxyManager.Instance.GetEntityProxy<T>();

            OrmTableAttribute ormTableAttribute =
                entityProxy.GetCustomerAttribute<OrmTableAttribute>();
            if (ormTableAttribute != null)
            {
                result = ormTableAttribute.CachedFlag;
            }

            return result;
        }



    }
}
