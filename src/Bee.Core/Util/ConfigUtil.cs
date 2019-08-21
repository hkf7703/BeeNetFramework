using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Bee.Util
{
    /// <summary>
    /// The Util for getting the value of the configuration.
    /// </summary>
    public static class ConfigUtil
    {
        /// <summary>
        /// Getting the AppSetting value in the provided type.
        /// </summary>
        /// <typeparam name="T">the provided type.</typeparam>
        /// <param name="key">the key of the appsetting.</param>
        /// <returns>the value of the configuration in the provided type.</returns>
        public static T GetAppSettingValue<T>(string key)
        {
            return GetAppSettingValue(key, default(T));
        }

        /// <summary>
        /// Getting the AppSetting value in the provided type.
        /// </summary>
        /// <typeparam name="T">the provided type.</typeparam>
        /// <param name="key">the key of the appsetting.</param>
        /// <param name="defaultValue">the default value</param>
        /// <returns>the value of the configuration in the provided type.</returns>
        public static T GetAppSettingValue<T>(string key, T defaultValue)
        {
            ThrowExceptionUtil.ArgumentNotNull(key, "key");

            string value = ConfigurationManager.AppSettings[key];
            if (!string.IsNullOrEmpty(value))
            {
                return ConvertUtil.CommonConvert<T>(value);
            }

            return defaultValue;
        }

        /// <summary>
        /// Getting the AppSetting value in list of the provided type.
        /// </summary>
        /// <typeparam name="T">the provided typ.</typeparam>
        /// <param name="key">the key of the appsetting.</param>
        /// <param name="splitChar">the split char.</param>
        /// <returns>the list of the provided type.</returns>
        public static List<T> GetAppSettingValue<T>(string key, char splitChar)
        {
            ThrowExceptionUtil.ArgumentNotNull(key, "key");

            string value = ConfigurationManager.AppSettings[key];

            return (from item in value.Split(new char[] { splitChar })
             select ConvertUtil.CommonConvert<T>(item)).ToList();
        }

        /// <summary>
        /// Get the vlaue of the ConnectionStringSetting.
        /// </summary>
        /// <param name="key">the key of the ConnectionStrings.</param>
        /// <returns>The ConnectionStringSetting value.</returns>
        public static ConnectionStringSettings GetConnectionString(string key)
        {
            ThrowExceptionUtil.ArgumentNotNull(key, "key");

            return ConfigurationManager.ConnectionStrings[key];
        }

        internal static ConnectionStringSettings InnerGetConnectionString(string key)
        {
            ConnectionStringSettings result = GetConnectionString(key);

            if (result == null)
            {
                foreach (ConnectionStringSettings item in ConfigurationManager.ConnectionStrings)
                {
                    if (item.Name.StartsWith(key))
                    {
                        result = item;
                        break;
                    }
                }
            }

            return result;
        }
    }
}
