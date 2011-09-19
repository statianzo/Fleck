using System;
using Fleck.Interfaces;

namespace Fleck
{
    public class WebSocketConnection : IWebSocketConnection
    {
        public WebSocketConnection(ISocket socket, ISender sender, IReceiver receiver)
        {
            Socket = socket;
            _sender = sender;
            _receiver = receiver;
            OnOpen = () => { };
            OnClose = () => { };
            OnMessage = x => { };
            
            _sender.OnError += Close;
            _receiver.OnError += Close;
            _receiver.OnMessage += s => OnMessage(s);
        }

        public ISocket Socket { get; set; }

        private readonly ISender _sender;
        private readonly IReceiver _receiver;

        public Action OnOpen { get; set; }
        public Action OnClose { get; set; }
        public Action<string> OnMessage { get; set; }

        public void Send(string message)
        {
            _sender.SendText(message);
        }

        public void StartReceiving()
        {
            _receiver.Receive();
        }

        public void Close()
        {
            OnClose();
            Socket.Close();
        }
    }
}