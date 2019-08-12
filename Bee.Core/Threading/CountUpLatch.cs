using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Threading;

namespace Bee.Threading
{
    public sealed class CountUpLatch
    {
        private object lockobj = new object();
        private int counts;
        private int maxValue;

        public CountUpLatch(int maxValue)
        {
            this.counts = 0;
            this.maxValue = maxValue;
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
                while (counts >= maxValue)
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

        public void CountUp()
        {
            lock (lockobj)
            {
                counts++;
                Monitor.PulseAll(lockobj);
            }
        }
    }
}
