using System;
using System.Collections.Generic;
using System.Linq;

namespace Fleck
{
    public class WebSocketConnection : IWebSocketConnection
    {
        public WebSocketConnection(ISocket socket, Func<byte[], WebSocketHttpRequest> parseRequest, Func<WebSocketHttpRequest, IHandler> handlerFactory)
        {
            Socket = socket;
            OnOpen = () => { };
            OnClose = () => { };
            OnMessage = x => { };
            OnError = x => { };
            _handlerFactory = handlerFactory;
            _parseRequest = parseRequest;
        }

        public ISocket Socket { get; set; }

        private readonly Func<WebSocketHttpRequest, IHandler> _handlerFactory;
        readonly Func<byte[], WebSocketHttpRequest> _parseRequest;
        public IHandler Handler { get; set; }
        private bool _closed;
        private const int ReadSize = 1024 * 4;

        public Action OnOpen { get; set; }
        public Action OnClose { get; set; }
        public Action<string> OnMessage { get; set; }
        public Action<Exception> OnError { get; set; }
        public IWebSocketConnectionInfo ConnectionInfo { get; private set; }

        public void Send(string message)
        {
            if (Handler == null)
                throw new WebSocketException("Cannot send before handshake");

            if (_closed || !Socket.Connected)
            {
                FleckLog.Warn("Data sent after close. Ignoring.");
                return;
            }

            var bytes = Handler.FrameText(message);
            SendBytes(bytes);
        }

        public void StartReceiving()
        {
            var data = new List<byte>(ReadSize);
            var buffer = new byte[ReadSize];
            Read(data, buffer);
        }

        private void Read(List<byte> data, byte[] buffer)
        {
            if (_closed || !Socket.Connected)
                return;
            Socket.Receive(buffer, r =>
            {
                if (r <= 0)
                {
                    FleckLog.Debug("0 bytes read. Closing.");
                    CloseSocket();
                    return;
                }
                FleckLog.Debug(r + " bytes read");
                var readBytes = buffer.Take(r);
                if (Handler != null)
                {
                    Handler.Receive(readBytes);
                }
                else
                {
                    data.AddRange(readBytes);
                    CreateHandler(data);
                }

                Read(data, buffer);
            },
            e =>
            {
                OnError(e);
                if (e is HandshakeException)
                {
                    FleckLog.Debug("Error while reading", e);
                }
                else if (e is WebSocketException)
                {
                    FleckLog.Debug("Error while reading", e);
                    Close(WebSocketStatusCodes.ProtocolError);
                }
                else
                {
                    FleckLog.Error("Application Error", e);
                    Close(WebSocketStatusCodes.ApplicationError);
                }
            });
        }

        private void SendBytes(byte[] bytes, Action callback = null)
        {
            Socket.Send(bytes, () =>
            {
                FleckLog.Debug("Sent " + bytes.Length + " bytes");
                if (callback != null)
                    callback();
            },
            e =>
            {
                FleckLog.Info("Failed to send. Disconnecting.", e);
                CloseSocket();
            });
        }

        private void CreateHandler(IEnumerable<byte> data)
        {
            var request = _parseRequest(data.ToArray());
            if (request == null)
                return;
            Handler = _handlerFactory(request);
            if (Handler == null)
                return;
            ConnectionInfo = WebSocketConnectionInfo.Create(request);

            var handshake = Handler.CreateHandshake();
            SendBytes(handshake, OnOpen);
        }

        public void Close()
        {
            Close(1000);
        }

        public void Close(int code)
        {
            if (Handler == null)
            {
                CloseSocket();
                return;
            }

            var bytes = Handler.FrameClose(code);
            if (bytes.Length == 0)
                CloseSocket();
            else
                SendBytes(bytes, CloseSocket);
        }

        private void CloseSocket()
        {
            OnClose();
            _closed = true;
            Socket.Close();
            Socket.Dispose();
        }
    }
}
