using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Security;
using Bee.Logging;

namespace Bee.Util
{
    /// <summary>
    /// The Util for general using.
    /// </summary>
    public static class GeneralUtil
    {
        private static string baseDirectory;
        private static bool isRunningOnMono;

        static GeneralUtil()
        {
            CatchAll(()=>
            {
                baseDirectory = AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar;

                isRunningOnMono = Type.GetType("Mono.Runtime") != null;
            });


        }

        /// <summary>
        /// Get the current time in the 'yyyy-MM-dd HH;mm:ss:fff' format.
        /// </summary>
        public static string CurrentTime
        {
            get
            {
                return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:ffff");
            }
        }

        /// <summary>
        /// Get the current function name.
        /// </summary>
        public static string CurrentFunctionName
        {
            get
            {
                return GetStackTrackFunctionName(0);
            }
        }

        /// <summary>
        /// Combine the path using the base directory of current AppDomain.
        /// </summary>
        /// <param name="relativePath">the relative path.</param>
        /// <returns>the combined path.</returns>
        public static string CombinePath(string relativePath)
        {
            ThrowExceptionUtil.ArgumentNotNullOrEmpty(relativePath, "relativePath");

            return Path.Combine(baseDirectory, relativePath);
        }

        /// <summary>
        /// Catch all the exception to invoke the delegate.
        /// </summary>
        /// <param name="callback">the delegate.</param>
        public static void CatchAll(CallbackVoidHandler callback)
        {
            CatchAll(callback, false);
        }

        /// <summary>
        /// Catch all the exception to invoke the delegate.
        /// </summary>
        /// <param name="callback">the delegate.</param>
        /// <param name="logFlag">the flag to idicate log the exception or not.</param>
        public static void CatchAll(CallbackVoidHandler callback, bool logFlag)
        {
            try
            {
                callback();
            }
            catch(Exception e)
            {
                if (logFlag)
                {
                    Logger.Error("Error", e);
                }
            }
        }

        /// <summary>
        /// Catch all the exception to invoke the delegate.
        /// </summary>
        /// <typeparam name="T">the generic type of the return value of the delegate.</typeparam>
        /// <param name="callback">the delegate.</param>
        /// <returns>the result of the delegate.</returns>
        public static T CatchAll<T>(CallbackReturnHandler<T> callback)
        {
            try
            {
                return callback();
            }
            catch
            {
            }

            return default(T);
        }


        #region General Extended Methods

        public static bool IsBetween<T>(this T t, T lowerBound, T upperBound)
            where T : IComparable<T>
        {
            return IsBetween(t, lowerBound, upperBound, true, true);
        }

        public static bool IsBetween<T>(this T t, T lowerBound, T upperBound,
            bool includeLowerBound, bool includeUpperBound)
            where T : IComparable<T>
        {
            if (t == null) throw new ArgumentNullException("t");

            var lowerCompareResult = t.CompareTo(lowerBound);
            var upperCompareResult = t.CompareTo(upperBound);

            return (includeLowerBound && lowerCompareResult == 0) ||
                (includeUpperBound && upperCompareResult == 0) ||
                (lowerCompareResult > 0 && upperCompareResult < 0);
        }



        public static IEnumerable<T> GetDescendants<T>(this T root,
         Func<T, IEnumerable<T>> childSelector, Predicate<T> filter)
        {
            foreach (T t in childSelector(root))
            {
                if (filter == null || filter(t))
                    yield return t;
                foreach (T child in GetDescendants((T)t, childSelector, filter))
                    yield return child;
            }
        }

        /// <summary>
        /// 先执行命令，再返回自身
        /// </summary>
        public static T Do<T>(this T t, Action<T> action)
        {
            action(t);
            return t;
        }

        #endregion

        #region CollectionUtil

        public static Dictionary<TKey, TResult> MapReduce<TInput, TKey, TValue, TResult>(
            this IEnumerable<TInput> list,
            Func<TInput, IEnumerable<KeyValuePair<TKey, TValue>>> map,
            Func<TKey, IEnumerable<TValue>, TResult> reduce)
        {
            Dictionary<TKey, List<TValue>> mapResult = new Dictionary<TKey, List<TValue>>();
            foreach (var item in list)
            {
                foreach (var one in map(item))
                {
                    List<TValue> mapValues;
                    if (!mapResult.TryGetValue(one.Key, out mapValues))
                    {
                        mapValues = new List<TValue>();
                        mapResult.Add(one.Key, mapValues);
                    }
                    mapValues.Add(one.Value);
                }
            }
            var result = new Dictionary<TKey, TResult>();
            foreach (var m in mapResult)
            {
                result.Add(m.Key, reduce(m.Key, m.Value));
            }
            return result;
        }

        #endregion

        /// <summary>
        /// Get the first date of the month.
        /// </summary>
        /// <param name="dateTime">the provided date.</param>
        /// <returns>the first date of the month.</returns>
        public static DateTime GetFirstDateOfMonth(this DateTime dateTime)
        {
            DateTime result = dateTime;

            DateTime.TryParse(string.Format("{0}-{1}-01", dateTime.Year, dateTime.Month), out result);

            return result;
        }

        #region StackTrack methods

        internal static string GetStackTrackFunctionName(int index)
        {
            StackTrace trace = new StackTrace(true);
            index += 3;

            if (trace.FrameCount > index)
            {
                return GetMothodDesc(trace.GetFrame(index), true);
            }
            return null;
        }

        private static string GetMothodDesc(StackFrame sf, bool needLineNo)
        {
            MethodBase method = sf.GetMethod();
            if (needLineNo)
            {
                return (GetMothodLine(method) + GetCurLine(sf));
            }
            return GetMothodLine(method);
        }

        private static string GetMothodLine(MethodBase mb)
        {
            StringBuilder builder = new StringBuilder();
            Type declaringType = mb.DeclaringType;
            if (declaringType != null)
            {
                string str = declaringType.Namespace;
                if (str != null)
                {
                    builder.Append(str);
                    builder.Append(".");
                }
                builder.Append(declaringType.Name);
                builder.Append(".");
            }
            builder.Append(mb.Name);
            builder.Append("(");
            ParameterInfo[] parameters = mb.GetParameters();
            for (int i = 0; i < parameters.Length; i++)
            {
                string name = "<UnknownType>";
                if (parameters[i].ParameterType != null)
                {
                    name = parameters[i].ParameterType.Name;
                }
                builder.Append(((i != 0) ? ", " : "") + name + " " + parameters[i].Name);
            }
            builder.Append(")");
            return builder.ToString();
        }

        private static string GetCurLine(StackFrame sf)
        {
            if (sf.GetILOffset() != -1)
            {
                string fileName = null;
                try
                {
                    fileName = sf.GetFileName();
                }
                catch (SecurityException)
                {
                }
                if (fileName != null)
                {
                    return string.Format(" in {0}:line {1}", fileName, sf.GetFileLineNumber());
                }
            }
            return "";
        }

        #endregion

        /// <summary>
        /// The base directory of the current AppDomain.
        /// </summary>
        public static string BaseDirectory
        {
            get
            {
                return baseDirectory;
            }
        }

        public static bool IsRunningOnMono
        {
            get
            {
                return isRunningOnMono;
            }
        }
    }
}
