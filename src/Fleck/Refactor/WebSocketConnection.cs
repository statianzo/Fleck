using System;
using Fleck.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Fleck
{

    public interface IHandler
    {
        byte[] CreateHandshake();
        void Recieve(IEnumerable<byte> data);
        byte[] FrameText(string text);
        byte[] FrameClose(int code);
    }

    public interface IHandlerFactory 
    {
        IHandler BuildHandler(byte[] data, Action<string> onMessage);
    }
    
    public class RecievingWebSocketConnection : IWebSocketConnection
    {
        public RecievingWebSocketConnection(ISocket socket, IHandlerFactory handlerFactory)
        {
            Socket = socket;
            OnOpen = () => { };
            OnClose = () => { };
            OnMessage = x => { };
            OnError = x => { };
            _handlerFactory = handlerFactory;
        }

        public ISocket Socket { get; set; }

        private readonly IHandlerFactory _handlerFactory;
        private IHandler _handler;

        public Action OnOpen { get; set; }
        public Action OnClose { get; set; }
        public Action<string> OnMessage { get; set; }
        public Action<Exception> OnError { get; set; }

        public void Send(string message)
        {
            if (_handler == null)
                throw new WebSocketException("Cannot send before handshake");
                
            var bytes = _handler.FrameText(message);
            SendBytes(bytes);
        }

        public void StartReceiving()
        {
            var data = new List<byte>(1024*16);
            var buffer = new byte[1024*16];
            Read(data, buffer);
        }
        
        private void Read(List<byte> data, byte[] buffer)
        {
            Socket.Receive(buffer, r => {
                if (r <= 0)
                {
                    CloseSocket();
                    return;
                }
                var readBytes = buffer.Take(r);
                if (_handler != null)
                {
                    _handler.Recieve(readBytes);
                }
                else
                {
                    data.AddRange(readBytes);
                    CreateHandler(data);
                }
                
                Read(data, buffer);
            },
            e => {
               FleckLog.Error("Error while reading", e);
            });
        }
        
        private void SendBytes(byte[] bytes, Action callback = null)
        {
            Socket.Send(bytes, () => {
                FleckLog.Debug("Sent " + bytes.Length + " bytes");
                if (callback != null)
                    callback();
            },
            e => {
                FleckLog.Info("Failed to send. Disconnecting.", e);
                CloseSocket();
            });
        }
        
        private void CreateHandler(IList<byte> data)
        {
            _handler = _handlerFactory.BuildHandler(data.ToArray(), OnMessage);
            if (_handler == null)
                return;
            var handshake = _handler.CreateHandshake();
            SendBytes(handshake, OnOpen);
        }

        public void Close()
        {
            Close(1000);
        }
        
        public void Close(int code)
        {
            if (_handler == null)
               CloseSocket();
               
            var bytes = _handler.FrameClose(code);
            if (bytes.Length == 0)
                CloseSocket();
            else
                SendBytes(bytes, CloseSocket);
        }
        
        private void CloseSocket() 
        {
            OnClose();
            Socket.Close();
            Socket.Dispose();
        }
    }
}