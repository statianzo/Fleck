using System;
using Fleck.Interfaces;

namespace Fleck
{
    public class Sender : ISender
    {
        private readonly ISocket _socket;
        
        public event Action OnError = delegate {};

        public Sender(ISocket socket)
        {
            _socket = socket;
        }

        public ISocket Socket { get { return _socket; } }

        public void SendText(string data)
        {
            if (!Socket.Connected) return;
            var wrapped = DataFrame.Wrap(data);

            _socket.Send(wrapped,
                         () => FleckLog.Debug("Draft76 Send Succeeded"),
                         e =>
                         {
                             FleckLog.Error("Draft76 Send Failed", e);
                             OnError();
                         });
        }
    }
}