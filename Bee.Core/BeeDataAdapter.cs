using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Bee.Core;
using Bee.Util;
using System.Web;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace Bee
{
    /// <summary>
    /// 数据集
    /// </summary>
    public class BeeDataAdapter
    {
        private Dictionary<string, object> data;

        #region Constructors

        public BeeDataAdapter()
            : this(new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase))
        {
        }

        public BeeDataAdapter(Dictionary<string, object> data)
        {
            this.data = new Dictionary<string, object>(data, StringComparer.InvariantCultureIgnoreCase);
        }

        public BeeDataAdapter(BeeDataAdapter dataAdapter)
            :this()
        {
            if (dataAdapter != null)
            {
                foreach (string key in dataAdapter.Keys)
                {
                    Add(key, dataAdapter[key]);
                }
            }
        }

        #endregion

        #region Basic Operations

        public BeeDataAdapter Add(string key, object value)
        {
            if (!data.ContainsKey(key) && value != null)
            {
                this.data.Add(key, value);
            }

            return this;
        }

        public bool ContainsKey(string key)
        {
            return this.data.ContainsKey(key);
        }

        public void RemoveKey(string key)
        {
            this.data.Remove(key);
        }

        public void RemoveEmptyOrNull()
        {
            List<string> needRemovedKeyList = new List<string>();
            foreach (string item in data.Keys)
            {
                object value = data[item];
                if (value == null)
                {
                    needRemovedKeyList.Add(item);
                }

                string stringValue = value as string;
                if (stringValue != null && stringValue.Length == 0)
                {
                    needRemovedKeyList.Add(item);
                }
            }

            foreach (string item in needRemovedKeyList)
            {
                this.data.Remove(item);
            }
        }

        public void Merge(BeeDataAdapter dataAdapter, bool overrideFlag)
        {
            foreach (string key in dataAdapter.Keys)
            {
                if (overrideFlag)
                {
                    data[key] = dataAdapter[key];
                }
                else
                {
                    if (!data.ContainsKey(key))
                    {
                        data[key] = dataAdapter[key];
                    }
                }
            }
        }

        // 若是
        public T TryGetValue<T>(string key, T defaultValue, bool writeBackFlag)
        {
            T result = defaultValue;
            object value = this[key];
            if (value != null)
            {
                try
                {
                    object convertedObj = ConvertUtil.Convert(value, typeof(T));
                    if (convertedObj != null)
                    {
                        result = (T)convertedObj;
                    }
                }
                catch (Exception)
                {
                    // do nothing here.
                }
            }
            else
            {
                if (writeBackFlag)
                {
                    this[key] = defaultValue;
                }
            }

            return result;
        }

        public T TryGetValue<T>(string key, T defaultValue)
        {
            return TryGetValue<T>(key, defaultValue, false);
        }

        /// <summary>
        /// 为页面显示用， 统一以
        /// 日期以yyyy-MM-dd hh:mm为主
        /// 数字以0.00为主
        /// </summary>
        /// <param name="key">键值</param>
        /// <returns>返回格式化好后的</returns>
        public string Format(string key)
        {
            string result = string.Empty;
            object value = this[key];
            if (value != null)
            {
                result = DataUtil.Format(value);
            }

            return result;
        }

        /// <summary>
        /// 转化为Json字符串。 采用微软方法
        /// </summary>
        /// <returns></returns>
        public string ToSJson()
        {
            return SerializeUtil.ToMsJson(this.data);
        }

        public string FormatDateTime(string key, string format)
        {
            string result = string.Empty;
            if (this[key] is DateTime)
            {
                if (!string.IsNullOrEmpty(format))
                {
                    result = ((DateTime)this[key]).ToString(format);
                }
                else
                {
                    result = ((DateTime)this[key]).ToString(Constants.DateTimeFormat);
                }
            }

            return result;
        }

        #endregion

        #region Properties

        public static BeeDataAdapter New
        {
            get
            {
                return new BeeDataAdapter();
            }
        }

        public int Count
        {
            get
            {
                int result = 0;
                if (this.data != null)
                {
                    result = this.data.Count;
                }

                return result;
            }
        }

        public ICollection<string> Keys
        {
            get
            {
                ICollection<string> result = null;
                if (data != null)
                {
                    result = this.data.Keys.ToList();
                }

                return result;
            }
        }

        public object this[string key]
        {
            get
            {
                object result = null;
                if (this.data.ContainsKey(key))
                {
                    result = this.data[key];
                }
                return result;
            }
            set
            {
                if (this.data.ContainsKey(key))
                {
                    this.data[key] = value;
                }
                else
                {
                    this.data.Add(key, value);
                }
            }
        }

        #endregion

        #region From Methods

        /// <summary>
        /// 从DataRow构造一个DataAdapter
        /// </summary>
        /// <param name="dataRow">DataRow</param>
        /// <returns>DataAdapter实例</returns>
        public static BeeDataAdapter From(DataRow dataRow)
        {
            BeeDataAdapter dataAdapter = new BeeDataAdapter();

            if (dataRow != null)
            {
                foreach (DataColumn column in dataRow.Table.Columns)
                {
                    object dbValue = dataRow[column];
                    if (dbValue == DBNull.Value)
                    {
                        continue;
                    }
                    dataAdapter.Add(column.ColumnName, dataRow[column]);
                }
            }

            return dataAdapter;
        }

        /// <summary>
        /// 从一个对象实例中构造一个DataAdapter.
        /// 若是String的话， 则采用简单Json构造。
        /// 符合正则： "(?<name>.*?)":"(?<value>.*?)"
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="value">对象值</param>
        /// <returns>DataAdapter实例</returns>
        public static BeeDataAdapter From<T>(T value) where T : class
        {
            BeeDataAdapter dataAdapter = new BeeDataAdapter();
            if (value != null)
            {
                if (value is string)
                {
                    string temp = value.ToString();
                    if (!string.IsNullOrEmpty(temp))
                    {
                        Dictionary<string, object> dict = SerializeUtil.FromMsJson<Dictionary<string, object>>(temp);
                        foreach (string item in dict.Keys)
                        {
                            dataAdapter[item] = dict[item];
                        }
                    }
                }
                else
                {

                    IEntityProxy entityProxy = EntityProxyManager.Instance.GetEntityProxyFromType(value.GetType());
                    foreach (PropertySchema item in entityProxy.GetPropertyList())
                    {
                        object propertyValue =
                            entityProxy.GetPropertyValue(value, item.Name);
                        if (ReflectionUtil.IsNullableType(item.PropertyType) && propertyValue == null)
                        {
                            continue;
                        }

                        if (propertyValue is Enum)
                        {
                            propertyValue = ((Enum)propertyValue).ToString("D");
                        }

                        if (item.PropertyType.UnderlyingSystemType == typeof(DateTime))
                        {
                            if (string.Compare("modifytime", item.Name, true) == 0
                                || string.Compare("updatetime", item.Name, true) == 0)
                            {
                                propertyValue = DateTime.Now;
                            }

                            if ((DateTime)propertyValue == DateTime.MinValue)
                            {
                                if (string.Compare("createtime", item.Name, true) == 0)
                                {
                                    propertyValue = DateTime.Now;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        }

                        dataAdapter.Add(item.Name, propertyValue);
                    }
                }
            }

            return dataAdapter;
        }

        #endregion

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("\r\n{0, 10}------->{1}\r\n", "Key", "Value");
            foreach (string key in Keys)
            {
                builder.AppendFormat("{0, 10}------->{1}\r\n", key, this[key]);
            }

            return builder.ToString();
        }
    }
}
