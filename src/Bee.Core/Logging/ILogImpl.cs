using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Bee.Logging
{
    internal delegate void LogEventHandler(LogLevel type, string name, string message, Exception exception);

    [TypeConverter(typeof(LogLeveConverter))]
    public class LogLevel : IComparable
    {
        private int value;
        private static Dictionary<int, string> dict = new Dictionary<int, string>();

        private static LogLevel core = new LogLevel(1);
        private static LogLevel debug = new LogLevel(2);
        private static LogLevel info = new LogLevel(4);
        private static LogLevel error = new LogLevel(6);
        private static LogLevel fatal = new LogLevel(7);

        static LogLevel()
        {
            dict.Add(1, "Core");
            dict.Add(2, "Debug");
            dict.Add(4, "Info");
            dict.Add(6, "Error");
            dict.Add(7, "Fatal");
        }

        private LogLevel()
        {
        }

        private LogLevel(int level)
        {
            value = level;
        }

        public static LogLevel Core { get { return core; } }
        public static LogLevel Debug { get { return debug; } }
        public static LogLevel Info { get { return info; } }
        public static LogLevel Error { get { return error; } }
        public static LogLevel Fatal { get { return fatal; } }

        internal int Value
        {
            get
            {
                return value;
            }
        }

        public override string ToString()
        {
            return dict[value];
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            LogLevel logLevel = obj as LogLevel;

            return logLevel != null && logLevel.value == value;
        }

        #region IComparable 成员

        public int CompareTo(object obj)
        {
            LogLevel logLevel = obj as LogLevel;
            if (logLevel != null)
            {
                return value.CompareTo(logLevel.value);
            }
            return 1;
        }

        #endregion
    }

    public class LogLeveConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return ((sourceType == typeof(string)) || base.CanConvertFrom(context, sourceType));
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return ((destinationType == typeof(string)) || base.CanConvertTo(context, destinationType));
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            string str = value as string;

            LogLevel result = null;

            switch (str.Trim().ToLower())
            {
                case "core":
                    result = LogLevel.Core;
                    break;
                case "debug":
                    result = LogLevel.Debug;
                    break;
                case "info":
                    result = LogLevel.Info;
                    break;
                case "error":
                    result = LogLevel.Error;
                    break;
                case "fatal":
                    result = LogLevel.Fatal;
                    break;
                default:
                    break;

            }

            return result;
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                return value.ToString();
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
 

    }


    public interface ILogImpl
    {
        void ProcessLog(LogLevel level, string name, string message, Exception exception);
    }
}
