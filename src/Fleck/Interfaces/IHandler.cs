using System;
using System.Collections.Generic;

namespace Fleck
{
    public interface IHandler
    {
        Tuple<string, byte[]> CreateHandshake();
        void Receive(IEnumerable<byte> data);
        byte[] FrameText(string text);
        byte[] FrameBinary(byte[] bytes);
        byte[] FrameClose(int code);
    }
}

