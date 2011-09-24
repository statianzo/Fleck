using System;
using System.Collections.Generic;

namespace Fleck
{
    public class Draft76Handler
    {
        public static IHandler Create(Action<byte[]> sendBytes)
        {
            return new ComposableHandler
            {
                Send = sendBytes
            };
        }
    }
    
    public class ComposableHandler : IHandler
    {
        public Func<byte[]> Handshake = () => new byte[0];
        public Func<string, byte[]> FrameText = x => new byte[0];
        public Action<byte[]> Send = delegate { };
        public Action<List<byte>> RecieveData = delegate { };
        public Action<int, string> Close = delegate { }; 
        public Action<Opcode, byte[]> ProcessMessage = delegate { };
        
        private List<byte> _data = new List<byte>();

        public void Run()
        {
            Send(Handshake());
        }

        public void Recieve(IEnumerable<byte> data)
        {
            _data.AddRange(data);
            
            RecieveData(_data);
        }
        
        public void SendText(string text)
        {
            Send(FrameText(text));
        }
    }
}
