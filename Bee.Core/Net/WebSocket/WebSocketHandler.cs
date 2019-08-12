using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bee.Net;
using System.Net.Sockets;

namespace Bee.Net.WebSocket
{
    internal class WebSocketHandler : ISocketHandler
    {
        private WebSocketServer owner;
        private IWebSocketHandler handler;
        private List<byte> data = new List<byte>();


        public WebSocketHandler(WebSocketServer webSocketServer, ISocketConnection socketConnection)
        {
            owner = webSocketServer;
            SockectConnection = socketConnection;
        }

        public ISocketConnection SockectConnection
        {
            get ;
            private set;
        }

        public IWebSocketConnectionInfo ConnectionInfo { get; private set; }

        public void Receive(IEnumerable<byte> buffer)
        {
            if (handler != null)
            {
                handler.Receive(buffer);
            }
            else
            {
                data.AddRange(buffer);
                CreateHandler(data);
            }
        }

        public void CreateHandler(IEnumerable<byte> data)
        {
            byte[] bytes = data.ToArray();
            var request = RequestParser.Parse(bytes, owner.Schema);
            if (request == null)
                return;
            handler = WebSocketHandlerFactory.BuildHandler(request, SockectConnection.OnMessage, SockectConnection.OnClose, SockectConnection.OnBinary);
            if (handler == null)
                return;
            var subProtocol = SubProtocolNegotiator.Negotiate(owner.SupportedSubProtocols, request.SubProtocols);

            ConnectionInfo = WebSocketConnectionInfo.Create(request, SockectConnection.Socket.RemoteIpAddress, SockectConnection.Socket.RemotePort, subProtocol);

            if (!string.IsNullOrEmpty(ConnectionInfo.Path))
            {
                SockectConnection.Session.Add("WebSocket_Path", ConnectionInfo.Path);
            }

            foreach (string item in ConnectionInfo.Cookies.Keys)
            {
                SockectConnection.Session.Add(item, ConnectionInfo.Cookies[item]);
            }

            var handshake = handler.CreateHandshake(subProtocol);
            SockectConnection.RawSend(handshake);
        }

        #region ISocketHandler 成员


        public byte[] FrameText(string text)
        {
            if (handler != null)
            {
                return handler.FrameText(text);
            }
            else
            {
                return Encoding.UTF8.GetBytes(text);
            }
        }

        public byte[] FrameBinary(byte[] bytes)
        {
            if (handler != null)
            {
                return handler.FrameBinary(bytes);
            }
            else
            {
                return bytes;
            }
        }

        public byte[] FrameClose(int code)
        {
            if (handler != null)
            {
                return handler.FrameClose(code);
            }
            else
            {
                return new byte[0];
            }
        }

        #endregion
    }
}
