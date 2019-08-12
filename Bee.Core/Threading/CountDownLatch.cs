using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Threading;

namespace Bee.Threading
{
    public sealed class CountDownLatch
    {
        private object lockobj = new object();
        private int counts;

        public CountDownLatch(int counts)
        {
            this.counts = counts;
        }

        public int Current
        {
            get
            {
                return this.counts;
            }
        }

        public void Await()
        {
            lock (lockobj)
            {
                while (counts > 0)
                {
                    Monitor.Wait(lockobj);
                }
            }
        }
        public void CountDown()
        {
            lock (lockobj)
            {
                counts--;
                Monitor.PulseAll(lockobj);
            }
        }

    }
}
