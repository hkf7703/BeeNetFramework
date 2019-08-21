using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bee.Net
{
    public interface ISocketConnection
    {
        ISocket Socket { get; set; }
        Action OnOpen { get; set; }
        Action OnClose { get; set; }
        Action<string> OnMessage { get; set; }

        Action<byte[]> OnBinary { get; set; }

        Action<Exception> OnError { get; set; }
        void Send(byte[] message);
        void Send(string message);
        void RawSend(byte[] message);
        void Close();
        bool IsAvailable { get; }

        BeeDataAdapter Session { get; }
    }
}
