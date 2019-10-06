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
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                var ch = name[i];
                if (char.IsUpper(ch) && i > 0)
                {
                    var prev = name[i - 1];
                    if (prev != '_')
                    {
                        if (char.IsUpper(prev))
                        {
                            if (i < name.Length - 1)
                            {
                                var next = name[i + 1];
                                if (char.IsLower(next))
                                {
                                    builder.Append('_');
                                }
                            }
                        }
                        else
                        {
                            builder.Append('_');
                        }
                    }
                }

                builder.Append(char.ToLower(ch));
            }

            return builder.ToString();
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
            DefaultJsonSetting.ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();//new UnderlineSplitContractResolver();

            DefaultJsonSetting.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        }

        public static T FromJson<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, DefaultJsonSetting);
        }
        
        /// <summary>
        /// Json the object.
        /// </summary>
        /// <param name="value">the instance.</param>
        /// <returns>the json string.</returns>
        public static string ToJson(object value)
        {
            //BeeMvcResult mvcResult = value as BeeMvcResult;
            //if (mvcResult != null)
            //{

            //    return JsonConvert.SerializeObject(mvcResult.InnerDict, SerializeUtil.DefaultJsonSetting);
            //}
            //else
            {
                return JsonConvert.SerializeObject(value, SerializeUtil.DefaultJsonSetting);
            }
        }
        
    }
    


}
