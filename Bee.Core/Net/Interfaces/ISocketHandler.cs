using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace Bee.Net
{
    public interface ISocketHandler
    {
        ISocketConnection SockectConnection { get; }

        void Receive(IEnumerable<byte> data);

        byte[] FrameText(string text);
        byte[] FrameBinary(byte[] bytes);
        byte[] FrameClose(int code);
    }
}
