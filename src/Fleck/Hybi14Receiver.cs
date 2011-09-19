using System;
using Fleck.Interfaces;
using System.Text;

namespace Fleck
{
    public class Hybi14Receiver : IReceiver
    {
        private readonly ISocket _socket;
        private readonly Hybi14DataFrameStreamReader _reader;
        
        public Hybi14Receiver(ISocket socket)
        {
            _socket = socket;
            _reader = new Hybi14DataFrameStreamReader(_socket.Stream);
        }
        
        public event Action OnError = delegate{};

        public event Action<string> OnMessage = delegate(string s) {};

        public void Receive()
        {
            Receive(new StringBuilder());
        }
        
        public void Receive(StringBuilder builder)
        {
            
            _reader.ReadFrame(f => {
                builder.Append(Encoding.UTF8.GetString(f.Payload));
                if (f.IsFinal)
                {
                    OnMessage(builder.ToString());
                    builder = new StringBuilder();
                }
                Receive(builder);
            },
            e => {
                FleckLog.Error("Failed to receive", e);
                OnError();
            });
            
        }
    }
}

