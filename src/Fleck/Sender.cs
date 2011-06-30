using System;

namespace Fleck
{
    public class Sender
    {
        private readonly ISocket _socket;
        private readonly Action _closeAction;

        public Sender(ISocket socket, Action closeAction)
        {
            _socket = socket;
            _closeAction = closeAction;
        }

        public ISocket Socket { get { return _socket; } }

        public void Send(string data)
        {
            if (!Socket.Connected) return;
            var wrapped = DataFrame.Wrap(data);

            _socket.Send(wrapped,
                         () => FleckLog.Debug("Send succeeded"),
                         e =>
                         {
                             FleckLog.Error("Send failed", e);
                             _closeAction();
                         });
        }
    }
}