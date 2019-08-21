using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using Bee.Core;
using System.Globalization;
using System.IO;
using System.Data;
using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Bee.Util
{
    [AttributeUsage(AttributeTargets.Property)]
    public class BeeJsonAttribute : Attribute
    {
        public bool IgnoreFlag { get; set; }
    }

    public class UnderlineSplitContractResolver : DefaultContractResolver
    {
        protected override string ResolvePropertyName(string propertyName)
        {
            return CamelCaseToUnderlineSplit(propertyName);
        }

        private string CamelCaseToUnderlineSplit(string name)
        {
            return name.ToLower();
        }
    }
    /// <summary>
    /// The Util for serialization.
    /// </summary>
    public static class SerializeUtil
    {
        private static Regex simpleRegex = new Regex(@"(?<name>.*?):{(?<value>.*?)};");

        internal static JsonSerializerSettings DefaultJsonSetting = new JsonSerializerSettings();

        static SerializeUtil()
        {
            DefaultJsonSetting.NullValueHandling = NullValueHandling.Ignore;
            DefaultJsonSetting.DateFormatHandling = Newtonsoft.Json.DateFormatHandling.MicrosoftDateFormat;
            DefaultJsonSetting.DateFormatString = "yyyy-MM-dd HH:mm:ss";
            DefaultJsonSetting.ContractResolver = new UnderlineSplitContractResolver();

            DefaultJsonSetting.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        }
        

        internal static T SimpleDeserialize<T>(string str)
            where T : class
        {
            T result = ReflectionUtil.CreateInstance<T>();
            foreach (Match match in simpleRegex.Matches(str))
            {
                string name = match.Groups["name"].Value;
                string value = match.Groups["value"].Value;

                SetValue<T>(result, name, value);
            }

            return result;
        }

        internal static string SimpleSerialize(object value)
        {
            ThrowExceptionUtil.ArgumentNotNull(value, "value");

            StringBuilder stringBuilder = new StringBuilder();

            IEntityProxy proxy = EntityProxyManager.Instance.GetEntityProxyFromType(value.GetType());
            foreach (PropertySchema propertyStruct in proxy.GetPropertyList())
            {
                string name = propertyStruct.Name;
                Type type = propertyStruct.PropertyType;


                object propertyValue = proxy.GetPropertyValue(value, name);
                if (propertyValue == null)
                {
                    continue;
                }

                stringBuilder.AppendFormat("{0}:{{", name);

                if (Type.GetTypeCode(type) == TypeCode.Object)
                {
                    if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(IList<>)
                        || type.GetGenericTypeDefinition() == typeof(List<>)))
                    {
                        IList list = propertyValue as IList;
                        for (int i = 0; i < list.Count; i++)
                        {
                            stringBuilder.AppendFormat("{0},", ConvertUtil.CommonConvert<string>(list[i]));

                            if (i == list.Count)
                            {
                                stringBuilder.Remove(stringBuilder.Length - 1, 1);
                            }
                        }
                    }
                    else if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(IDictionary<,>)
                        || type.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
                    {
                        IDictionary dict = propertyValue as IDictionary;
                        foreach (DictionaryEntry item in dict)
                        {
                            stringBuilder.AppendFormat("{{{0}:{1}}}",
                                ConvertUtil.CommonConvert<string>(item.Key), ConvertUtil.CommonConvert<string>(item.Value));
                        }
                    }
                    else if (type == typeof(string))
                    {
                        stringBuilder.Append(propertyValue.ToString());
                    }
                    else if (type == typeof(byte[]))
                    {

                    }
                    else if (ConvertUtil.CanConvertType(type, typeof(string))
                        || type.IsValueType)
                    {
                        stringBuilder.Append(ConvertUtil.CommonConvert<string>(propertyValue));
                    }
                    else
                    {

                        //IEntityProxy entityProxy = EntityProxyManager.Instance.GetEntityProxyFromType(type);
                        stringBuilder.Append(SimpleSerialize(proxy.GetPropertyValue(value, name)));

                        //stringBuilder.Append(proxy.GetPropertyValue(value, name).ToString());
                    }
                }
                else
                {
                    stringBuilder.Append(propertyValue.ToString());
                }
                stringBuilder.Append("};");
            }

            return stringBuilder.ToString();
        }

        private static void SetValue<T>(T target, string name, string value)
            where T : class
        {
            EntityProxy<T> proxy = EntityProxyManager.Instance.GetEntityProxy<T>();
            PropertySchema propertyStruct = proxy[name];
            if (propertyStruct != null)
            {
                Type type = propertyStruct.PropertyType;
                if (Type.GetTypeCode(type) == TypeCode.Object)
                {
                    if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(IList<>)
                        || type.GetGenericTypeDefinition() == typeof(List<>)))
                    {
                        Type type2 = type.GetGenericArguments()[0];
                        IList list = proxy.GetPropertyValue(target, name) as IList;
                        if (list == null)
                        {
                            Type type3 = typeof(List<>).MakeGenericType(new Type[]
						    {
							    type2
						    });
                            list = ReflectionUtil.CreateInstance(type3) as IList;
                            proxy.SetPropertyValue(target, name, list);
                        }
                        if (list != null)
                        {
                            foreach (string item in value.Split(','))
                            {
                                list.Add(ConvertUtil.Convert(item, type2));
                            }
                        }
                    }
                    else if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(IDictionary<,>)
                    || type.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
                    {
                        Type type4 = type.GetGenericArguments()[0];
                        Type type5 = type.GetGenericArguments()[1];
                        IDictionary dictionary = proxy.GetPropertyValue(target, name) as IDictionary;

                        if (dictionary == null)
                        {
                            Type type3 = typeof(List<>).MakeGenericType(new Type[]
						    {
							    type4, type5
						    });
                            dictionary = ReflectionUtil.CreateInstance(type3) as IDictionary;
                            proxy.SetPropertyValue(target, name, dictionary);
                        }

                        if (dictionary != null)
                        {
                            foreach (string item in value.Split(':'))
                            {
                                string[] itemArray = item.Split(':');
                                if (itemArray.Length == 2)
                                {
                                    dictionary.Add(ConvertUtil.Convert(itemArray[0], type4), ConvertUtil.Convert(itemArray[1], type5));
                                }
                            }
                        }

                    }
                    else if (type.IsValueType)
                    {
                        object objectValue = ConvertUtil.Convert(value, type);
                        if (objectValue != null)
                        {
                            proxy.SetPropertyValue(target, name, value);
                        }
                    }
                    else
                    {
                        proxy.SetPropertyValue(target, name, value);
                    }
                }
                else
                {
                    proxy.SetPropertyValue(target, name, value);
                }

            }
        }

        public static string ToXml(object value)
        {
            ThrowExceptionUtil.ArgumentNotNull(value, "value");

            Type type = value.GetType();
            XmlSerializer serializer = new XmlSerializer(type);
            using (MemoryStream stream = new MemoryStream())
            {
                serializer.Serialize((Stream)stream, value);
                byte[] bytes = stream.ToArray();
                return Encoding.UTF8.GetString(bytes);
            }
        }

        public static T FromXml<T>(string xml)
        {
            Type type = typeof(T);
            XmlSerializer serializer = new XmlSerializer(type);
            using (MemoryStream stream = new MemoryStream())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(xml);
                stream.Write(bytes, 0, bytes.Length);
                stream.Position = 0L;
                return (T)serializer.Deserialize(stream);
            }
        }

        public static string ToMsJson(object value)
        {
            string jsonString = (new JavaScriptSerializer()).Serialize(value);

            //替换Json的Date字符串  
            string p = @"\\/Date\((\d+)\)\\/";
            MatchEvaluator matchEvaluator = new MatchEvaluator(ConvertJsonDateToDateString);
            Regex reg = new Regex(p);
            jsonString = reg.Replace(jsonString, matchEvaluator);
            return jsonString;  
        }

        public static T FromMsJson<T>(string json)
        {
            string p = @"/d{4}-/d{2}-/d{2}\s/d{2}:/d{2}:/d{2}";
            MatchEvaluator matchEvaluator = new MatchEvaluator(ConvertDateStringToJsonDate);
            Regex reg = new Regex(p);
            json = reg.Replace(json, matchEvaluator);  

            return (new JavaScriptSerializer()).Deserialize<T>(json);
        }

        /// <summary>  
        /// 将Json序列化的时间由/Date(1294499956278+0800)转为字符串  
        /// </summary>  
        private static string ConvertJsonDateToDateString(Match m)
        {
            string result = string.Empty;
            DateTime dt = new DateTime(1970, 1, 1);
            dt = dt.AddMilliseconds(long.Parse(m.Groups[1].Value));
            dt = dt.ToLocalTime();
            result = dt.ToString("yyyy-MM-dd HH:mm:ss");
            return result;
        }
        /// <summary>  
        /// 将时间字符串转为Json时间  
        /// </summary>  
        private static string ConvertDateStringToJsonDate(Match m)
        {
            string result = string.Empty;
            DateTime dt = DateTime.Parse(m.Groups[0].Value);
            dt = dt.ToUniversalTime();
            TimeSpan ts = dt - DateTime.Parse("1970-01-01");
            result = string.Format(@"\/Date({0}+0800)\/", ts.TotalMilliseconds);
            return result;
        }  

        /// <summary>
        /// 简单Json的解析。
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        //public static BeeDataAdapter FromJson(string json)
        //{
        //    BeeDataAdapter result = new BeeDataAdapter();

        //    foreach (Match match in simpleRegex.Matches(json))
        //    {
        //        string name = match.Groups["name"].Value;
        //        string value = match.Groups["value"].Value;

        //        result.Add(name, value);
        //    }

        //    return result;
        //}

        /// <summary>
        /// Json the object.
        /// </summary>
        /// <param name="value">the instance.</param>
        /// <returns>the json string.</returns>
        public static string ToJson(object value)
        {
            ThrowExceptionUtil.ArgumentNotNull(value, "value");

            StringBuilder stringBuilder = new StringBuilder();

            BeeDataAdapter dataAdapter = value as BeeDataAdapter;
            if (dataAdapter != null)
            {
                BeeDataAdapter newDataAdapter = new BeeDataAdapter(dataAdapter);
                newDataAdapter.RemoveEmptyOrNull();

                if (newDataAdapter.Keys.Count > 0)
                {
                    stringBuilder.Append("{");
                    foreach (string item in newDataAdapter.Keys)
                    {
                        WriteJsonValue(stringBuilder, item, newDataAdapter[item]);
                    }
                    stringBuilder.Remove(stringBuilder.Length - 1, 1);
                    stringBuilder.Append("}");
                }
            }
            else if (value is IList)
            {
                WriteJsonValue(stringBuilder, value.GetType(), value);
            }
            else if (value is IDictionary)
            {
                WriteJsonValue(stringBuilder, value.GetType(), value);
            }
            else if (value is DataTable)
            {
                WriteJsonValue(stringBuilder, value.GetType(), value);
            }
            else
            {
                stringBuilder.Append("{ ");
                IEntityProxy proxy = EntityProxyManager.Instance.GetEntityProxyFromType(value.GetType());
                foreach (PropertySchema propertyStruct in proxy.GetPropertyList())
                {
                    string name = propertyStruct.Name;
                    Type type = propertyStruct.PropertyType;

                    BeeJsonAttribute beeJsonAttribute = propertyStruct.GetCustomerAttribute<BeeJsonAttribute>();
                    if (beeJsonAttribute != null && beeJsonAttribute.IgnoreFlag)
                    {
                        continue;
                    }

                    object propertyValue = proxy.GetPropertyValue(value, name);
                    if (propertyValue == null)
                    {
                        continue;
                    }
                    type = propertyValue.GetType();

                    WriteJsonValue(stringBuilder, name, type, propertyValue);

                }

                stringBuilder.Remove(stringBuilder.Length - 1, 1);
                stringBuilder.Append("}");
            }

            return stringBuilder.ToString();

        }

        private static void WriteJsonValue(StringBuilder stringBuilder, string name, object value)
        {
            WriteJsonValue(stringBuilder, name, value.GetType(), value);
        }

        private static void WriteJsonValue(StringBuilder stringBuilder, string name, Type type, object value)
        {
            name = name.ToLower();
            if (ReflectionUtil.IsNullableType(type))
            {
                type = Nullable.GetUnderlyingType(type);
            }

            if (value != null)
            {
                stringBuilder.AppendFormat(@"""{0}"":", name);

                WriteJsonValue(stringBuilder, type, value);

                stringBuilder.Append(",");
            }
        }

        private static void WriteJsonValue(StringBuilder stringBuilder, Type type, object value)
        {
            TypeCode typeCode = Type.GetTypeCode(type);

            if (typeCode == TypeCode.Object && type != typeof(Guid))
            {
                if (type.IsArray)
                {
                    Array array = value as Array;

                    if (array.Length > 0)
                    {
                        stringBuilder.Append(@"[");
                        foreach (object item in array)
                        {
                            WriteJsonValue(stringBuilder, item.GetType(), item);
                            stringBuilder.Append(",");
                        }

                        stringBuilder.Remove(stringBuilder.Length - 1, 1);
                        stringBuilder.Append("]");
                    }
                }
                else if (value is IDictionary)
                {
                    IDictionary dict = value as IDictionary;
                    stringBuilder.Append(@"{");
                    foreach (DictionaryEntry entry in dict)
                    {
                        WriteJsonValue(stringBuilder, entry.Key.ToString(), entry.Value);
                    }
                    stringBuilder.Remove(stringBuilder.Length - 1, 1);
                    stringBuilder.Append("}");
                }
                else if (value is IEnumerable)
                {
                    bool havaFlag = false;

                    stringBuilder.Append(@"[");

                    foreach (object item in (IEnumerable)value)
                    {
                        havaFlag = true;
                        if (item is DictionaryEntry)
                        {
                            DictionaryEntry dictEntry = (DictionaryEntry)item;
                            stringBuilder.AppendFormat(@"""{0}"":", dictEntry.Key.ToString());
                            WriteJsonValue(stringBuilder, item.GetType(), item);
                            stringBuilder.Append(",");
                        }
                        else
                        {
                            WriteJsonValue(stringBuilder, item.GetType(), item);
                            stringBuilder.Append(",");
                        }
                    }

                    stringBuilder.Remove(stringBuilder.Length - 1, 1);
                    if (havaFlag)
                    {
                        stringBuilder.Append("]");
                    }
                    else
                    {
                        stringBuilder.Append(@"""""");
                    }
                }
                else if (value is DataTable)
                {
                    DataTable data = value as DataTable;
                    if (data == null || data.Rows.Count == 0)
                    {
                        stringBuilder.Append(@"""""");
                        return;
                    }
                    stringBuilder.Append(@"[");
                    foreach (DataRow row in ((DataTable)value).Rows)
                    {
                        WriteJsonValue(stringBuilder, row.GetType(), row);
                        stringBuilder.Append(",");
                    }

                    stringBuilder.Remove(stringBuilder.Length - 1, 1);
                    stringBuilder.Append("]");
                }
                else if (value is DataRow)
                {
                    stringBuilder.Append(ToJson(BeeDataAdapter.From((DataRow)value)));
                }
                else if (type.IsValueType) // struct
                {
                }
                else
                {
                    stringBuilder.Append(ToJson(value));
                }
            }
            else if (typeCode == TypeCode.Char || typeCode == TypeCode.String || type == typeof(Guid))
            {
                string stringValue = value.ToString();
                stringValue = JavaScriptUtils.ToEscapedJavaScriptString(stringValue);
                stringBuilder.AppendFormat(@"""{0}""", stringValue);
            }
            else if (typeCode == TypeCode.DateTime)
            {
                DateTime dateTime = (DateTime)value;

                TimeSpan utcOffset = TimeZoneInfo.Local.GetUtcOffset(dateTime);
                string str = utcOffset.Hours.ToString("+00;-00", CultureInfo.InvariantCulture)
                    + utcOffset.Minutes.ToString("00;00", CultureInfo.InvariantCulture);


                long num = ((dateTime.ToUniversalTime().Ticks - Constants.InitialJavaScriptDateTime.Ticks) / 0x2710L);

                stringBuilder.AppendFormat(@"""\/Date({0}{1})\/""", num.ToString(CultureInfo.InvariantCulture), str);
            }
            else
            {
                if (type.IsEnum)
                {
                    Enum enumValue = (Enum)value;
                    stringBuilder.Append(enumValue.ToString("D"));
                }
                else if (typeCode == TypeCode.Boolean)
                {
                    stringBuilder.Append(value.ToString().ToLower());
                }
                else if (typeCode == TypeCode.Double || typeCode == TypeCode.Single)
                {
                    double doubleValue = 0.0f;
                    float floatValue = 0.0f;
                    if (typeCode == TypeCode.Single)
                    {
                        floatValue = (float)value;
                        doubleValue = (double)floatValue;
                    }
                    else
                    {
                        doubleValue = (double)value;
                    }

                    string text = doubleValue.ToString("R", CultureInfo.InvariantCulture);
                    if ((!double.IsNaN(doubleValue) && !double.IsInfinity(doubleValue))
                        && ((text.IndexOf('.') == -1) && (text.IndexOf('E') == -1)))
                    {
                        text = (text + ".0");
                    }


                    stringBuilder.Append(text);
                }
                else
                {
                    stringBuilder.Append(value);
                }
            }
        }
    }

    internal static class JavaScriptUtils
    {
        public static string ToEscapedJavaScriptString(string value)
        {
            int length = value.Length;
            using (StringWriter writer = new StringWriter())
            {
                WriteEscapedJavaScriptString(writer, value);
                return writer.ToString();
            }
        }

        public static void WriteEscapedJavaScriptChar(TextWriter writer, char c)
        {
            switch (c)
            {
                case '\'':
                    writer.Write("'");
                    return;

                case '\\':
                    writer.Write(@"\\");
                    return;

                case '\b':
                    writer.Write(@"\b");
                    return;

                case '\t':
                    writer.Write(@"\t");
                    return;

                case '\n':
                    writer.Write(@"\n");
                    return;

                case '\f':
                    writer.Write(@"\f");
                    return;

                case '\r':
                    writer.Write(@"\r");
                    return;

                case '"':
                    writer.Write("\\\"");
                    return;
            }
            writer.Write(c);
        }

        public static void WriteEscapedJavaScriptString(TextWriter writer, string value)
        {
            if (value != null)
            {
                for (int i = 0; i < value.Length; i++)
                {
                    WriteEscapedJavaScriptChar(writer, value[i]);
                }
            }
        }
    }


}
