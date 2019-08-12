using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Bee.Util;
using Bee.Core;

namespace Bee.Logging
{
    internal class LogSettingManager
    {
        private static LogSettingManager instance = new LogSettingManager();

        private LogSetting logSetting = null;

        private LogSettingManager()
        {
        }

        private void Init()
        {
            string loggingSetting = ConfigUtil.GetAppSettingValue<string>(Constants.LoggingSetting);

            if (!string.IsNullOrEmpty(loggingSetting))
            {
                logSetting = SerializeUtil.SimpleDeserialize<LogSetting>(loggingSetting);
            }
            else
            {
                logSetting = new LogSetting();
            }
        }

        public static LogSettingManager Instance
        {
            get
            {
                return instance;
            }
        }

        public LogSetting LogSetting
        {
            get
            {
                if (logSetting == null)
                {
                    Init();
                }
                return logSetting; 
            }
        }
    }

    public class LogSetting
    {
        public List<string> Target { get; set; }
        public LogLevel Level { get; set; }
        public LogLevel InnerLevel { get; set; }
        public string FileDir { get; set; }
        public bool Listen { get; set; }

        public static LogSetting Default
        {
            get
            {
                return LogSettingManager.Instance.LogSetting;
            }
        }
    }
}
