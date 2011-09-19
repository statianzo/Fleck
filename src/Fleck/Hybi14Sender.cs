using System;
using Fleck.Interfaces;
using System.Text;

namespace Fleck
{
    public class Hybi14Sender : ISender
    {
        private readonly ISocket _socket;
        public Hybi14Sender(ISocket socket)
        {
            _socket = socket;
        }

        public event Action OnError = delegate { };
         

        public void SendText(string text)
        {
            var frame = new Hybi14DataFrame {
                IsFinal = true,
                IsMasked = false,
                Opcode = Opcode.Text,
                Payload = Encoding.UTF8.GetBytes(text)
            };
            
            var bytes = frame.ToBytes();
            _socket.Send(bytes, () => {
                FleckLog.Debug("Hybi14 Send succeeded");
            }, e => {
                FleckLog.Error("Hybi14 Send Failed", e);
                OnError();
            });
        }
    }
}

