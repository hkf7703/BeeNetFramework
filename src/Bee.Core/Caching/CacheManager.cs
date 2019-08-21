using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Collections;

namespace Bee.Caching
{
    public class CacheManager
    {
        private static CacheManager instance = new CacheManager();

        private CacheManager()
        {
        }

        public static CacheManager Instance
        {
            get
            {
                return instance;
            }
        }

        public T GetEntity<T>(string name)
            where T : class
        {
            name = name.ToLower();
            return HttpRuntime.Cache.Get(name) as T;
        }

        public void SetEntity<T>(string name, T value)
        {
            name = name.ToLower();

            HttpRuntime.Cache[name] = value;
        }

        public void RemoveCache(string name)
        {
            name = name.ToLower();
            HttpRuntime.Cache.Remove(name);
        }

        public void RemoveCache<P>(string category, P para)
        {
            string name = string.Format("{0}_{1}", category, para.ToString()).ToLower();
            RemoveCache(name);
        }

        public void RemoveCategoryCache(string category)
        {
            category = (category + "_").ToLower();

            List<string> keyList = new List<string>();
            foreach (DictionaryEntry item in HttpRuntime.Cache)
            {
                if (item.Key.ToString().StartsWith(category))
                {
                    keyList.Add(item.Key.ToString());
                }
            }

            foreach (string item in keyList)
            {
                HttpRuntime.Cache.Remove(item);
            }

            //HttpRuntime.Cache.Remove(category.ToLower());
        }

        public void AddEntity<T>(string name, T value, TimeSpan durationTime)
        {
            name = name.ToLower();
            DateTime absoluteTime = DateTime.MaxValue;
            if (durationTime != TimeSpan.MaxValue)
            {
                absoluteTime = DateTime.UtcNow.Add(durationTime);
            }
            HttpRuntime.Cache.Insert(name, value, null, absoluteTime, TimeSpan.Zero);
        }

        public T GetEntity<T>(string name, TimeSpan durationTime, CallbackReturnHandler<T> handler)
                        where T : class
        {
            name = name.ToLower();

            T result = HttpRuntime.Cache[name] as T;

            if (result == null)
            {
                result = handler();

                if (result != null)
                {
                    DateTime absoluteTime = DateTime.MaxValue;
                    if (durationTime != TimeSpan.MaxValue)
                    {
                        absoluteTime = DateTime.Now.Add(durationTime);
                    }
                    HttpRuntime.Cache.Insert(name, result, null, absoluteTime, TimeSpan.Zero);
                }
            }

            return result;
        }

        public T GetEntity<T, P>(string category, P para, TimeSpan durationTime, CallbackReturnHandler<P, T> handler)
            where T : class
        {

            //Dictionary<string, T> cacheDict = HttpRuntime.Cache[category] as Dictionary<string, T>;

            //string key = para.ToString().ToLower();

            //if (cacheDict == null)
            //{
            //    cacheDict = new Dictionary<string, T>();

            //    DateTime absoluteTime = DateTime.MaxValue;
            //    if (durationTime != TimeSpan.MaxValue)
            //    {
            //        absoluteTime = DateTime.Now.Add(durationTime);
            //    }

            //    HttpRuntime.Cache.Insert(category, cacheDict, null, absoluteTime, TimeSpan.Zero);
            //}

            //T result = null;
            //if (cacheDict.ContainsKey(key))
            //{
            //    result = cacheDict[key];
            //}
            //else
            //{
            //     result = handler(para);

            //     if (result != null)
            //     {
            //         cacheDict.Add(key, result);
            //     }
            //}


            string name = string.Format("{0}_{1}", category, para.ToString()).ToLower();

            T result = HttpRuntime.Cache[name] as T;

            if (result == null)
            {
                result = handler(para);

                if (result != null)
                {
                    DateTime absoluteTime = DateTime.MaxValue;
                    if (durationTime != TimeSpan.MaxValue)
                    {
                        absoluteTime = DateTime.Now.Add(durationTime);
                    }
                    HttpRuntime.Cache.Insert(name, result, null, absoluteTime, TimeSpan.Zero);
                }
            }

            return result;
        }
    }
}
