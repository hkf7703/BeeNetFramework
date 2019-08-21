/*
 * 创建人：hukaifeng (hkf7703@163.com)

 * 最后更新：2013/10/18 10:13:34 
 * 功能说明： SocketServer类
 * 
 
 * 主要类、属性，成员及其功能
    1. 
 * 历史修改记录：
	1 hukaifeng, 2013/10/18 10:13:34 ,  1.0.0.0, create   
	2 

*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using Bee.Logging;

namespace Bee.Net
{
    public class SocketServer : ISocketServer
    {
        public SocketServer(int port)
        {
            Port = port;
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            ListenerSocket = new SocketWrapper(socket);
        }

        public int Port { get; set; }
        public ISocket ListenerSocket { get; set; }
        public X509Certificate2 Certificate { get; set; }
        private Action<ISocketConnection> config;

        public virtual bool IsSecure
        {
            get { return Certificate != null; }
        }

        public void Start(Action<ISocketConnection> config)
        {
            this.config = config;

            var ipLocal = new IPEndPoint(IPAddress.Any, Port);
            ListenerSocket.Bind(ipLocal);
            ListenerSocket.Listen(100);

            Logger.Debug("Socket Server started");

            ListenForClients();
        }

        private void ListenForClients()
        {
            ListenerSocket.Accept(OnClientConnect, e => Logger.Error("Listener socket is closed", e));
        }

        private void OnClientConnect(ISocket clientSocket)
        {
            Logger.Debug(String.Format("Client connected from {0}:{1}", clientSocket.RemoteIpAddress, clientSocket.RemotePort.ToString()));

            ListenForClients();

            SocketConnection connection = new SocketConnection(clientSocket);
            if (config != null)
            {
                config(connection);
            }

            connection.SocketHandler = CreateHandler(connection);

            if (IsSecure)
            {
                Logger.Debug("Authenticating Secure Connection");
                clientSocket
                    .Authenticate(Certificate,
                                  connection.StartReceiving,
                                  e => Logger.Error("Failed to Authenticate", e));
            }
            else
            {
                connection.StartReceiving();
            }
        }

        protected virtual ISocketHandler CreateHandler(ISocketConnection sockectConnection)
        {
            return null;
        }

        public void Stop()
        {
            ListenerSocket.Dispose();
        }


        public void Dispose()
        {
            ListenerSocket.Dispose();
        }
    }
}
