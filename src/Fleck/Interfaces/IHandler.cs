using System;
using System.Collections.Generic;

namespace Fleck.Interfaces
{
    public interface IHandler
    {
        byte[] CreateHandshake();
        void Recieve(IEnumerable<byte> data);
        byte[] FrameText(string text);
        byte[] FrameClose(int code);
    }
}

