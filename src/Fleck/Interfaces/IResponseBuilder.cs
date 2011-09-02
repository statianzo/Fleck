using System;

namespace Fleck.Interfaces
{
    public interface IResponseBuilder
    {
        bool CanHandle(WebSocketHttpRequest request);
        byte[] Build(WebSocketHttpRequest request);
    }
}

