using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bee.Net;
using System.Net.Sockets;


using Bee.Net.WebSocket;
using Bee.Util;

namespace Bee.Net.HttpSocket
{
    internal class HttpSocketHandler : ISocketHandler
    {
        private HttpSocketServer owner;
        private IWebSocketHandler handler;
        private List<byte> data = new List<byte>();

        public HttpSocketHandler(HttpSocketServer httpSocketServer, ISocketConnection socketConnection)
        {
            owner = httpSocketServer;
            SockectConnection = socketConnection;
        }

        public ISocketConnection SockectConnection
        {
            get;
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
            {
                Write("error request");
                return;
            }


            Write("hello world");

        }

        private void Write(string content)
        {
            if (content == null)
            {
                content = string.Empty;
            }

            var contentBytes = Encoding.UTF8.GetBytes(content);
            var headerByes = this.GetHeaderBytes(contentBytes.Length);

            var all = new byte[contentBytes.Length + headerByes.Length];
            Buffer.BlockCopy(headerByes, 0, all, 0, headerByes.Length);
            Buffer.BlockCopy(contentBytes, 0, all, headerByes.Length * sizeof(byte), contentBytes.Length);

            SockectConnection.Send(all);
        }

        /// <summary>
        /// 生成头部数据
        /// </summary>
        /// <param name="contentLength">内容长度</param>
        /// <returns></returns>
        private byte[] GetHeaderBytes(int contentLength)
        {
            var header = new StringBuilder()
                   .AppendFormat("HTTP/1.1 {0} {1}", 200, "OK").AppendLine()
                   .AppendLine("Content-Type: text/html; charset=utf-8");

            if (contentLength > -1)
            {
                header.AppendFormat("Content-Length: {0}", contentLength).AppendLine();
            }

            header
                .AppendFormat("Date: {0}", DateTime.Now.ToUniversalTime().ToString("r")).AppendLine()
                .AppendLine("Server: BeeSocketServer");

            //var keys = this.Headers.AllKeys.Where(item => IsIgnoreKey(item) == false).ToArray();
            //foreach (var key in keys)
            //{
            //    var value = this.Headers[key];
            //    if (string.IsNullOrWhiteSpace(value) == false)
            //    {
            //        header.AppendFormat("{0}: {1}", key, value).AppendLine();
            //    }
            //}
            return Encoding.ASCII.GetBytes(header.AppendLine().ToString());
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
