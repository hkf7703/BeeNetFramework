
using System;
using System.Threading;
using Bee.Logging;
using Bee.Util;

namespace Bee.Service
{
    public class BaseRunService : IDisposable
    {
        private bool _started;
        protected string ServiceName = string.Empty;
        private readonly ManualResetEvent _mreExit = new ManualResetEvent(false);
        protected int Interval = 60;
        protected bool RunOnce;
        protected bool RunFlag = true;
        public BaseRunService()
            : this(60)
        {
        }

        public BaseRunService(int interval)
        {
            Type type = GetType();
            ServiceName = type.Name;
            Interval = interval;

            if (HttpContextUtil.CurrentHttpContext != null 
                && HttpContextUtil.CurrentHttpContext.Request.Url.Host.IndexOf("localhost") >= 0)
            {
                RunFlag = false;
            }

        }

        public void Start()
        {
            if (!RunFlag) return;

            Logger.Info(string.Format("{0} is started", ServiceName));
            if (!_started)
            {
                _started = true;

                while (_started)
                {
                    try
                    {
                        Run();
                    }
                    catch (Exception e)
                    {
                        Logger.Error(string.Format("{0} 发生错误.", ServiceName), e);
                    }

                    if (RunOnce)
                    {
                        Stop();
                    }

                    if (_started)
                    {
                        _mreExit.WaitOne(Interval * 1000, false);
                    }
                }
            }
        }

        protected virtual void Run()
        {
        }

        public virtual void Stop()
        {
            _started = false;
            _mreExit.Set();

            Logger.Info(string.Format("{0} is stopped", ServiceName));
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
