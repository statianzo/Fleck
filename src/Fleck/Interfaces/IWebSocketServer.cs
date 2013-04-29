using System;
using System.Collections.Generic;

namespace Fleck
{
    public interface IWebSocketServer : IDisposable
    {
        void Start(Action<IWebSocketConnection> config, ISet<string> supportedSubProtocols);
    }
}
