using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bee.Net.WebSocket
{
    internal interface IWebSocketHandler
    {
        byte[] CreateHandshake(string subProtocol);
        void Receive(IEnumerable<byte> data);
        byte[] FrameText(string text);
        byte[] FrameBinary(byte[] bytes);
        byte[] FrameClose(int code);
    }
}
