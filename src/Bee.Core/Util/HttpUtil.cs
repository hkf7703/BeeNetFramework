using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Net;
using Bee.Web;
using System.IO;
using System.Text.RegularExpressions;

namespace Bee.Util
{
    public static class HttpUtil
    {
        private static readonly HttpClient httpClient = new HttpClient(null, null, true);

        private static readonly object lockObject = new object();

        /// <summary>
        /// 使用Get方法获取字符串结果
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string HttpGet(string url, Encoding encoding)
        {
            lock (lockObject)
            {
                httpClient.Url = url;

                return httpClient.GetString(encoding);
            }
        }

        /// <summary>
        /// 使用Post方法获取字符串结果
        /// </summary>
        public static string HttpPost(string url, Dictionary<string, string> formData, Encoding encoding)
        {
            lock (lockObject)
            {
                httpClient.Url = url;
                foreach (string item in formData.Keys)
                {
                    httpClient.PostingData.Add(item, formData[item]);
                }

                return httpClient.GetString(encoding);
            }
        }

        /// <summary>
        /// 使用Post方法获取字符串结果
        /// </summary>
        public static string HttpPost(string url, Stream postStream, Encoding encoding)
        {
            lock (lockObject)
            {
                httpClient.Url = url;
                httpClient.PostStream = postStream;

                return httpClient.GetString(encoding);
            }
        }

        public static bool IsUrl(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return false;
            string pattern = @"^(http|https|ftp|rtsp|mms):(\/\/|\\\\)[A-Za-z0-9%\-_@]+\.[A-Za-z0-9%\-_@]+[A-Za-z0-9\.\/=\?%\-&_~`@:\+!;]*$";
            return Regex.IsMatch(str, pattern, RegexOptions.IgnoreCase);
        }

        public static bool IsEmail(this string str)
        {
            return Regex.IsMatch(str, @"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$");
        }
    }
}
