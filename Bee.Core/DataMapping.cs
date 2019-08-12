using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bee.Core;
using System.Collections;
using System.Data;
using Bee.Caching;
using Bee.Util;
using Bee.Data;

namespace Bee
{
    /// <summary>
    /// 数据对应表处理。
    /// 应用设计上很多都是对照的， 如国家对照表， 肯定存在一个国家Id， 国家名称， 洲信息。
    /// 数据中只有国家Id， 方便处理显示对应的信息。
    /// </summary>
    public class DataMapping
    {
        private static DataMapping instance = new DataMapping();

        private Dictionary<string, CallbackReturnHandler<object>> innerMapping
            = new Dictionary<string, CallbackReturnHandler<object>>(StringComparer.CurrentCultureIgnoreCase);

        private static readonly string DataMappingCacheNameFormat = "DataMapping_{0}";

        private DataMapping()
        {
        }

        public static DataMapping Instance
        {
            get
            {
                return instance;
            }
        }

        public void Register(string name, CallbackReturnHandler<object> callback)
        {
            if (!innerMapping.ContainsKey(name))
            {
                innerMapping.Add(name, callback);
            }
        }

        internal void Add(string name, object value)
        {
            if (!innerMapping.ContainsKey(name))
            {
                innerMapping.Add(name, null);

                string cacheName = DataMappingCacheNameFormat.FormatWith(name);

                value = ConvertToDataTable(value);

                Caching.CacheManager.Instance.AddEntity(cacheName, value, TimeSpan.MaxValue);
            }
        }

        public void Refresh(string mappingName)
        {
            string cacheName = DataMappingCacheNameFormat.FormatWith(mappingName);
            CacheManager.Instance.RemoveCache(cacheName);
        }

        public object GetMapping(string mapping)
        {
            return GetCachedMappingValue(mapping);
        }

        public string Mapping(string mappingName, string keyValue)
        {
            return Mapping(mappingName, keyValue, null, null);
        }

        public string Mapping(string mappingName, string keyValue, string valuePropertyName)
        {
            return Mapping(mappingName, keyValue, null, valuePropertyName);
        }

        public string MappingAll(string mappingName, string valuePropertyFormat)
        {
            return MappingAll(mappingName, valuePropertyFormat, null);
        }

        public string MappingAll(string mappingName, string valuePropertyFormat, SqlCriteria sqlCriteria)
        {
            StringBuilder builder = new StringBuilder();
            object cacheValue = GetCachedMappingValue(mappingName);
            if (cacheValue != null)
            {
                DataTable tableValue = cacheValue as DataTable;
                if (tableValue != null)
                {
                    if (sqlCriteria != null)
                    {
                        try
                        {
                            DataRow[] rows = tableValue.Select(sqlCriteria.FilterClause);
                            foreach (DataRow item in rows)
                            {
                                builder.AppendLine(valuePropertyFormat.RazorFormat(item));
                            }
                        }
                        catch (Exception)
                        {
                            // do nothing here.
                        }
                    }
                    else
                    {
                        foreach (DataRow item in tableValue.Rows)
                        {
                            builder.AppendLine(valuePropertyFormat.RazorFormat(item));
                        }
                    }
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// 返回数据对应表所对应key的值。
        /// </summary>
        /// <param name="mappingName">数据对应表名</param>
        /// <param name="keyValue">对应表值, 可以多个值， 以,分割</param>
        /// <param name="valuePropertyName"></param>
        /// <returns></returns>
        public string Mapping(string mappingName, string keyValue, string keyPropertyName, string valuePropertyName)
        {
            ThrowExceptionUtil.ArgumentNotNullOrEmpty(mappingName, "mappingName");

            List<string> list = new List<string>();
            string result = string.Empty;

            object cacheValue = GetCachedMappingValue(mappingName);
            if (cacheValue != null && !string.IsNullOrEmpty(keyValue))
            {
                string[] array = keyValue.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                DataTable tableValue = cacheValue as DataTable;
                if (tableValue != null)
                {
                    foreach (string itemValue in array)
                    {
                        result = GetDataTableValue(tableValue, itemValue, keyPropertyName, valuePropertyName);

                        list.Add(result);
                    }
                }
            }

            return string.Join(",", list.ToArray());
        }

        private object GetCachedMappingValue(string mappingName)
        {
            object cacheValue = null;
            if (innerMapping.ContainsKey(mappingName))
            {
                string cacheName = DataMappingCacheNameFormat.FormatWith(mappingName);

                cacheValue = CacheManager.Instance.GetEntity<object>(cacheName);

                if (cacheValue == null && innerMapping[mappingName] != null)
                {
                    cacheValue = innerMapping[mappingName]();

                    cacheValue = ConvertToDataTable(cacheValue);

                    Caching.CacheManager.Instance.AddEntity(cacheName, cacheValue, TimeSpan.FromHours(1));
                }
            }

            return cacheValue;
        }

        private DataTable ConvertToDataTable(object cacheValue)
        {
            DataTable table = new DataTable();

            if (cacheValue is DataTable)
            {
                table = cacheValue as DataTable;

                return table;
            }

            IDictionary dictValue = cacheValue as IDictionary;
            if (dictValue != null)
            {
                object firstValue = null;
                foreach (object item in dictValue.Values)
                {
                    firstValue = item;
                    break;
                }
                if (firstValue != null)
                {
                    if (Type.GetTypeCode(firstValue.GetType()) != TypeCode.Object)
                    {
                        table.Columns.Add("id");
                        table.Columns.Add("name");

                        foreach (object key in dictValue.Keys)
                        {
                            DataRow row = table.NewRow();
                            row["id"] = key;
                            row["name"] = dictValue[key];

                            table.Rows.Add(row);
                        }
                    }
                    else
                    {
                        table.Columns.Add("id");
                        IEntityProxy entityProxy = EntityProxyManager.Instance.GetEntityProxyFromType(firstValue.GetType());
                        foreach (PropertySchema item in entityProxy.GetPropertyList())
                        {
                            if (string.Compare(item.Name, "id", true) != 0)
                            {
                                table.Columns.Add(item.Name);
                            }
                        }

                        foreach (object key in dictValue.Keys)
                        {
                            DataRow row = table.NewRow();

                            row["id"] = key;
                            object itemValue = dictValue[key];
                            foreach (PropertySchema item in entityProxy.GetPropertyList())
                            {
                                row[item.Name] = entityProxy.GetPropertyValue(itemValue, item.Name);
                            }

                            table.Rows.Add(row);
                        }

                    }
                }
            }

            #region Convert the List<> data to datatable

            IList list = cacheValue as IList;
            if (list != null && list.Count > 0)
            {
                object firstValue = list[0];
                if (firstValue != null && Type.GetTypeCode(firstValue.GetType()) == TypeCode.Object)
                {
                    IEntityProxy entityProxy = EntityProxyManager.Instance.GetEntityProxyFromType(firstValue.GetType());

                    foreach (PropertySchema item in entityProxy.GetPropertyList())
                    {
                        table.Columns.Add(item.Name);
                    }

                    foreach (object key in list)
                    {
                        DataRow row = table.NewRow();

                        foreach (PropertySchema item in entityProxy.GetPropertyList())
                        {
                            row[item.Name] = entityProxy.GetPropertyValue(key, item.Name);
                        }

                        table.Rows.Add(row);
                    }
                }

            }

            #endregion


            return table;
        }

        private string GetDataTableValue(DataTable tableValue, string keyValue, string keyPropertyName, string valuePropertyName)
        {
            string result = string.Empty;

            try
            {

                if (string.IsNullOrEmpty(valuePropertyName))
                {
                    foreach (DataRow row in tableValue.Rows)
                    {
                        if (string.Compare(row[0].ToString(), keyValue, true) == 0)
                        {
                            result = row[1].ToString();
                            break;
                        }
                    }

                    // 通配符方案
                    //if (string.IsNullOrEmpty(result))
                    //{
                    //    DataRow[] rows = tableValue.Select(string.Format("[{0}]='{1}'", tableValue.Columns[0].ColumnName, "*"));
                    //    if (rows.Length > 0)
                    //    {
                    //        result = rows[0][1].ToString();
                    //    }
                    //}
                }
                else
                {
                    keyPropertyName = string.IsNullOrEmpty(keyPropertyName) ? "id" : keyPropertyName;

                    if (tableValue.Columns.Contains(keyPropertyName) && tableValue.Columns.Contains(valuePropertyName))
                    {

                        DataRow[] rows = tableValue.Select(string.Format("{0}='{1}'", keyPropertyName, keyValue));
                        if (rows.Length == 1)
                        {
                            if (string.IsNullOrEmpty(valuePropertyName))
                            {
                                result = rows[0][1].ToString();
                            }
                            else if (valuePropertyName.IndexOf(",") >= 0)
                            {
                                ///TODO 处理多个返回值
                            }
                            else
                            {
                                object resultValue = rows[0][valuePropertyName];
                                if (resultValue != null)
                                {
                                    result = resultValue.ToString();
                                }
                            }
                        }

                        // 通配符方案
                        //if (string.IsNullOrEmpty(result))
                        //{
                        //    rows = tableValue.Select(string.Format("[{0}]='{1}'", keyPropertyName, "*"));
                        //    if (rows.Length > 0)
                        //    {
                        //        if (string.IsNullOrEmpty(valuePropertyName))
                        //        {
                        //            result = rows[0][1].ToString();
                        //        }
                        //        else
                        //        {
                        //            object resultValue = rows[0][valuePropertyName];
                        //            if (resultValue != null)
                        //            {
                        //                result = resultValue.ToString();
                        //            }
                        //        }
                        //    }
                        //}

                    }
                }
            }
            catch (EvaluateException)
            {
                // do nothing.
            }


            return result;
        }
    }
}
