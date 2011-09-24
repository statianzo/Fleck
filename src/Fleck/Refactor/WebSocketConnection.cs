using System;
using Fleck.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Fleck
{

    public interface IHandler
    {
        void Run();
        void Recieve(IEnumerable<byte> data);
    }

    public interface IHandlerFactory 
    {
        IHandler BuildHandler(byte[] data, Action<int> close);
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
                
            },
            e => {
                
            });
        }
        
        private void CreateHandler(IList<byte> data)
        {
            _handler = _handlerFactory.BuildHandler(data.ToArray(), Close);
            if (_handler == null)
                return;
            _handler.Run();
        }

        public void Close()
        {
            Close(1000);
        }
        
        public void Close(int code)
        {
        }
    }
}