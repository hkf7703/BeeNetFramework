using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bee.Util;
using System.Text.RegularExpressions;

namespace Bee.Logging
{
    public static class Logger
    {
        private static LogEventHandler LogEvent;

        private static bool EnableFlag = true;

        private static Dictionary<string, Type> LoggerImplementDict = 
            new Dictionary<string, Type>(StringComparer.InvariantCultureIgnoreCase);

        static Logger()
        {
            LoggerImplementDict.Add("Console", typeof(ConsoleLogImpl));
            LoggerImplementDict.Add("File", typeof(FileLogImpl));

            LogSetting.Default.Level = LogLevel.Core;
            if (LogSetting.Default.Target == null)
            {
                LogSetting.Default.Target = new List<string>();
                LogSetting.Default.Target.Add("File");
            }

            if (LogSetting.Default.Level == null)
            {
                LogSetting.Default.Level = LogLevel.Error;
            }

            foreach (string targetItem in LogSetting.Default.Target)
            {
                ILogImpl logImpl =
                    ReflectionUtil.CreateInstance(LoggerImplementDict[targetItem])
                    as ILogImpl;

                if (logImpl != null)
                {
                    Init(logImpl);
                }
            }
        }

        public static bool Enable
        {
            get
            {
                return EnableFlag;
            }
            set
            {
                EnableFlag = value;
            }
        }

        public static void Init(ILogImpl logImp)
        {
            LogEvent = (LogEventHandler)Delegate.Combine(LogEvent, new LogEventHandler(logImp.ProcessLog));
        }

        public static void Debug(string message)
        {
            InnerLog(LogLevel.Debug, message, null);
        }

        public static void Debug(string message, Exception exception)
        {
            InnerLog(LogLevel.Debug, message, exception);
        }

        public static void Info(string message)
        {
            InnerLog(LogLevel.Info, message, null);
        }

        public static void Info(string message, Exception exception)
        {
            InnerLog(LogLevel.Info, message, exception);
        }

        public static void Error(string message)
        {
            InnerLog(LogLevel.Error, message, null);
        }

        public static void Error(string message, Exception exception)
        {
            InnerLog(LogLevel.Error, message, exception);
        }

        public static void Fatal(string message)
        {
            InnerLog(LogLevel.Fatal, message, null);
        }

        public static void Fatal(string message, Exception exception)
        {
            InnerLog(LogLevel.Fatal, message, exception);
        }

        public static void Log(LogLevel level, string message)
        {
            InnerLog(level, message, null);
        }

        public static void Log(LogLevel level, string message, Exception exception)
        {
            InnerLog(level, message, exception);
        }

        private static void InnerLog(LogLevel level, string message, Exception exception)
        {
            string name = GeneralUtil.GetStackTrackFunctionName(0);
            if (LogEvent != null && EnableFlag)
            {
                if ((LogSetting.Default.Level != null && LogSetting.Default.Level.CompareTo(level) <= 0)
                    || LogSetting.Default.InnerLevel == level)
                {
                    LogEvent(level, name, message, exception);
                }
            }
        }

        internal static string FormatOut(LogLevel level, string name, string message, Exception exception)
        {
            return string.Format("{0} [{5}] {1} {2} \r\n{3} \r\n{4}", GeneralUtil.CurrentTime, level, name, message, exception, System.Threading.Thread.CurrentThread.ManagedThreadId);
        }

    }
}
