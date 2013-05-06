using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fleck
{
    public interface ISubProtocolHandler
    {
        string Identifier { get; }
        Action<IWebSocketConnection> SubProtocolInitializer { get; }
        IDictionary<Guid, IWebSocketConnection> Connections { get; }
    }
}
