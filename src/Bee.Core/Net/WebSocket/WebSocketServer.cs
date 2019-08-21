using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bee.Net;

namespace Bee.Net.WebSocket
{
    public class WebSocketServer : SocketServer
    {
        private readonly string scheme;

        public WebSocketServer(string path)
            : this(8426, path)
        {
        }

        public WebSocketServer(int port, string path)
            :base(port)
        {
            var uri = new Uri(path);
            scheme = uri.Scheme;

            SupportedSubProtocols = new string[0];
        }

        public IEnumerable<string> SupportedSubProtocols { get; set; }

        internal string Schema
        {
            get
            {
                return this.scheme;
            }
        }

        public override bool IsSecure
        {
            get { return scheme == "wss" && Certificate != null; }
        }

        protected override ISocketHandler CreateHandler(ISocketConnection sockectConnection)
        {
            return new WebSocketHandler(this, sockectConnection);
        }
    }
}
