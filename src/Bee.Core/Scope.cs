using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace Bee.Core
{
    // this class copied from MSDN :
    // http://msdn.microsoft.com/zh-cn/magazine/cc300805(en-us).aspx
    public class Scope<T> : IDisposable where T : class
    {
        private bool _disposed;
        private bool _ownsInstance;
        private T _instance;
        private Scope<T> _parent;

        [ThreadStatic]
        protected static Scope<T> _head;

        public Scope(T instance) : this(instance, true) { }

        public Scope(T instance, bool ownsInstance)
        {
            Init(instance, ownsInstance);
        }

        protected void Init(T instance, bool ownsInstance)
        {
            if (instance == null)
                throw new ArgumentNullException("instance");
            _instance = instance;
            _ownsInstance = ownsInstance;

            Thread.BeginThreadAffinity();
            _parent = _head;
            _head = this;
        }

        public static T Current
        {
            get { return _head != null ? _head._instance : null; }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                Debug.Assert(this == _head, "Disposed out of order.");
                _head = _parent;
                Thread.EndThreadAffinity();

                if (_ownsInstance)
                {
                    var disposable = _instance as IDisposable;
                    if (disposable != null) disposable.Dispose();
                }
            }
        }
    }
}
