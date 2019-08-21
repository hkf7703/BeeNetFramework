
using Bee.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Bee.Threading
{
    public abstract class AbstractSmartQueue<T> : IProducerConsumer<T>
    {
        private Queue<T> innerQueue = new Queue<T>();
        private int threadCount = Environment.ProcessorCount;
        private int maxQueueSize = Environment.ProcessorCount * 64;

        private CountUpLatch threadCountLatch = null;
        private CountUpLatch queueSizeLatch = null;

        private object lockObject = new object();

        private bool runFlag = true;
        public AbstractSmartQueue(int threadCount, int maxQueueSize)
        {
            this.threadCount = threadCount;
            this.maxQueueSize = maxQueueSize;

            threadCountLatch = new CountUpLatch(threadCount);
            queueSizeLatch = new CountUpLatch(maxQueueSize);
        }

        public int CurrentQueueSize
        {
            get
            {
                return innerQueue.Count;
            }
        }

        public int CurrentThreadCount
        {
            get
            {
                return threadCountLatch.Current;
            }
        }

        public int MaxQueueSize
        {
            get
            {
                return this.maxQueueSize;
            }
        }

        public int ThreadCount
        {
            get
            {
                return this.threadCount;
            }
        }

        protected abstract void Process(T element);

        private void Run()
        {
            while (runFlag)
            {
                threadCountLatch.Await();

                Thread.Sleep(20);
                if (!CanTake())
                {
                    Thread.Sleep(10);
                    continue;
                }

                threadCountLatch.CountUp();

                DeferredHelper.When<T>(() =>
                {
                    T item = Take();

                    return item;
                })
                .Then((item) =>
                {
                    this.Process(item);
                    threadCountLatch.CountDown();
                })
                .Fail((ex) =>
                {
                    Logging.Logger.Error("Process Queue error!", ex);
                    threadCountLatch.CountDown();
                });
            }
        }

        public void Start()
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                GeneralUtil.CatchAll(() =>
                {
                    Run();
                });

            });
        }

        public void Stop()
        {
            runFlag = false;
        }

        public void Add(T item)
        {
            queueSizeLatch.Await();

            queueSizeLatch.CountUp();

            lock (lockObject)
            {
                this.innerQueue.Enqueue(item);
            }
        }

        private bool CanTake()
        {
            lock (lockObject)
            {
                return innerQueue.Count > 0;
            }
        }

        public T Take()
        {
            lock (lockObject)
            {
                T result = default(T);

                ThrowExceptionUtil.ArgumentConditionTrue(innerQueue.Count > 0, string.Empty, "there is no item in queue");

                queueSizeLatch.CountDown();

                result = innerQueue.Dequeue();

                return result;
            }
        }
    }
}
