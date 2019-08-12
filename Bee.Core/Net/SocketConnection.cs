/*
 * 创建人：hukaifeng (hkf7703@163.com)

 * 最后更新：2013/10/18 11:07:14 
 * 功能说明： SocketConnection类
 * 
 
 * 主要类、属性，成员及其功能
    1. 
 * 历史修改记录：
	1 hukaifeng, 2013/10/18 11:07:14 ,  1.0.0.0, create   
	2 

*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bee.Logging;
using System.IO;

namespace Bee.Net
{
    public class SocketConnection : ISocketConnection
    {
        public SocketConnection(ISocket socket)
        {
            Socket = socket;
            OnOpen = () => { };
            OnClose = () => { };
            OnError = x => { };

            OnMessage = x => { };
            OnBinary = x => { };

            Session = new BeeDataAdapter();
        }

        public ISocket Socket { get; set; }

        private bool _closing;
        private bool _closed;
        private const int ReadSize = 1024 * 4;

        public Action OnOpen { get; set; }

        public Action OnClose { get; set; }

        public Action<Exception> OnError { get; set; }

        public Action<string> OnMessage { get; set; }

        public Action<byte[]> OnBinary { get; set; }

        public BeeDataAdapter Session { get; private set; }

        public ISocketHandler SocketHandler { get; internal set; }

        public void Send(byte[] bytes)
        {
            if (SocketHandler != null)
            {
                bytes = SocketHandler.FrameBinary(bytes);
            }

            RawSend(bytes);
        }

        public void Send(string message)
        {
            byte[] bytes = null;
            if (SocketHandler != null)
            {
                bytes = SocketHandler.FrameText(message);
            }
            else
            {
                bytes = Encoding.UTF8.GetBytes(message);
            }

            RawSend(bytes);
        }

        public void RawSend(byte[] bytes)
        {
            if (!IsAvailable)
            {
                Logger.Error("Data sent while closing or after close. Ignoring.");
                return;
            }

            Socket.Send(bytes, () =>
            {
                Logger.Debug("Sent " + bytes.Length + " bytes");
                Close();
            },
            e =>
            {
                CloseSocket();

                HandleReadError(e);
            });
        }

        public virtual void StartReceiving()
        {
            OnOpen();
            var buffer = new byte[ReadSize];
            Read(buffer);
        }

        private void Read(byte[] buffer)
        {
            if (!IsAvailable)
                return;

            Socket.Receive(buffer, r =>
            {
                if (r <= 0)
                {
                    Logger.Debug("0 bytes read. Closing.");
                    CloseSocket();
                    return;
                }

                Logger.Debug(r + " bytes read");

                var readBytes = buffer.Take(r);
                if (SocketHandler != null)
                {
                    SocketHandler.Receive(readBytes);
                }

                Read(buffer);
            },
            HandleReadError, 0);
        }

        private void HandleReadError(Exception e)
        {
            if (e is ObjectDisposedException)
            {
                Logger.Debug("Swallowing ObjectDisposedException", e);
                return;
            }

            OnError(e);

            if (e is IOException)
            {
                Logger.Debug("Error while reading", e);
                Close();
            }
            else
            {
                Logger.Error("Application Error", e);
                Close();
            }
        }

        public void Close()
        {
            CloseSocket();
        }


        private void CloseSocket()
        {
            _closing = true;
            OnClose();
            _closed = true;
            Socket.Dispose();
            _closing = false;
        }

        public bool IsAvailable
        {
            get { return !_closing && !_closed && Socket.Connected; }
        }
    }
}
