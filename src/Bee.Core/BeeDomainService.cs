using Bee.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bee
{
    public abstract class BeeDomainService<T> where T : class
    {
        private static T _instance;
        public static readonly object lockObject = new object();

        protected BeeDomainService()
        {

        }

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (lockObject)
                    {
                        if (_instance == null)
                        {
                            _instance = (T)Activator.CreateInstance(typeof(T), true); 
                        }
                    }
                }
                return _instance;
            }
            
        }


        /// <summary>
        /// Gets the instance of the DbSession.
        /// </summary>
        /// <returns>the instance of DbSession.</returns>
        protected DbSession GetDbSession()
        {
            return GetDbSession(false);
        }

        /// <summary>
        /// Gets the instance of the DbSession.
        /// </summary>
        /// <param name="useTransaction">the flag indicate to use the transaction or not.</param>
        /// <returns></returns>
        protected abstract DbSession GetDbSession(bool useTransaction);
    }
}
