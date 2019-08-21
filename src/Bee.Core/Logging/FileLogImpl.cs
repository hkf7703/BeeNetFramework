using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bee.Util;
using System.IO;
using Bee.Core;

namespace Bee.Logging
{
    internal class FileLogImpl : ILogImpl, IDisposable
    {
        private int lastDay = DateTime.Now.Day;
        private string logFileName;
        private StreamWriter writer;
        private static object lockobject = new object();

        public FileLogImpl()
        {
            IOUtil.SafeCreateDirectory(LogFileName);
        }

        #region ILogImpl 成员

        public void ProcessLog(LogLevel level, string name, string message, Exception exception)
        {
            lock (this)
            {
                if (level == LogLevel.Core) return;
                writer = GetCurrentWriter();
                lock (writer)
                {
                    writer.WriteLine(Logger.FormatOut(level, name, message, exception));
                    writer.Flush();
                    //writer.Close();
                    //writer = null;
                }
            }
        }

        #endregion

        private StreamWriter GetCurrentWriter()
        {
            bool flag = true;
            lock (lockobject)
            {
                if (writer != null)
                {
                    if (this.lastDay == DateTime.Now.Day)
                    {
                        flag = false;
                    }
                }

                if (flag)
                {
                    if (writer != null)
                    {
                        writer.Close();
                        writer = null;
                    }

                    string logFileNameValue = LogFileName;

                    int tryTime = 1;
                    while (tryTime < 5)
                    {
                        try
                        {
                            this.writer = new StreamWriter(File.Open(logFileNameValue,
                                FileMode.Append, FileAccess.Write, FileShare.Read));
                            break;
                        }
                        catch (IOException)
                        {
                            logFileNameValue = logFileNameValue + ".log";

                            if (tryTime == 4)
                            {
                                throw;
                            }
                        }
                        tryTime++;
                    }
                }
            }
            return writer;
        }

        private string LogFileName
        {
            get
            {
                if (this.lastDay != DateTime.Now.Day || string.IsNullOrEmpty(logFileName))
                {
                    if (!string.IsNullOrEmpty(LogSetting.Default.FileDir))
                    {
                        this.logFileName = string.Format(LogSetting.Default.FileDir, DateTime.Today.ToString("yyyyMMdd"));
                    }
                    else
                    {
                        this.logFileName = string.Format(Constants.LogFileName, DateTime.Today.ToString("yyyyMMdd"));
                    }

                    this.logFileName = Path.Combine(GeneralUtil.BaseDirectory, logFileName);


                    this.lastDay = DateTime.Now.Day;
                }
                return this.logFileName;
            }
        }


        #region IDisposable 成员

        public void Dispose()
        {
            GeneralUtil.CatchAll(delegate
            {
                if (this.writer != null)
                {
                    this.writer.Close();
                    this.writer = null;
                }
            });

        }

        #endregion
    }
}
