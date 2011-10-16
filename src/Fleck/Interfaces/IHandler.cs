using System.Collections.Generic;

namespace Fleck
{
    public interface IHandler
    {
        byte[] CreateHandshake();
        void Receive(IEnumerable<byte> data);
        byte[] FrameText(string text);
        byte[] FrameClose(int code);
    }
}

