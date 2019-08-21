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
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Threading;
using Bee.Logging;

namespace Bee.Net
{
    internal class AsyncSocketAcceptState
    {
        public Action<ISocket> Callback;
        public Action<Exception> Error;
    }

    internal class AsyncSocketSendState
    {
        public Action Callback;
        public Action<Exception> Error;
        public byte[] Buffer;
    }

    public class SocketWrapper : ISocket
    {
        private readonly Socket _socket;
        private Stream _stream;


        public SocketWrapper(Socket socket)
        {
            _socket = socket;
            if (_socket.Connected)
                _stream = new NetworkStream(_socket);
        }

        public bool Connected
        {
            get { return _socket.Connected; }
        }

        public string RemoteIpAddress
        {
            get
            {
                var endpoint = _socket.RemoteEndPoint as IPEndPoint;
                return endpoint != null ? endpoint.Address.ToString() : null;
            }
        }

        public int RemotePort
        {
            get
            {
                var endpoint = _socket.RemoteEndPoint as IPEndPoint;
                return endpoint != null ? endpoint.Port : -1;
            }
        }

        public System.IO.Stream Stream
        {
            get { return _stream; }
        }

        public bool NoDelay
        {
            get { return _socket.NoDelay; }
            set { _socket.NoDelay = value; }
        }

        public void Accept(Action<ISocket> callback, Action<Exception> error)
        {
            //try
            //{
            //    SocketWrapper result = new SocketWrapper(_socket.Accept());

            //    callback(result);
            //}
            //catch (Exception e)
            //{
            //    error(e);
            //}
            AsyncSocketAcceptState state = new AsyncSocketAcceptState()
            {
                Callback = callback,
                Error = error
            };
            ThreadPool.QueueUserWorkItem(new WaitCallback(InnerAccept), state);
        }

        private void InnerAccept(object state)
        {
            AsyncSocketAcceptState stateValue = state as AsyncSocketAcceptState;
            try
            {
                if (stateValue == null) return;

                SocketWrapper result = new SocketWrapper(_socket.Accept());

                stateValue.Callback(result);
            }
            catch (Exception e)
            {
                stateValue.Error(e);
            }
        }

        public void Send(byte[] buffer, Action callback, Action<Exception> error)
        {
            AsyncSocketSendState state = new AsyncSocketSendState()
            {
                Callback = callback,
                Error = error,
                Buffer = buffer
            };
            ThreadPool.QueueUserWorkItem(new WaitCallback(InnerSend), state);
        }

        private void InnerSend(object state)
        {
            AsyncSocketSendState stateValue = state as AsyncSocketSendState;
            try
            {
                if (stateValue == null) return;

                _socket.Send(stateValue.Buffer);

                stateValue.Callback();
            }
            catch (Exception e)
            {
                stateValue.Error(e);
            }
        }

        public void Receive(byte[] buffer, Action<int> callback, Action<Exception> error, int offset)
        {
            try
            {
                int byteReceived = _socket.Receive(buffer, offset, buffer.Length, SocketFlags.None);
                callback(byteReceived);
            }
            catch (Exception e)
            {
                error(e);
            }
        }

        public void Authenticate(X509Certificate2 certificate, Action callback, Action<Exception> error)
        {
            try
            {
                var ssl = new SslStream(_stream, false);
                _stream = ssl;

                ssl.AuthenticateAsServer(certificate);

                callback();
            }
            catch (Exception e)
            {
                error(e);
            }
        }

        public void Dispose()
        {
            if (_stream != null) _stream.Close();
            if (_socket != null) _socket.Close();
        }

        public void Bind(System.Net.EndPoint ipLocal)
        {
            _socket.Bind(ipLocal);
        }

        public void Listen(int backlog)
        {
            _socket.Listen(backlog);
        }

    }


    public class AsyncSocketWrapper : ISocket
    {
        private readonly Socket _socket;
        private Stream _stream;
        private SocketAsyncEventArgs _acceptSocketArgs;

        public AsyncSocketWrapper(Socket socket)
        {
            _socket = socket;
            _acceptSocketArgs = new SocketAsyncEventArgs(); 
            if (_socket.Connected)
                _stream = new NetworkStream(_socket);
        }

        public bool Connected
        {
            get { return _socket.Connected; }
        }

        public string RemoteIpAddress
        {
            get
            {
                var endpoint = _socket.RemoteEndPoint as IPEndPoint;
                return endpoint != null ? endpoint.Address.ToString() : null;
            }
        }

        public int RemotePort
        {
            get
            {
                var endpoint = _socket.RemoteEndPoint as IPEndPoint;
                return endpoint != null ? endpoint.Port : -1;
            }
        }

        public System.IO.Stream Stream
        {
            get { return _stream; }
        }

        public bool NoDelay
        {
            get { return _socket.NoDelay; }
            set { _socket.NoDelay = value; }
        }

        public void Accept(Action<ISocket> callback, Action<Exception> error)
        {
            try
            {
                if (!this._socket.AcceptAsync(this._acceptSocketArgs))
                {
                    this.ProcessAccept(this._acceptSocketArgs, callback, error);
                }
            }
            catch (Exception ex)
            {
                error(ex);
                Thread.Sleep(1000);
                this.Accept(callback, error);
            }

        }

        private void ProcessAccept(SocketAsyncEventArgs e, Action<ISocket> callback, Action<Exception> error)
        {
            try
            {
                if (e.SocketError == SocketError.Success)
                {
                    Socket acceptSocket = e.AcceptSocket;
                    e.AcceptSocket = null;

                    AsyncSocketWrapper result = new AsyncSocketWrapper(acceptSocket);
                    callback(result);
                }
                else
                {
                    Bee.Util.GeneralUtil.CatchAll(() =>
                        {
                            e.AcceptSocket.Shutdown(SocketShutdown.Both);
                            e.AcceptSocket.Close(10000);
                        });
                    e.AcceptSocket = null;
                }
            }
            catch (Exception ex)
            {
                error(ex);
            }
            finally
            {
                this.Accept(callback, error);
            }
        }



        public void Send(byte[] buffer, Action callback, Action<Exception> error)
        {
            AsyncSocketSendState state = new AsyncSocketSendState()
            {
                Callback = callback,
                Error = error,
                Buffer = buffer
            };
            ThreadPool.QueueUserWorkItem(new WaitCallback(InnerSend), state);
        }

        private void InnerSend(object state)
        {
            AsyncSocketSendState stateValue = state as AsyncSocketSendState;
            try
            {
                if (stateValue == null) return;

                _socket.Send(stateValue.Buffer);

                stateValue.Callback();
            }
            catch (Exception e)
            {
                stateValue.Error(e);
            }
        }

        public void Receive(byte[] buffer, Action<int> callback, Action<Exception> error, int offset)
        {
            try
            {
                int byteReceived = _socket.Receive(buffer, offset, buffer.Length, SocketFlags.None);
                callback(byteReceived);
            }
            catch (Exception e)
            {
                error(e);
            }
        }

        public void Authenticate(X509Certificate2 certificate, Action callback, Action<Exception> error)
        {
            try
            {
                var ssl = new SslStream(_stream, false);
                _stream = ssl;

                ssl.AuthenticateAsServer(certificate);

                callback();
            }
            catch (Exception e)
            {
                error(e);
            }
        }

        public void Dispose()
        {
            if (_stream != null) _stream.Close();
            if (_socket != null) _socket.Close();
        }

        public void Bind(System.Net.EndPoint ipLocal)
        {
            _socket.Bind(ipLocal);
        }

        public void Listen(int backlog)
        {
            _socket.Listen(backlog);
        }
    }
}
