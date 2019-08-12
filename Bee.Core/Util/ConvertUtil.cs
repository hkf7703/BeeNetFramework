using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.ComponentModel;
using System.Data;
using Bee.Core;
using Bee.Data;
using Newtonsoft.Json.Linq;

namespace Bee.Util
{
    /// <summary>
    /// Util for Converting.
    /// </summary>
    public static class ConvertUtil
    {
        /// <summary>
        /// Provided for common converting between two type.
        /// </summary>
        /// <typeparam name="T">the type of the result.</typeparam>
        /// <param name="initialValue">the initial value.</param>
        /// <returns>the instance of the result.</returns>
        public static T CommonConvert<T>(object initialValue)
        {
            object result = Convert(initialValue, typeof(T));

            return (T)result;
        }

        /// <summary>
        /// Provided for common clone for the object. 
        /// the object should be a class.
        /// </summary>
        /// <typeparam name="T">the type of the object</typeparam>
        /// <param name="value">the instance of the type.</param>
        /// <returns>the cloned instance.</returns>
        public static T CommonClone<T>(object value) where T : class
        {
            ThrowExceptionUtil.ArgumentNotNull(value, "value");
            BeeDataAdapter dataAdapter = BeeDataAdapter.From(value);

            return ConvertDataToObject<T>(dataAdapter);
        }

        /// <summary>
        /// Check can be converted from initial type to the target type.
        /// </summary>
        /// <param name="initialType">the initial type.</param>
        /// <param name="targetType">the target type.</param>
        /// <returns>true, if can convert; or false.</returns>
        public static bool CanConvertType(Type initialType, Type targetType)
        {
            ThrowExceptionUtil.ArgumentNotNull(initialType, "initialType");
            ThrowExceptionUtil.ArgumentNotNull(targetType, "targetType");
            if (ReflectionUtil.IsNullableType(targetType))
            {
                targetType = Nullable.GetUnderlyingType(targetType);
            }
            if (targetType == initialType)
            {
                return true;
            }
            if (typeof(IConvertible).IsAssignableFrom(initialType) && typeof(IConvertible).IsAssignableFrom(targetType))
            {
                return true;
            }
            if ((initialType == typeof(DateTime)) && (targetType == typeof(DateTimeOffset)))
            {
                return true;
            }
            if ((initialType == typeof(Guid)) && ((targetType == typeof(Guid)) || (targetType == typeof(string))))
            {
                return true;
            }
            if ((initialType == typeof(Type)) && (targetType == typeof(string)))
            {
                return true;
            }
            TypeConverter converter = TypeDescriptor.GetConverter(initialType);
            if ((((converter != null) && !(converter is ComponentConverter))
                && converter.CanConvertTo(targetType)) && ((converter.GetType() != typeof(TypeConverter))))
            {
                return true;
            }
            TypeConverter converter2 = TypeDescriptor.GetConverter(targetType);
            return ((((converter2 != null) && !(converter2 is ComponentConverter))
                && converter2.CanConvertFrom(initialType)) || ((initialType == typeof(DBNull))
                && ReflectionUtil.IsNullableType(targetType)));
        }

        /// <summary>
        /// Convert the initial value to the target type. 
        /// </summary>
        /// <param name="initialValue">the initial value.</param>
        /// <param name="targetType">the target type.</param>
        /// <returns>the result that is target type.</returns>
        /// <exception cref="ArgumentException">
        /// when the parameter of the initial value is null.
        /// or when parameter of the target type is null.
        /// or when the target type is interface or abstract, or generic type.
        /// </exception>
        public static object Convert(object initialValue, Type targetType)
        {
            object result = null;
            try
            {
                result = Convert(initialValue, CultureInfo.InvariantCulture, targetType);
            }
            catch (Exception e)
            {
                ThrowExceptionUtil.ThrowMessageException("Convert error！initialValue:{0}, targetType:{1}, Exception:{2}".FormatWith(initialValue, targetType, e));
            }

            return result;
        }

        public static object Convert(object initialValue, CultureInfo culture, Type targetType)
        {
            //return initialValue;

            //ThrowExceptionUtil.ArgumentNotNull(initialValue, "initialValue");
            ThrowExceptionUtil.ArgumentNotNull(targetType, "targetType");

            if (initialValue == null)
            {
                if (targetType.IsValueType)
                {
                    return ReflectionUtil.CreateInstance(targetType);
                }
                else
                {
                    return initialValue;
                }
            }
            else
            {
                string tempValue = initialValue as string;
                if (tempValue != null && tempValue.Length == 0)
                {
                    // if the initial value is string, and the string is empty.
                    if (targetType.IsValueType)
                    {
                        return ReflectionUtil.CreateInstance(targetType);
                    }
                }
            }

            if (initialValue == DBNull.Value)
            {
                if (targetType.IsValueType)
                {
                    return ReflectionUtil.CreateInstance(targetType);
                }
                else
                {
                    return null;
                }
            }
            Type t = initialValue.GetType();

            if (targetType == t || targetType.IsAssignableFrom(t))
            {
                return initialValue;
            }

            if (targetType.IsGenericType && (targetType.GetGenericTypeDefinition() == typeof(Nullable<>)))
            {
                targetType = Nullable.GetUnderlyingType(targetType);
            }

            if (targetType == t || targetType.IsAssignableFrom(t))
            {
                return initialValue;
            }

            if ((initialValue is string) && typeof(Type).IsAssignableFrom(targetType))
            {
                return Type.GetType((string)initialValue, true);
            }

            if ((targetType.IsInterface || targetType.IsGenericTypeDefinition) || targetType.IsAbstract)
            {
                throw new ArgumentException("Target type {0} is not a value type or a non-abstract class.".FormatWith(targetType), "targetType");
            }

            if ((initialValue is IConvertible) && typeof(IConvertible).IsAssignableFrom(targetType))
            {
                if (targetType.IsEnum)
                {
                    return Enum.Parse(targetType, initialValue.ToString(), true);
                }
                return System.Convert.ChangeType(initialValue, targetType, culture);
            }

            if ((initialValue is string) && (targetType == typeof(Guid)))
            {
                return new Guid((string)initialValue);
            }

            TypeConverter converter = TypeDescriptor.GetConverter(t);
            if ((converter != null) && converter.CanConvertTo(targetType))
            {
                return converter.ConvertTo(null, culture, initialValue, targetType);
            }
            TypeConverter converter2 = TypeDescriptor.GetConverter(targetType);
            if ((converter2 != null) && converter2.CanConvertFrom(t))
            {
                return converter2.ConvertFrom(null, culture, initialValue);
            }
            return null;
        }

        private static bool IsInteger(object value)
        {
            switch (System.Convert.GetTypeCode(value))
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Decimal:
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Convert a <typeparamref name="BeeDataAdapter"/> to a instance of the target type.
        /// </summary>
        /// <param name="type">the target type.</param>
        /// <param name="dataAdapter">the instance of a <typeparamref name="BeeDataAdapter"/>.</param>
        /// <returns>the instance of the target type.</returns>
        public static object ConvertDataToObject(Type type, BeeDataAdapter dataAdapter)
        {
            return ConvertDataToObject(type, dataAdapter, null);
        }

        /// <summary>
        /// 转换辅助类。 mapping暂未使用
        /// </summary>
        /// <param name="type"></param>
        /// <param name="dataAdapter"></param>
        /// <param name="mapping"></param>
        /// <returns></returns>
        private static object ConvertDataToObject(Type type, BeeDataAdapter dataAdapter, Dictionary<string, string> mapping)
        {
            ThrowExceptionUtil.ArgumentNotNull(type, "type");
            ThrowExceptionUtil.ArgumentNotNull(dataAdapter, "dataAdapter");

            object result = ReflectionUtil.CreateInstance(type);
            if (result != null)
            {
                IEntityProxy entityProxy = EntityProxyManager.Instance.GetEntityProxyFromType(type);

                foreach (string key in dataAdapter.Keys)
                {
                    string value = dataAdapter[key] as string;
                    if (value != null && value.Length == 0)
                    {
                        /// 空字符串并且目标类型不是字符型则不转换
                        PropertySchema schema = entityProxy[key];
                        if (schema != null && schema.PropertyType != typeof(string))
                        {
                            continue;
                        }
                    }

                    entityProxy.SetPropertyValue(result, key, dataAdapter[key]);
                }
            }

            return result;
        }

        /// <summary>
        /// Convert a <typeparamref name="BeeDataAdapter"/> to a instance of the target type.
        /// </summary>
        /// <typeparam name="T">the target type.</typeparam>
        /// <param name="dataAdapter">the instance of a <typeparamref name="BeeDataAdapter"/>.</param>
        /// <returns>the instance of the target type.</returns>
        public static T ConvertDataToObject<T>(BeeDataAdapter dataAdapter)
            where T : class
        {
            return ConvertDataToObject(typeof(T), dataAdapter) as T;
        }

        /// <summary>
        /// Convert a instance of DataRow to a instance of the target type.
        /// If the target type is value type, return the result using the value of the first column.
        /// This method can use the attribute <typeparamref name="OrmColumnAttribute"/> to get the mapping of the column and the property.
        /// </summary>
        /// <typeparam name="T">the target type.</typeparam>
        /// <param name="row">the instance of DataRow.</param>
        /// <returns>a instance of the target type.</returns>
        public static T ConvertDataToObject<T>(DataRow row)
        {
            object result = null;
            Type type = typeof(T);
            if (Type.GetTypeCode(type) == TypeCode.Object)
            {
                IEntityProxy entityProxy = EntityProxyManager.Instance.GetEntityProxyFromType(type);
                Dictionary<string, string> mapping = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
                foreach (PropertySchema item in entityProxy.GetPropertyList())
                {
                    OrmColumnAttribute ormColumnAttribute = item.GetCustomerAttribute<OrmColumnAttribute>();
                    if (ormColumnAttribute != null && !string.IsNullOrEmpty(ormColumnAttribute.DbColumnName))
                    {
                        mapping.Add(ormColumnAttribute.DbColumnName, item.Name);
                    }
                }
                result = ConvertDataToObject<T>(row, mapping);
            }
            else
            {
                result = Convert(row[0], type);
            }

            return (T)result;
        }

        /// <summary>
        ///  Convert a instance of DataRow to a instance of the target type using the mapping. 
        ///  using the column name of the DataRow since the mapping is null or empty.
        /// </summary>
        /// <typeparam name="T">the target type.</typeparam>
        /// <param name="row">the instance of DataRow.</param>
        /// <param name="mapping">The mapping for the column name and the property name.</param>
        /// <returns>the instance of the target type.</returns>
        public static T ConvertDataToObject<T>(DataRow row, Dictionary<string, string> mapping)
        {
            object result = null;
            Type type = typeof(T);
            if (Type.GetTypeCode(type) == TypeCode.Object)
            {
                IEntityProxy entityProxy = EntityProxyManager.Instance.GetEntityProxyFromType(type);
                result = ReflectionUtil.CreateInstance(type);
                if (mapping == null || mapping.Count == 0)
                {
                    foreach (DataColumn column in row.Table.Columns)
                    {
                        entityProxy.SetPropertyValue(result, column.ColumnName, row[column]);
                    }
                }
                else
                {
                    foreach (DataColumn column in row.Table.Columns)
                    {
                        string columnName = column.ColumnName;
                        if (mapping.ContainsKey(columnName))
                        {
                            entityProxy.SetPropertyValue(result, mapping[columnName], row[column]);
                        }
                        else
                        {
                            entityProxy.SetPropertyValue(result, columnName, row[column]);
                        }
                    }
                }
            }

            return (T)result;
        }

        public static List<T> ConvertDataToObject<T>(DataTable dataTable)
        {
            List<T> list = new List<T>();
            Type type = typeof(T);

            if (Type.GetTypeCode(type) == TypeCode.Object)
            {
                IEntityProxy entityProxy = EntityProxyManager.Instance.GetEntityProxyFromType(typeof(T));

                Dictionary<string, string> mapping = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
                foreach (PropertySchema item in entityProxy.GetPropertyList())
                {
                    OrmColumnAttribute ormColumnAttribute = item.GetCustomerAttribute<OrmColumnAttribute>();
                    if (ormColumnAttribute != null && !string.IsNullOrEmpty(ormColumnAttribute.DbColumnName))
                    {
                        mapping.Add(ormColumnAttribute.DbColumnName, item.Name);
                    }
                }

                list = ConvertDataToObject<T>(dataTable, mapping);
            }
            else
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    list.Add((T)Convert(row[0], type));
                }
            }

            return list;
        }

        public static List<T> ConvertDataToObject<T>(DataTable dataTable, Dictionary<string, string> mapping)
        {
            List<T> list = new List<T>(dataTable.Rows.Count);
            IEntityProxy entityProxy = EntityProxyManager.Instance.GetEntityProxyFromType(typeof(T));
            if (mapping == null || mapping.Count == 0)
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    T obj = (T)entityProxy.CreateInstance();

                    foreach (DataColumn column in row.Table.Columns)
                    {
                        entityProxy.SetPropertyValue(obj, column.ColumnName, row[column]);
                    }

                    list.Add(obj);
                }
            }
            else
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    T obj = (T)entityProxy.CreateInstance();

                    foreach (DataColumn column in row.Table.Columns)
                    {
                        string columnName = column.ColumnName;
                        if (mapping.ContainsKey(columnName))
                        {
                            entityProxy.SetPropertyValue(obj, mapping[columnName], row[column]);
                        }
                        else
                        {
                            entityProxy.SetPropertyValue(obj, columnName, row[column]);
                        }
                    }

                    list.Add(obj);
                }
            }

            return list;
        }


        public static List<T> ConvertJArrayToObject<T>(JArray jArray)
        {
            List<T> result = new List<T>();

            if(jArray != null)
            {
                foreach(var item in jArray)
                {

                }
            }

            return result;
        }
    }
}
