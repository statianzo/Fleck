using System;

namespace Fleck.Interfaces
{
    public interface IResponseBuilder
    {
        bool CanHandle(WebSocketHttpRequest request);
        byte[] Build(WebSocketHttpRequest request);
        ISender CreateSender(ISocket socket);
        IReceiver CreateReceiver(ISocket socket);
    }
}

