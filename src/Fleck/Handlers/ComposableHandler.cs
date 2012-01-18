using System;
using System.Collections.Generic;

namespace Fleck.Handlers
{
    public class ComposableHandler : IHandler
    {
        public Func<byte[]> Handshake = () => new byte[0];
        public Func<string, byte[]> TextFrame = x => new byte[0];
        public Func<byte[], byte[]> BinaryFrame = x => new byte[0];
        public Action<List<byte>> ReceiveData = delegate { };
        public Func<int, byte[]> CloseFrame = i => new byte[0];
        
        private readonly List<byte> _data = new List<byte>();

        public byte[] CreateHandshake()
        {
            return Handshake();
        }

        public void Receive(IEnumerable<byte> data)
        {
            _data.AddRange(data);
            
            ReceiveData(_data);
        }
        
        public byte[] FrameText(string text)
        {
            return TextFrame(text);
        }
        
        public byte[] FrameBinary(byte[] bytes)
        {
            return BinaryFrame(bytes);
        }
        
        public byte[] FrameClose(int code)
        {
            return CloseFrame(code);
        }
    }
}

