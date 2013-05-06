using System;
using System.Collections.Generic;

namespace Fleck
{
    public interface IWebSocketServer : IDisposable
    {
        void Start(ISubProtocolHandler defaultInitializer, IEnumerable<ISubProtocolHandler> subProtocolInitializers);
    }
}
