using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bee.Net
{
    public interface ISocketServer : IDisposable
    {
        void Start(Action<ISocketConnection> config);

        void Stop();
    }
}
