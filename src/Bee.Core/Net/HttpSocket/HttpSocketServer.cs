using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bee.Net;

namespace Bee.Net.HttpSocket
{
    public class HttpSocketServer : SocketServer
    {
        private readonly string scheme;

        public HttpSocketServer(string path)
            : this(8426, path)
        {
        }

        public HttpSocketServer(int port, string path)
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
            get { return scheme == "https" && Certificate != null; }
        }

        protected override ISocketHandler CreateHandler(ISocketConnection sockectConnection)
        {
            return new HttpSocketHandler(this, sockectConnection);
        }
    }
}
