using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bee
{
    public abstract class FlyweightBase<TKey, TValue>
    {
        
        protected static Dictionary<TKey, TValue> InnerDict;

        
        static FlyweightBase()
        {
            InnerDict = new Dictionary<TKey, TValue>();
        }

        protected FlyweightBase()
        {
        }


        protected abstract TValue CreateInstance(TKey t);

        protected virtual TValue InnerGetInstance(TKey key)
        {
            if (InnerDict.ContainsKey(key))
            {
                return InnerDict[key];
            }
            lock (InnerDict)
            {
                if (InnerDict.ContainsKey(key))
                {
                    return InnerDict[key];
                }
                TValue value = this.CreateInstance(key);
                if (value != null)
                {
                    InnerDict[key] = value;
                }
                return value;
            }
        }

        public TValue GetInstance(TKey key)
        {
            return this.InnerGetInstance(key);
        }
    }
}
