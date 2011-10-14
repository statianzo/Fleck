using System;
using System.Collections.Generic;

namespace Fleck.Handlers
{
    public class ComposableHandler : IHandler
    {
        public Func<byte[]> Handshake = () => new byte[0];
        public Func<string, byte[]> Frame = x => new byte[0];
        public Action<List<byte>> RecieveData = delegate { };
        public Func<int, byte[]> Close = i => new byte[0];
        
        private readonly List<byte> _data = new List<byte>();

        public byte[] CreateHandshake()
        {
            return Handshake();
        }

        public void Recieve(IEnumerable<byte> data)
        {
            _data.AddRange(data);
            
            RecieveData(_data);
        }
        
        public byte[] FrameText(string text)
        {
            return Frame(text);
        }
        
        public byte[] FrameClose(int code)
        {
            return Close(code);
        }
    }
}

