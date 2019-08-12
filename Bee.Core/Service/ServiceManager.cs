
using System;
using System.Threading;
using Bee.Logging;
using System.Collections.Generic;

namespace Bee.Service
{
    public class ServiceManager : IDisposable
    {
        private static ServiceManager instance = new ServiceManager();

        private readonly List<BaseRunService> serviceList = new List<BaseRunService>();

        private ServiceManager()
        {
        }

        /// <summary>
        /// 使用该实例可能会发生应用程序卸载， 导致线程中断的问题。
        /// </summary>
        public static ServiceManager Instance
        {
            get
            {
                return instance;
            }
        }

        public void AppendTask(BaseRunService baseRunService)
        {
            serviceList.Add(baseRunService);
            var thread = new Thread(baseRunService.Start);
            thread.IsBackground = true;
            thread.Start();
        }

        public void StopService()
        {
            foreach (BaseRunService baseRunService in serviceList)
            {
                baseRunService.Stop();
            }
        }


        public void Dispose()
        {
            StopService();
        }
    }
}
