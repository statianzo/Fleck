using System;
using System.Collections.Generic;

namespace Fleck.Handlers
{
    public class ComposableHandler : IHandler
    {
        public Func<string, byte[]> Handshake = s => new byte[0];
        public Func<string, byte[]> TextFrame = x => new byte[0];
        public Func<byte[], byte[]> BinaryFrame = x => new byte[0];
        public Action<List<byte>> ReceiveData = delegate { };
        public Func<byte[], byte[]> PingFrame = i => new byte[0];
        public Func<byte[], byte[]> PongFrame = i => new byte[0];
        public Func<int, byte[]> CloseFrame = i => new byte[0];
        
        private readonly List<byte> _data = new List<byte>();

        public byte[] CreateHandshake(string subProtocol = null)
        {
            return Handshake(subProtocol);
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
        
        public byte[] FramePing(byte[] bytes)
        {
            return PingFrame(bytes);
        }
        
        public byte[] FramePong(byte[] bytes)
        {
            return PongFrame(bytes);
        }
        
        public byte[] FrameClose(int code)
        {
            return CloseFrame(code);
        }
    }
}

