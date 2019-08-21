using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bee.Threading
{
    public interface ISmartQueue<T> : IProducerConsumer<T>
    {
        int ThreadCount { get; }
        int MaxQueueSize { get;}

        int CurrentThreadCount { get;}
        int CurrentQueueSize { get; }
        
        void Start();

        void Stop();
    }
}
