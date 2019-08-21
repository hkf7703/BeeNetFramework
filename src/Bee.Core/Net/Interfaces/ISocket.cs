using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace Bee.Net
{
    public interface ISocket : IDisposable
    {
        bool Connected { get; }
        string RemoteIpAddress { get; }
        int RemotePort { get; }
        Stream Stream { get; }
        bool NoDelay { get; set; }

        void Accept(Action<ISocket> callback, Action<Exception> error);
        void Send(byte[] buffer, Action callback, Action<Exception> error);
        void Receive(byte[] buffer, Action<int> callback, Action<Exception> error, int offset);
        void Authenticate(X509Certificate2 certificate, Action callback, Action<Exception> error);

        void Bind(EndPoint ipLocal);
        void Listen(int backlog);
    }
}
