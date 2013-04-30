using System;
using System.Collections.Generic;

namespace Fleck
{
    public interface IWebSocketServer : IDisposable
    {
        void Start(Action<IWebSocketConnection> defaultInitializer, IDictionary<string, Action<IWebSocketConnection>> subProtocolInitializers);
    }
}
